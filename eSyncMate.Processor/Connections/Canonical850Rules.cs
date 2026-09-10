using System.Data;
using System.Text.Json;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;

namespace eSyncMate.Processor.Connections
{
    /// <summary>One intake rule a canonical 850 broke.</summary>
    public sealed class IntakeViolation
    {
        public string Rule { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
        public string Detail { get; init; } = string.Empty;

        public override string ToString() => $"{Path}: {Detail}";
    }

    /// <summary>
    /// An 850 that broke an intake rule. Carried as its own exception so the pipeline can record it
    /// as what it is - a refusal at the boundary - rather than as a translation that fell over.
    /// </summary>
    public sealed class IntakeRefusedException : Exception
    {
        public IntakeRefusedException(string message, string outcome) : base(message)
        {
            Outcome = outcome;
        }

        /// <summary>Failed for a document that is wrong; Rejected for one we decline to act on.</summary>
        public string Outcome { get; }
    }

    /// <summary>
    /// The 850 intake rules that sit beyond the schema (W3-02).
    ///
    /// A document can satisfy `850.schema.json` completely and still be one BizMate cannot turn into
    /// an order. The schema says a ship-to must be present; it cannot say that a non-dropship ship-to
    /// has to carry something that resolves to a CustomerContacts row, or that a dropship one needs a
    /// street and a country, because those are facts about BizMate's intake rather than about the
    /// document's shape.
    ///
    /// Catching them here is the difference between a Failed ledger row naming the rule and a
    /// document that reaches BizMate, is refused there for a reason nobody local can see, and looks
    /// from eSyncMate's side like it simply never arrived.
    ///
    /// **Two payload shapes, both live.** The contract's shape wraps everything in `purchaseOrder`
    /// with `lines`; the production 850 map emits eSyncMate's own flat shape with `orderNumber`,
    /// `shipToCustomer` and `items`. W3-01 renames the second into the first and is not finished, so
    /// both are real today and every accessor here reads either. This is not defensive
    /// generalisation: the first cut of these rules understood only the contract shape, which meant
    /// it refused every real document for "carries no purchaseOrder object" while looking at none of
    /// them. Reading both is what makes the rules apply to the traffic that actually exists.
    ///
    /// **What this deliberately does not do**: it does not stop the 997. The partner's document is
    /// syntactically fine and a functional acknowledgment says only that - withholding it would tell
    /// the partner their envelope was malformed, which is untrue. The right answer to "syntax fine,
    /// content refused" is an 824, which is W3-18 and not built. The 997 is generated after SaveOrder
    /// in the route, so today a refusal suppresses it as a side effect. That is a gap, not a design.
    /// </summary>
    public static class Canonical850Rules
    {
        /// <summary>Statuses that mean an order is no longer live.</summary>
        private static readonly string[] ClosedStatuses = { "CANCELLED", "CANCELED", "DELETED" };

        /// <summary>Any one of these identifies a line's item, across both payload shapes.</summary>
        private static readonly string[] ItemIdentifiers =
            { "partnerSku", "vendorSku", "upc", "gtin", "vendorStyle", "partNo" };

        /// <summary>Any one of these resolves a ship-to that is not a consumer address.</summary>
        private static readonly string[] ShipToIdentifiers = { "id", "contactId", "gln", "locationCode" };

        /// <summary>
        /// The five rules that can be read off the payload alone. The sixth - no duplicate live PO -
        /// needs the database and is <see cref="DecideOrderIntake"/>.
        /// </summary>
        public static List<IntakeViolation> Inspect(JsonElement payload)
        {
            var l_Violations = new List<IntakeViolation>();

            // The contract wraps the order; the production map is the order. Either way this is the
            // node the rules read.
            JsonElement? l_Wrapped = X12Render.Obj(payload, "purchaseOrder");
            JsonElement l_Order = l_Wrapped ?? payload;
            string l_Root = l_Wrapped.HasValue ? "$.purchaseOrder" : "$";

            ShipTo(l_Order, l_Root, l_Violations);
            Lines(l_Order, l_Root, l_Violations);

            return l_Violations;
        }

