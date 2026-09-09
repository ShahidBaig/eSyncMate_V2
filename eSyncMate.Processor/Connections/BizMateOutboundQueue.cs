using System.Data;
using eSyncMate.DB.Entities;

namespace eSyncMate.Processor.Connections
{
    /// <summary>What one pass of the drain did, so a caller can log or alert on it.</summary>
    public sealed class DrainSummary
    {
        public int Attempted { get; set; }
        public int Succeeded { get; set; }
        public int Deferred { get; set; }
        public int Failed { get; set; }
        public int Reclaimed { get; set; }
        public List<string> PartnersBackedUp { get; } = new();

        /// <summary>
        /// Partners a call went through to on this pass. Whatever was wrong with the connection to
        /// them is over, which is the only moment eSyncMate can honestly say Up (W2-17).
        /// </summary>
        public HashSet<string> PartnersReachable { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Partners whose call failed on the transport - unreachable, timed out, a 5xx. Not a
        /// partner whose document BizMate refused, which says nothing about the connection.
        /// </summary>
        public HashSet<string> PartnersUnreachable { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Raised when the queue refuses to accept more work for a partner. This is not shedding:
    /// nothing already accepted is discarded. It is a visible refusal at the boundary, which is the
    /// honest alternative to silently accepting work that cannot be honoured.
    /// </summary>
    public sealed class BizMateQueueFullException : Exception
    {
        public string PartnerId { get; }
        public int Depth { get; }

        public BizMateQueueFullException(string partnerId, int depth)
            : base($"The outbound queue for partner '{partnerId}' holds {depth} calls, at or above the " +
                   $"acceptance ceiling of {BizMateOutboundQueue.AcceptanceCeiling}. Nothing already queued " +
                   "has been discarded; new work is refused until the backlog drains.")
        {
            PartnerId = partnerId;
            Depth = depth;
        }
    }

    /// <summary>
    /// Durable store-and-forward for calls to BizMate (W1-14, requirement E22).
    ///
    /// The three commitments made in answer to EQ-11, implemented here and in the table's indexes:
    ///
    ///   Ordering  - FIFO within a partner. The claim refuses to step over an older in-flight call
    ///               for the same partner, so a document cannot overtake its predecessor.
    ///   Fairness  - across partners the drain round-robins, oldest-waiting partner first, so one
    ///               partner's backlog never starves the others.
    ///   Shedding  - never. Accepted work is delivered or visibly failed. Pressure is handled by
    ///               refusing acceptance at a stated ceiling, not by discarding.
    ///
    /// Both thresholds are provisional and are to be confirmed by the load measurement in X-06
    /// before Phase 1 closes.
    /// </summary>
    public sealed class BizMateOutboundQueue
    {
        /// <summary>Depth at which QueueBacklog is reported on the connection-state feed (W2-17).</summary>
        public const int BacklogWarnDepth = 10_000;

        /// <summary>Depth at which new work is refused at the boundary rather than accepted and lost.</summary>
        public const int AcceptanceCeiling = 50_000;

        /// <summary>A claim held longer than this is assumed abandoned by a dead worker.</summary>
        public const int StaleClaimMinutes = 15;

        private const int MaxBackoffSeconds = 900;   // 15 minutes
        private const int MaxAttempts = 50;

        private static readonly Random _jitter = new();

        private readonly BizMateConnector _connector;
        private readonly string _connectionString;
        private readonly int _userNo;
        private readonly string _workerName;

        public BizMateOutboundQueue(BizMateConnector connector, string connectionString, int userNo, string? workerName = null)
        {
            _connector = connector ?? throw new ArgumentNullException(nameof(connector));
            _connectionString = connectionString;
            _userNo = userNo;
            _workerName = workerName ?? (Environment.MachineName + ":" + Environment.ProcessId);
        }

        /// <summary>
        /// Persists a call before it is attempted. Nothing reaches BizMate that is not written here
        /// first, which is what makes "nothing is lost across a restart" true on both sides.
        /// </summary>
        public EDIOutboundQueue Enqueue(
            string partnerId,
            string operation,
            HttpMethod method,
            string publicPath,
            string? payloadJson,
            string scope,
            string correlationId,
            long? ledgerId = null,
            string? urlPathWithQuery = null)
        {
            var l_Item = new EDIOutboundQueue();

            l_Item.UseConnection(_connectionString);

            int l_Depth = l_Item.GetDepth(partnerId);

            if (l_Depth >= AcceptanceCeiling)
            {
                // Refuse loudly. The caller records the refusal on the ledger, so the document is
                // visible as refused rather than absent.
                throw new BizMateQueueFullException(partnerId, l_Depth);
            }

            l_Item.LedgerId = ledgerId;
            l_Item.PartnerId = partnerId;
            l_Item.Operation = operation;
            l_Item.HttpMethod = method.Method;
            l_Item.PublicPath = publicPath;
            l_Item.UrlPathWithQuery = urlPathWithQuery;
            l_Item.Scope = scope;
            l_Item.Payload = payloadJson;
            l_Item.CorrelationId = correlationId;
            l_Item.Status = "Pending";
            l_Item.AttemptCount = 0;
            l_Item.NextAttemptAt = DateTime.UtcNow;
            l_Item.CreatedDate = DateTime.UtcNow;
            l_Item.CreatedBy = _userNo;

            l_Item.SaveNew();

            return l_Item;
        }

        /// <summary>
        /// One pass of the drain. Returns when there is no work due, or when
        /// <paramref name="maxCalls"/> have been attempted - a bounded pass so a scheduled job
        /// cannot run away, and so a caller can interleave passes with other work.
        /// </summary>
        public async Task<DrainSummary> DrainAsync(
            int maxCalls = 500, int maxPartnersPerPass = 100, CancellationToken cancellationToken = default)
        {
            var l_Summary = new DrainSummary();

            var l_Reclaimer = new EDIOutboundQueue();
            l_Reclaimer.UseConnection(_connectionString);

            // A worker that died mid-flight would otherwise block its partner's queue forever,
            // because the FIFO rule will not step over an in-flight row.
            l_Summary.Reclaimed = l_Reclaimer.ReclaimStale(StaleClaimMinutes);

            var l_Partners = new DataTable();

            if (!l_Reclaimer.GetPartnersWithDueWork(maxPartnersPerPass, ref l_Partners) || l_Partners.Rows.Count == 0)
            {
                l_Partners.Dispose();

                return l_Summary;
            }

            // Round-robin: one call per partner per lap, oldest-waiting partner first. A partner
            // with ten thousand queued calls therefore gets one turn per lap, exactly like a
            // partner with one - which is the whole of the fairness commitment.
            bool l_ProgressThisLap = true;

            while (l_ProgressThisLap && l_Summary.Attempted < maxCalls && !cancellationToken.IsCancellationRequested)
            {
                l_ProgressThisLap = false;

                foreach (DataRow l_Row in l_Partners.Rows)
                {
                    if (l_Summary.Attempted >= maxCalls || cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    string l_PartnerId = Convert.ToString(l_Row["PartnerId"]) ?? string.Empty;

                    if (string.IsNullOrEmpty(l_PartnerId))
                    {
                        continue;
                    }

                    var l_Item = new EDIOutboundQueue();
                    l_Item.UseConnection(_connectionString);

                    if (!l_Item.ClaimNext(l_PartnerId, _workerName).IsSuccess)
                    {
                        continue;
                    }

                    l_ProgressThisLap = true;
                    l_Summary.Attempted++;

                    await AttemptAsync(l_Item, l_Summary, cancellationToken).ConfigureAwait(false);
                }
            }

            // Report anyone sitting above the warning depth so the connection-state feed can say so
            // rather than the backlog living only in our own monitoring (X-07).
            foreach (DataRow l_Row in l_Partners.Rows)
            {
                if (Convert.ToInt32(l_Row["Depth"]) >= BacklogWarnDepth)
                {
                    l_Summary.PartnersBackedUp.Add(Convert.ToString(l_Row["PartnerId"]) ?? string.Empty);
                }
            }

            l_Partners.Dispose();

            return l_Summary;
        }

        private async Task AttemptAsync(EDIOutboundQueue item, DrainSummary summary, CancellationToken cancellationToken)
        {
            item.AttemptCount += 1;
            item.LastAttemptAt = DateTime.UtcNow;

            try
            {
                await _connector.SendRawAsync(
                    new HttpMethod(item.HttpMethod),
                    item.PublicPath,
                    item.Payload,
                    item.Scope,
                    item.CorrelationId,
                    item.UrlPathWithQuery,
                    cancellationToken).ConfigureAwait(false);

                item.Status = "Succeeded";
                item.CompletedAt = DateTime.UtcNow;
                item.ClaimedAt = null;
                item.ClaimedBy = null;
                item.Modify();

                summary.Succeeded++;

                // A call that went through is the only honest evidence the connection is back.
                summary.PartnersReachable.Add(item.PartnerId ?? string.Empty);
            }
            catch (BizMateRateLimitedException ex)
            {
                // Not a document failure - the document was never read. Honour Retry-After when
                // BizMate gave one, and do not count this against the attempt ceiling.
                item.AttemptCount -= 1;

                Defer(item, ex.RetryAfter ?? TimeSpan.FromSeconds(30), "Rate limited: " + ex.Message);

                summary.Deferred++;
            }
            catch (BizMateException ex) when (ex.IsRetryable)
            {
                // Retryable means the transport or the far side, not the document - so this is the
                // one failure that says something about the connection (W2-17).
                summary.PartnersUnreachable.Add(item.PartnerId ?? string.Empty);

                if (item.AttemptCount >= MaxAttempts)
                {
                    Fail(item, $"Gave up after {item.AttemptCount} attempts: {ex.Message}");

                    summary.Failed++;
                }
                else
                {
                    Defer(item, Backoff(item.AttemptCount), ex.Message);

                    summary.Deferred++;
                }
            }
            catch (BizMateException ex)
            {
                // A refusal or a bad request. Sending it again will not help, so it stops here and
                // is recorded rather than retried on a schedule (W1-17, EQ-05).
                Fail(item, ex.Message);

                summary.Failed++;
            }
            catch (Exception ex)
            {
                Defer(item, Backoff(item.AttemptCount), "Unexpected error: " + ex.Message);

                summary.Deferred++;
            }
        }

        private static void Defer(EDIOutboundQueue item, TimeSpan delay, string reason)
        {
            item.Status = "Pending";
            item.NextAttemptAt = DateTime.UtcNow.Add(delay);
            item.LastError = reason;
            item.ClaimedAt = null;
            item.ClaimedBy = null;

            item.Modify();
        }

        private static void Fail(EDIOutboundQueue item, string reason)
        {
            item.Status = "Failed";
            item.LastError = string.IsNullOrWhiteSpace(reason) ? "No reason recorded." : reason;
            item.CompletedAt = DateTime.UtcNow;
            item.ClaimedAt = null;
            item.ClaimedBy = null;

            item.Modify();
        }

        /// <summary>
        /// Exponential backoff with jitter, capped. The jitter matters more than the curve: without
        /// it, everything queued during one BizMate outage retries in lockstep the moment it
        /// returns, and the recovery is what knocks it over again.
        /// </summary>
        public static TimeSpan Backoff(int attemptCount)
        {
            int l_Exponent = Math.Min(Math.Max(attemptCount, 1), 10);

            double l_Seconds = Math.Min(Math.Pow(2, l_Exponent), MaxBackoffSeconds);

            double l_Jittered;

            lock (_jitter)
            {
                // Full jitter: anywhere in [half, full]. Spreads a thundering herd without ever
                // waiting less than half the intended delay.
                l_Jittered = (l_Seconds / 2.0) + (_jitter.NextDouble() * (l_Seconds / 2.0));
            }

            return TimeSpan.FromSeconds(l_Jittered);
        }
    }
}
