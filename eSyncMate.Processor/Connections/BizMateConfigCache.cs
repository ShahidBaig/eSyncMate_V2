using System.Collections.Concurrent;
using eSyncMate.Processor.Models;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// BizMate's resolved configuration for a partner, held only long enough to avoid asking on
    /// every document (W1-13, E9, E21).
    ///
    /// BizMate is the authority on whether a partner is enabled, and this deliberately keeps **no
    /// local copy** of that: nothing is written to a table, nothing survives a restart, and the only
    /// thing that lives between calls is a short-lived answer in memory. An enablement decision that
    /// persists on our side is one that can disagree with theirs, and the disagreement would be
    /// invisible until a partner received something they had been switched off from.
    ///
    /// The TTL is BizMate's own `cacheTtlSeconds` where they send one, clamped to
    /// `BizMate_ConfigCacheMaxSeconds`. Clamping matters because BizMate already caches the resolved
    /// configuration for up to 120 seconds on their side: whatever we hold stacks on top of that, so
    /// a generous TTL here turns "a change takes up to two minutes" into "a change takes up to two
    /// minutes plus however long eSyncMate felt like".
    ///
    /// **When BizMate cannot be reached it fails OPEN, and that is deliberate.** A config endpoint
    /// that blips would otherwise stop every route at once - the failure operators notice last and
    /// hate most - and nothing is actually risked by continuing: an inbound document still has to be
    /// accepted by BizMate, who will refuse it if the partner is disabled, and an outbound document
    /// cannot be collected from a BizMate that is not answering. The authority keeps the last word
    /// either way. A stale answer is preferred to no answer, and no answer is logged rather than
    /// silently treated as "enabled".
    /// </summary>
    public static class BizMateConfigCache
    {
        private sealed class Entry
        {
            public CustomerIntegrationConfig Config { get; init; } = new();
            public DateTime ExpiresAt { get; init; }
        }

        private static readonly ConcurrentDictionary<string, Entry> _entries =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Used when BizMate sends no cacheTtlSeconds of its own.</summary>
        private const int DefaultTtlSeconds = 60;

        /// <summary>
        /// The configuration for a partner, from cache when it is still fresh and from BizMate
        /// otherwise.
        ///
        /// <paramref name="stale"/> says the answer came from an expired entry because BizMate could
        /// not be reached - worth logging where it is used, because it means enablement may have
        /// changed without us hearing.
        ///
        /// Returns null only when BizMate has never answered for this partner AND cannot be reached
        /// now. Callers read that as "carry on", not as "disabled".
        /// </summary>
        public static async Task<CustomerIntegrationConfig?> GetAsync(
            BizMateConnector connector,
            string partnerId,
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            if (connector is null || string.IsNullOrWhiteSpace(partnerId))
            {
                return null;
            }

            if (_entries.TryGetValue(partnerId, out Entry? l_Cached) && l_Cached.ExpiresAt > DateTime.UtcNow)
            {
                return l_Cached.Config;
            }

            try
            {
                CustomerIntegrationConfig l_Config = await connector
                    .GetConfigByPartnerAsync(partnerId, correlationId, cancellationToken)
                    .ConfigureAwait(false);

                _entries[partnerId] = new Entry
                {
                    Config = l_Config,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(Ttl(l_Config))
                };

                return l_Config;
            }
            catch (BizMateException)
            {
                // Stale beats nothing: the last answer BizMate gave is still the best evidence we
                // have, and it is a great deal better than assuming.
                return l_Cached?.Config;
            }
        }

        /// <summary>
        /// Whether this partner is enabled for the EDI channel, and why the answer is what it is.
        ///
        /// The reason is returned rather than logged here so the caller can put it in RouteLog, where
        /// an operator is already looking, instead of in a place only this class knows about.
        /// </summary>
        public static async Task<(bool Enabled, string Reason)> IsEdiEnabledAsync(
            BizMateConnector connector,
            string partnerId,
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            CustomerIntegrationConfig? l_Config =
                await GetAsync(connector, partnerId, correlationId, cancellationToken).ConfigureAwait(false);

            if (l_Config is null)
            {
                return (true, $"BizMate has never answered for [{partnerId}] and cannot be reached now, "
                            + "so the route carries on: BizMate still refuses anything it should not accept.");
            }

            return l_Config.EdiEnabled
                ? (true, string.Empty)
                : (false, $"BizMate reports the EDI channel is not enabled for [{partnerId}].");
        }

        /// <summary>
        /// Whether this partner is ALSO configured on the API channel (W1-08).
        ///
        /// BizMate's outbox poll defaults to EDI, so a customer on both channels has its API-side
        /// documents sit unseen unless they are asked for separately. That failure is the quiet
        /// kind - a poll that returns nothing looks exactly like a poll that had nothing to return -
        /// which is why the answer comes from BizMate's own configuration rather than a local flag
        /// somebody has to remember to set.
        ///
        /// False when BizMate cannot be reached and has never answered. Unlike the enablement check,
        /// which fails OPEN because refusing to send is the greater harm, this one fails CLOSED: an
        /// unnecessary second poll costs a round trip on every pass for every partner, and a document
        /// missed for one poll interval is recovered on the next pass once BizMate answers again.
        /// </summary>
        public static async Task<bool> IsApiAlsoEnabledAsync(
            BizMateConnector connector,
            string partnerId,
            string correlationId,
            CancellationToken cancellationToken = default)
        {
            CustomerIntegrationConfig? l_Config =
                await GetAsync(connector, partnerId, correlationId, cancellationToken).ConfigureAwait(false);

            return l_Config is { ApiEnabled: true };
        }

        /// <summary>Forgets everything held. For tests, and for an operator who has just changed configuration.</summary>
        public static void Clear() => _entries.Clear();

        private static int Ttl(CustomerIntegrationConfig config)
        {
            int l_Max = CommonUtils.BizMate_ConfigCacheMaxSeconds > 0
                ? CommonUtils.BizMate_ConfigCacheMaxSeconds
                : DefaultTtlSeconds;

            int l_Wanted = config.CacheTtlSeconds is > 0 ? config.CacheTtlSeconds!.Value : DefaultTtlSeconds;

            return Math.Min(l_Wanted, l_Max);
        }
    }
}