        /// <summary>The order's PO number, whichever shape it arrived in.</summary>
        public static string? PoNumber(JsonElement payload)
        {
            JsonElement l_Order = X12Render.Obj(payload, "purchaseOrder") ?? payload;

            return X12Render.Str(l_Order, "poNumber") ?? X12Render.Str(l_Order, "orderNumber");
        }

        /// <summary>
        /// Rules 1 and 2. Which one applies is decided by the order being a dropship, not guessed
        /// from whether an address happens to be present: a partner that sends a warehouse address on
        /// a non-dropship order still needs its ship-to to resolve, and a consumer delivery still
        /// needs a street even when an id came along with it.
        /// </summary>
        private static void ShipTo(JsonElement order, string root, List<IntakeViolation> violations)
        {
            // Named as the payload names it, not as the contract would: an operator who reads
            // "$.shipToCustomer.address1" can go and find that property in the file.
            string l_Field = "shipTo";
            JsonElement? l_ShipTo = X12Render.Obj(order, "shipTo");

            if (!l_ShipTo.HasValue)
            {
                l_Field = "shipToCustomer";
                l_ShipTo = X12Render.Obj(order, "shipToCustomer");
            }

            if (!l_ShipTo.HasValue)
            {
                violations.Add(new IntakeViolation
                {
                    Rule = "ShipToPresent",
                    Path = root + "." + l_Field,
                    Detail = "mandatory, and absent"
                });

                return;
            }

            JsonElement l_Party = l_ShipTo.Value;

            if (IsDropship(order))
            {
                foreach (string l_Missing in new[] { "address1", "country" })
                {
                    if (string.IsNullOrWhiteSpace(X12Render.Str(l_Party, l_Missing)))
                    {
                        violations.Add(new IntakeViolation
                        {
                            Rule = "DropshipAddress",
                            Path = root + "." + l_Field + "." + l_Missing,
                            Detail = "a dropship ship-to is a consumer address and needs both address1 and country"
                        });
                    }
                }

                return;
            }

            if (ShipToIdentifiers.All(f => string.IsNullOrWhiteSpace(X12Render.Str(l_Party, f))))
            {
                violations.Add(new IntakeViolation
                {
                    Rule = "ShipToIdentifier",
                    Path = root + "." + l_Field,
                    Detail = "a non-dropship ship-to needs one of " + string.Join(", ", ShipToIdentifiers)
                           + " to resolve against CustomerContacts; it carries none"
                });
            }
        }

        /// <summary>Rules 3, 4 and 5, per line.</summary>
        private static void Lines(JsonElement order, string root, List<IntakeViolation> violations)
        {
            string l_Name = "lines";
            IReadOnlyList<JsonElement> l_Lines = X12Render.Array(order, "lines");

            if (l_Lines.Count == 0)
            {
                l_Name = "items";
                l_Lines = X12Render.Array(order, "items");
            }

            if (l_Lines.Count == 0)
            {
                violations.Add(new IntakeViolation
                {
                    Rule = "LinesPresent",
                    Path = root + ".lines",
                    Detail = "an order with no lines is not an order"
                });

                return;
            }

            for (int i = 0; i < l_Lines.Count; i++)
            {
                JsonElement l_Line = l_Lines[i];

                // Named by the partner's own line id where there is one, because that is what a human
                // will look for in the file when they go and read it.
                string l_Where = $"{root}.{l_Name}[{i}]"
                    + (string.IsNullOrWhiteSpace(X12Render.Str(l_Line, "lineNo"))
                        ? string.Empty
                        : " (lineNo " + X12Render.Str(l_Line, "lineNo") + ")");

                if (!(Quantity(l_Line) > 0))
                {
                    violations.Add(new IntakeViolation
                    {
                        Rule = "LineQuantity",
                        Path = l_Where + ".quantity",
                        Detail = "every line quantity must be above zero; a zero-quantity line is a "
                               + "cancellation and belongs in an 860"
                    });
                }

                if (ItemIdentifiers.All(f => string.IsNullOrWhiteSpace(X12Render.Str(l_Line, f))))
                {
                    violations.Add(new IntakeViolation
                    {
                        Rule = "LineItemIdentifier",
                        Path = l_Where,
                        Detail = "a line needs at least one of " + string.Join(", ", ItemIdentifiers)
                               + " for BizMate to resolve the product; it carries none"
                    });
                }

                int l_Locations = X12Render.Array(l_Line, "locations").Count;

                if (l_Locations > 1)
                {
                    violations.Add(new IntakeViolation
                    {
                        Rule = "LineLocations",
                        Path = l_Where + ".locations",
                        Detail = $"BizMate takes at most one location per line and this carries {l_Locations}; "
                               + "an SDQ fanning one line out to several stores has to arrive as one line each"
                    });
                }
            }
        }

