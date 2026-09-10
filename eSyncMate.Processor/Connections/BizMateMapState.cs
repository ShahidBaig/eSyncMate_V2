using eSyncMate.DB.Entities;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Reports MapDisabled to BizMate's partner-state feed (W2-17, E18), on the transition into and
    /// out of the state rather than on every pass.
    ///
    /// Why this state is reported at all, given BizMate set the configuration itself: the feed exists
    /// so their boards can explain silence. A partner whose documents stop flowing looks identical to
    /// a partner with nothing to send, and the person reading the board is not necessarily the person
    /// who turned the map off. So this is not a claim about their configuration - it is a report of
    /// OUR behaviour: eSyncMate is not sending for this partner, and this is why.
    ///
    /// Why it does not belong in the drain, where the other three states are reported: Up, Down and
    /// QueueBacklog are things a drain pass observed about a connection. MapDisabled is a statement
    /// about configuration, and the drain has no way to know it - sending it from there would be
    /// inventing a fact. It belongs where the configuration is already read, which is the outbound
    /// route's enablement check (W1-13).
    ///
    /// Why it latches: the other three are transient observations that stop being reported when they
    /// stop being true. A disabled map is steady state and would otherwise be re-sent every poll
    /// interval for as long as it stayed off, which is noise on a board rather than information.
    ///
    /// Why the latch is a TABLE and not a static dictionary, which is what it was first built as:
    /// `RouteEngine:UseExternalProcess` is true, so every route execution is a fresh short-lived
    /// RouteWorker process. Process memory is born empty on every pass, and the first version duly
    /// reported MapDisabled at 17:55:07 and again at 18:00:09 having remembered nothing. Anything
    /// that must survive from one pass to the next has to be written down.
    /// </summary>
    public static class BizMateMapState
    {

        /// <summary>
        /// Called on every outbound pass with what the enablement check decided.
        ///
        /// <paramref name="disabled"/> says whether eSyncMate is declining to serve this partner
        /// because BizMate's configuration says so. Returns the state actually reported, or null when
        /// nothing needed saying - which is the ordinary answer.
        ///
        /// Never throws. A partner state that cannot be delivered must not fail a route that is
        /// otherwise working, and an unreachable BizMate is a likely reason for the failure anyway.
        /// </summary>
        public static string? Sync(
            BizMateConnector connector,
            string connectionString,
            int userNo,
            string partnerId,
            bool disabled,
            string detail,
            Action<string, string> log)
        {
            if (connector is null || string.IsNullOrWhiteSpace(partnerId))
            {
                return null;
            }

            // Nothing is reported on recovery. Up would be a lie - it means a call went through,
            // which is the drain's fact to state - and there is no "MapEnabled" in the vocabulary.
            // The resumption of documents is what says the map is back, and it says it better than
            // we could. So a partner that is fine needs no row and no call.
            if (!disabled)
            {
                return null;
            }

            var l_Report = new EDIPartnerStateReport();

            try
            {
                l_Report.UseConnection(connectionString);

                if (string.Equals(l_Report.GetLatestState(partnerId), PartnerStates.MapDisabled, StringComparison.OrdinalIgnoreCase))
                {
                    return null;   // already said, and it has not changed
                }
            }
            catch (Exception ex)
            {
                // Cannot tell whether it has been said. Say it: a board that hears twice is better
                // than one that never hears at all, and the failure is visible in the log either way.
                log("Could not read the last reported state for [" + partnerId + "]; reporting anyway", ex.Message);
            }

            try
            {
                connector.PostPartnerStateAsync(
                    new PartnerStateRequest
                    {
                        PartnerId = partnerId,
                        State = PartnerStates.MapDisabled,
                        Detail = detail,
                        At = DateTimeOffset.UtcNow
                    },
                    BizMateInboundPipeline.NewCorrelationId()).GetAwaiter().GetResult();
            }
            catch (BizMateException ex)
            {
                // No row is written, so it is tried again next pass: a state BizMate never received
                // is not a state we have reported.
                log("Could not report " + PartnerStates.MapDisabled + " for [" + partnerId + "]", ex.Message);

                return null;
            }

            try
            {
                l_Report.PartnerId = partnerId;
                l_Report.State = PartnerStates.MapDisabled;
                l_Report.Detail = detail;
                l_Report.ReportedAt = DateTime.UtcNow;
                l_Report.CreatedDate = DateTime.UtcNow;
                l_Report.CreatedBy = userNo;

                l_Report.SaveNew();
            }
            catch (Exception ex)
            {
                // BizMate has it. Failing to write our own note of that must not undo the report -
                // the only cost is that the next pass says it again.
                log("Reported " + PartnerStates.MapDisabled + " for [" + partnerId + "] but could not record it", ex.Message);

                return PartnerStates.MapDisabled;
            }

            log("Reported " + PartnerStates.MapDisabled + " for [" + partnerId + "]: " + detail, string.Empty);

            return PartnerStates.MapDisabled;
        }

        /// <summary>
        /// The document types BizMate has switched off for this partner, as a readable phrase, or
        /// null when none are. Goes in the detail so the board entry says which map rather than only
        /// that one is off.
        /// </summary>
        public static string? DisabledDocuments(CustomerIntegrationConfig? config)
        {
            if (config is null || config.Documents.Count == 0)
            {
                return null;
            }

            List<string> l_Off = config.Documents
                .Where(d => !d.Enabled)
                .Select(d => d.Describe())
                .Where(d => d.Length > 0)
                .ToList();

            return l_Off.Count == 0 ? null : string.Join(", ", l_Off);
        }

    }
}
