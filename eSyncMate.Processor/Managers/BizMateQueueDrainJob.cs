using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using Hangfire;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// The recurring drain of the BizMate store-and-forward queue (W1-14, requirement E22).
    ///
    /// "Automatic drain when BizMate returns" is what this provides: the queue itself guarantees
    /// nothing is lost, but something has to come back and try again, and a scheduled job is that
    /// something. Nothing else in the pipeline retries - a route that hands a call to the queue is
    /// finished with it.
    ///
    /// One recurring job, not one per partner. Fairness across partners is the drain's own job
    /// (round-robin, oldest-waiting first), and a job per partner would hand that decision to
    /// Hangfire's scheduler, which knows nothing about it.
    /// </summary>
    public class BizMateQueueDrainJob
    {
        public const string RecurringJobId = "BizMate queue drain";

        /// <summary>
        /// Runs one bounded pass.
        ///
        /// <see cref="DisableConcurrentExecution"/> is belt and braces rather than the guarantee:
        /// the claim itself is safe under concurrency (ROWLOCK, READPAST, and the FIFO rule refusing
        /// to step over an in-flight row), so two overlapping drains would be correct but wasteful.
        ///
        /// <see cref="AutomaticRetryAttribute"/> is set to zero deliberately. Hangfire retrying this
        /// job would be a second, competing retry policy on top of the queue's own backoff - and the
        /// queue's is the one that knows about 429s, refusals and per-partner ordering. A failed
        /// pass simply means the next scheduled one picks the work up.
        /// </summary>
        [DisableConcurrentExecution(timeoutInSeconds: 300)]
        [AutomaticRetry(Attempts = 0)]
        public async Task Execute()
        {
            if (!IsConfigured(out string l_Reason))
            {
                // Do not throw. While the credential question (EQ-06) is open this would fail every
                // minute and bury the Hangfire dashboard in noise that says nothing new.
                Console.WriteLine($"[BizMateQueueDrainJob] Skipped: {l_Reason}");

                return;
            }

            var l_Connector = new BizMateConnector(
                CommonUtils.BizMate_GatewayUrl,
                CommonUtils.BizMate_ClientId,
                CommonUtils.BizMate_ClientSecret,
                CommonUtils.BizMate_SigningSecret);

            var l_Queue = new BizMateOutboundQueue(l_Connector, CommonUtils.ConnectionString, 1);

            DrainSummary l_Summary = await l_Queue.DrainAsync(
                CommonUtils.BizMate_QueueDrainMaxCalls,
                CommonUtils.BizMate_QueueDrainMaxPartners).ConfigureAwait(false);

            if (l_Summary.Attempted > 0 || l_Summary.Reclaimed > 0)
            {
                Console.WriteLine(
                    $"[BizMateQueueDrainJob] attempted {l_Summary.Attempted}, succeeded {l_Summary.Succeeded}, " +
                    $"deferred {l_Summary.Deferred}, failed {l_Summary.Failed}, reclaimed {l_Summary.Reclaimed}");
            }

            await ReportBacklogAsync(l_Connector, l_Summary).ConfigureAwait(false);
        }

        /// <summary>
        /// Tells BizMate which partners are backed up (W2-17, E18), so queue depth is a fact on
        /// their boards rather than a number living only in our own monitoring (X-07).
        ///
        /// Posted directly rather than through the queue: if BizMate is unreachable, queueing a
        /// report that BizMate is unreachable helps nobody, and it would sit behind the very
        /// backlog it describes.
        /// </summary>
        private static async Task ReportBacklogAsync(BizMateConnector connector, DrainSummary summary)
        {
            foreach (string l_PartnerId in summary.PartnersBackedUp)
            {
                if (string.IsNullOrWhiteSpace(l_PartnerId))
                {
                    continue;
                }

                try
                {
                    await connector.PostPartnerStateAsync(
                        new PartnerStateRequest
                        {
                            PartnerId = l_PartnerId,
                            State = PartnerStates.QueueBacklog,
                            Detail = $"Outbound queue at or above {BizMateOutboundQueue.BacklogWarnDepth} calls.",
                            At = DateTimeOffset.UtcNow
                        },
                        BizMateInboundPipeline.NewCorrelationId()).ConfigureAwait(false);
                }
                catch (BizMateException ex)
                {
                    // An unknown partner answers 404, and BizMate being down is the likeliest reason
                    // for the backlog in the first place. Neither is worth failing the drain over.
                    Console.WriteLine($"[BizMateQueueDrainJob] Could not report backlog for {l_PartnerId}: {ex.Message}");
                }
            }
        }

        private static bool IsConfigured(out string reason)
        {
            if (!CommonUtils.BizMate_QueueDrainEnabled)
            {
                reason = "BizMate_QueueDrainEnabled is false.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(CommonUtils.BizMate_GatewayUrl)
                || string.IsNullOrWhiteSpace(CommonUtils.BizMate_ClientId)
                || string.IsNullOrWhiteSpace(CommonUtils.BizMate_ClientSecret)
                || string.IsNullOrWhiteSpace(CommonUtils.BizMate_SigningSecret))
            {
                reason = "the BizMate gateway URL and credential trio are not set in ApplicationSettings " +
                         "(BizMate_GatewayUrl, BizMate_ClientId, BizMate_ClientSecret, BizMate_SigningSecret).";

                return false;
            }

            reason = string.Empty;

            return true;
        }

        /// <summary>
        /// Registers or removes the recurring job to match configuration. Called once at start-up.
        ///
        /// It registers even when the credentials are missing, so the schedule exists and starts
        /// working the moment the trio is filled in - no redeploy, and no silently absent job that
        /// somebody has to remember to create.
        /// </summary>
        public static void Register()
        {
            try
            {
                if (!CommonUtils.BizMate_QueueDrainEnabled)
                {
                    RecurringJob.RemoveIfExists(RecurringJobId);

                    return;
                }

                var l_Job = new BizMateQueueDrainJob();

                // UTC, not TimeZoneInfo.Local as the older route jobs use. The schedule has no
                // business shifting with a server's zone, and X-02 is the same argument.
                //
                // The RecurringJobOptions overload rather than the TimeZoneInfo one: the latter is
                // obsolete in Hangfire 1.8 and goes away at 2.0.
                RecurringJob.AddOrUpdate(
                    RecurringJobId,
                    () => l_Job.Execute(),
                    CommonUtils.BizMate_QueueDrainCron,
                    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
            }
            catch (Exception ex)
            {
                // A bad cron expression must not stop the service booting.
                Console.WriteLine($"[BizMateQueueDrainJob.Register] Failed to schedule the drain: {ex.Message}");
            }
        }
    }
}