        /// <summary>What rule 6 decided about writing an order for this document.</summary>
        public enum OrderIntake
        {
            /// <summary>Nothing has this PO. Write the order.</summary>
            Create,

            /// <summary>We have taken this exact interchange before. Reuse the order it created.</summary>
            Reuse,

            /// <summary>A different interchange, and the PO already has a live order. Refuse.</summary>
            Refuse
        }

        /// <summary>Rule 6's answer, with what the caller needs to act on it.</summary>
        public sealed class OrderIntakeDecision
        {
            public OrderIntake Outcome { get; init; }

            /// <summary>The order to reuse. Set only for <see cref="OrderIntake.Reuse"/>.</summary>
            public int? ExistingOrderId { get; init; }

            /// <summary>Why, in words an operator can act on. Set for Refuse and Reuse.</summary>
            public string? Message { get; init; }
        }

        /// <summary>
        /// Rule 6, which needs the database: should this document create an order, reuse one, or be
        /// refused?
        ///
        /// This is NOT defensive de-duplication, which W1-15 is explicit about leaving to BizMate.
        /// BizMate's idempotency key is partner + document type + control number, so a genuine retry
        /// of the same interchange is its problem and it answers duplicate=true. What that key cannot
        /// see is a DIFFERENT interchange carrying a PO number that already has a live order - two
        /// orders for one PO, which is the thing this rule exists to stop.
        ///
        /// **Reuse is why this returns three answers rather than two (F-46).** The first version
        /// answered only "refuse" or "nothing wrong", and stepping aside for a repeat interchange -
        /// correctly, so BizMate could answer duplicate=true - also meant SaveOrder ran again and
        /// wrote a SECOND order for the same PO. It was proven on a live retry: PO
        /// TST-20260909-300002 ended with orders 660888 and 660889, the ledger knowing the second
        /// was a duplicate and the Orders table not. Deferring the BIZMATE call to BizMate is right;
        /// deferring our own order writing to it was never possible, because BizMate does not write
        /// our order rows. So a repeat now names the order it already produced and the caller reuses
        /// it, while the document still goes to BizMate exactly as before.
        /// </summary>
        public static OrderIntakeDecision DecideOrderIntake(
            string connectionString, int customerId, string? poNumber, string? partnerControlNo, long currentLedgerId)
        {
            if (string.IsNullOrWhiteSpace(poNumber) || customerId <= 0)
            {
                return new OrderIntakeDecision { Outcome = OrderIntake.Create };
            }

            string l_Po = poNumber!.Trim().Replace("'", "''");

            // A repeat of an interchange we have already taken. BizMate's idempotency owns the
            // question of whether to accept it again; ours is only whether to write another order.
            if (!string.IsNullOrWhiteSpace(partnerControlNo))
            {
                var l_Ledger = new EDILedger();

                l_Ledger.UseConnection(connectionString);

                var l_Prior = new DataTable();

                string l_PriorCriteria =
                    $"Direction = 'In' AND Id <> {currentLedgerId} AND Outcome = 'Translated' " +
                    $"AND PartnerControlNo = '{partnerControlNo!.Trim().Replace("'", "''")}'";

                if (l_Ledger.GetList(l_PriorCriteria, "Id, OrderId", ref l_Prior, "Id ASC") && l_Prior.Rows.Count > 0)
                {
                    DataRow l_First = l_Prior.Rows[0];
                    object l_OrderId = l_First["OrderId"];

                    // The earliest row is the one that created the order, so a third copy reuses the
                    // same order as the second rather than chaining off it.
                    if (l_OrderId != null && l_OrderId != DBNull.Value)
                    {
                        return new OrderIntakeDecision
                        {
                            Outcome = OrderIntake.Reuse,
                            ExistingOrderId = Convert.ToInt32(l_OrderId),
                            Message = $"A repeat of interchange [{partnerControlNo}], which eSyncMate already took as "
                                    + $"ledger {l_First["Id"]} and order {l_OrderId}. The document is sent to BizMate "
                                    + "again - their idempotency answers it - and no second order is written."
                        };
                    }

                    // Taken before but it produced no order: an 824, or a translation that made
                    // nothing. There is nothing to reuse, so treat it as new.
                    return new OrderIntakeDecision { Outcome = OrderIntake.Create };
                }
            }

            var l_Orders = new Orders();

            l_Orders.UseConnection(connectionString);

            var l_Rows = new DataTable();

            string l_Criteria =
                $"OrderNumber = '{l_Po}' AND CustomerId = {customerId} " +
                "AND (Status IS NULL OR Status NOT IN ('" + string.Join("','", ClosedStatuses) + "'))";

            if (!l_Orders.GetList(l_Criteria, "Id, Status", ref l_Rows, "Id DESC") || l_Rows.Rows.Count == 0)
            {
                return new OrderIntakeDecision { Outcome = OrderIntake.Create };
            }

            return new OrderIntakeDecision
            {
                Outcome = OrderIntake.Refuse,
                Message = $"DuplicateOrder: purchase order [{poNumber}] already has a live order "
                        + $"(order {l_Rows.Rows[0]["Id"]}, status {l_Rows.Rows[0]["Status"]}) for this customer, and this "
                        + "document is not a repeat of the interchange that created it."
            };
        }

