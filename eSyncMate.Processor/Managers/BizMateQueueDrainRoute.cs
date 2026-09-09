using eSyncMate.Processor.Connections;
using eSyncMate.Processor.Models;
using eSyncMate.DB.Entities;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.Processor.Managers
{
    /// <summary>
    /// Route type 602 - the drain of the BizMate store-and-forward queue (W1-19, W1-14, E22).
    ///
    /// The queue guarantees nothing is lost when BizMate is unreachable; something still has to come
    /// back and try again, and this is that something. Nothing else retries - a route that hands a
    /// call to the queue is finished with it.
    ///
    /// It is a ROUTE rather than a Hangfire recurring job registered in Program.cs, which is what it
    /// used to be. The difference is not cosmetic: as a route it takes the same execution lock as
    /// everything else, writes to RouteLog where operators already look, appears in VW_Routes and the
    /// Flow interface, and can be test-run from the UI. As a bare Hangfire job it was invisible to
    /// all of that and its only voice was Console.WriteLine into a service window nobody reads.
    ///
    /// One route, not one per partner. Fairness across partners is the drain's own business
    /// (round-robin, oldest waiting first); a route per partner would hand that decision to the
    /// scheduler, which knows nothing about it. The route therefore needs no connectors of its own -
    /// it works the queue, and the queue already knows every partner in it.
    /// </summary>
    public class BizMateQueueDrainRoute
    {
        public static void Execute(IConfiguration config, Routes route)
        {
            int userNo = 1;

            try
            {
                route.SaveLog(LogTypeEnum.RouteInfo, $"[BizMateQueueDrain] Started route [{route.Id}]", string.Empty, userNo);

                if (!CommonUtils.BizMate_QueueDrainEnabled)
                {
                    route.SaveLog(LogTypeEnum.RouteInfo,
                        "[BizMateQueueDrain] Skipped: BizMate_QueueDrainEnabled is false in ApplicationSettings.",
                        string.Empty, userNo);

                    return;
                }

                if (string.IsNullOrWhiteSpace(CommonUtils.BizMate_GatewayUrl)
                    || string.IsNullOrWhiteSpace(CommonUtils.BizMate_ClientId)
                    || string.IsNullOrWhiteSpace(CommonUtils.BizMate_ClientSecret)
                    || string.IsNullOrWhiteSpace(CommonUtils.BizMate_SigningSecret))
                {
                    // Recorded, not failed. While the credential question (EQ-06) is open, failing
                    // every few minutes would bury the log in a line that says nothing new.
                    route.SaveLog(LogTypeEnum.RouteInfo,
                        "[BizMateQueueDrain] Skipped: the BizMate gateway URL and credential trio are not set in ApplicationSettings.",
                        string.Empty, userNo);

                    return;
                }

                var l_Connector = new BizMateConnector(
                    CommonUtils.BizMate_GatewayUrl,
                    CommonUtils.BizMate_ClientId,
                    CommonUtils.BizMate_ClientSecret,
                    CommonUtils.BizMate_SigningSecret);

                var l_Queue = new BizMateOutboundQueue(l_Connector, CommonUtils.ConnectionString, userNo);

                DrainSummary l_Summary = l_Queue.DrainAsync(
                    CommonUtils.BizMate_QueueDrainMaxCalls,
                    CommonUtils.BizMate_QueueDrainMaxPartners).GetAwaiter().GetResult();

                // Silence is the normal state: an empty queue is the healthy one, and a line every
                // few minutes saying "nothing to do" would drown the passes that did something.
                if (l_Summary.Attempted > 0 || l_Summary.Reclaimed > 0)
                {
                    route.SaveLog(LogTypeEnum.RouteInfo,
                        $"[BizMateQueueDrain] Completed route [{route.Id}]: attempted {l_Summary.Attempted}, " +
                        $"succeeded {l_Summary.Succeeded}, deferred {l_Summary.Deferred}, failed {l_Summary.Failed}, " +
                        $"reclaimed {l_Summary.Reclaimed}.",
                        string.Empty, userNo);
                }

                ReportBacklog(l_Connector, l_Summary, route, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(LogTypeEnum.Exception, $"[BizMateQueueDrain] Error executing route [{route.Id}]", ex.ToString(), userNo);
            }
        }

        /// <summary>
        /// Tells BizMate which partners are backed up (W2-17, E18), so queue depth is a fact on their
        /// boards rather than a number living only in our own monitoring (X-07).
        ///
        /// Posted directly rather than through the queue: if BizMate is unreachable, queueing a
        /// report that BizMate is unreachable helps nobody, and it would sit behind the very backlog
        /// it describes.
        /// </summary>
        private static void ReportBacklog(BizMateConnector connector, DrainSummary summary, Routes route, int userNo)
        {
            foreach (string l_PartnerId in summary.PartnersBackedUp)
            {
                if (string.IsNullOrWhiteSpace(l_PartnerId))
                {
                    continue;
                }

                try
                {
                    connector.PostPartnerStateAsync(
                        new PartnerStateRequest
                        {
                            PartnerId = l_PartnerId,
                            State = PartnerStates.QueueBacklog,
                            Detail = $"Outbound queue at or above {BizMateOutboundQueue.BacklogWarnDepth} calls.",
                            At = DateTimeOffset.UtcNow
                        },
                        BizMateInboundPipeline.NewCorrelationId()).GetAwaiter().GetResult();

                    route.SaveLog(LogTypeEnum.RouteInfo,
                        $"[BizMateQueueDrain] Reported QueueBacklog for [{l_PartnerId}].", string.Empty, userNo);
                }
                catch (BizMateException ex)
                {
                    // An unknown partner answers 404, and BizMate being down is the likeliest reason
                    // for the backlog in the first place. Neither is worth failing the drain over.
                    route.SaveLog(LogTypeEnum.Error,
                        $"[BizMateQueueDrain] Could not report the backlog for [{l_PartnerId}]", ex.Message, userNo);
                }
            }
        }
    }
}
