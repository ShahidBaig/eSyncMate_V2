using System.Data;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Registration point for the outbound renderers, and the little bit of lookup they share.
    ///
    /// Registration is explicit rather than a static constructor because the route consults the
    /// registry to decide what to fetch: a type with no renderer must be left Staged in BizMate's
    /// outbox, and "the static initialiser had not run yet" is not a reason a document should be
    /// skipped. Calling this at the top of the route makes the set deterministic.
    /// </summary>
    public static class BizMateRenderers
    {
        private static readonly object _lock = new();
        private static bool _registered;

        /// <summary>Registers every renderer that exists. Safe to call on every route run.</summary>
        public static void RegisterAll()
        {
            lock (_lock)
            {
                if (_registered)
                {
                    return;
                }

                BizMate856Renderer.Register();
                BizMate865Renderer.Register();

                _registered = true;
            }
        }

        /// <summary>
        /// The eSyncMate order a document answers, found by the partner's PO number.
        ///
        /// Orders.OrderNumber holds the partner's PO (BEG03 of the 850 that created it), so this is
        /// the same key both directions use. Null is an ordinary answer, not a failure: BizMate
        /// stages documents for orders that never came through eSyncMate, and the pipeline keeps
        /// the artifact alone in that case rather than inventing an order to hang it on (F-22).
        /// </summary>
        public static int? ResolveOrderId(Customers customer, string? poNumber)
        {
            if (customer == null || string.IsNullOrWhiteSpace(poNumber))
            {
                return null;
            }

            try
            {
                var l_Orders = new Orders();

                l_Orders.UseConnection(CommonUtils.ConnectionString);

                var l_Data = new DataTable();

                string l_Criteria =
                    $"OrderNumber = '{poNumber.Trim().Replace("'", "''")}' AND CustomerId = {customer.Id}";

                if (!l_Orders.GetList(l_Criteria, "Id", ref l_Data, "Id DESC") || l_Data.Rows.Count == 0)
                {
                    return null;
                }

                return Convert.ToInt32(l_Data.Rows[0]["Id"]);
            }
            catch (Exception)
            {
                // A lookup that cannot answer must not fail the document. Without an order id the
                // rendering is still captured, just not linked to the order screens.
                return null;
            }
        }
    }
}