        /// <summary>
        /// Dropship as the contract defines it: the flag, or the PO type that sets it.
        ///
        /// Worth knowing before trusting this on real traffic: the production 850 map resolves
        /// `poType` through `Transformations.orderTypeMap`, which nothing ever populates, so BEG02
        /// renders as an empty string and PruneEmpty then drops the property. On today's production
        /// payload every order therefore reads as non-dropship and rule 2 cannot fire. That is a
        /// finding about the map, not something to paper over here - the rule is written to the
        /// contract, and it starts working the moment the map carries the value.
        /// </summary>
        private static bool IsDropship(JsonElement order)
        {
            if (order.TryGetProperty("dropship", out JsonElement l_Flag) && l_Flag.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            string? l_Type = X12Render.Str(order, "poType");

            return string.Equals(l_Type, "DS", StringComparison.OrdinalIgnoreCase)
                || string.Equals(l_Type, "DropShip", StringComparison.OrdinalIgnoreCase)
                || string.Equals(l_Type, "Drop Ship", StringComparison.OrdinalIgnoreCase);
        }

        private static decimal Quantity(JsonElement line)
        {
            if (!line.TryGetProperty("quantity", out JsonElement l_Quantity))
            {
                return 0m;
            }

            return l_Quantity.ValueKind switch
            {
                JsonValueKind.Number => l_Quantity.TryGetDecimal(out decimal l_Value) ? l_Value : 0m,
                JsonValueKind.String => decimal.TryParse(l_Quantity.GetString(), out decimal l_Parsed) ? l_Parsed : 0m,
                _ => 0m
            };
        }
    }
}
