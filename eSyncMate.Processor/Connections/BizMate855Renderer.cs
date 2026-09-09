using System.Text;
using System.Text.Json;
using EdiEngine.Runtime;
using eSyncMate.Processor.Models;
using Maps_4010 = EdiEngine.Standards.X12_004010.Maps;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Canonical 855 to X12 004010 - the purchase order acknowledgment renderer (W3-26).
    ///
    /// The 855 is BizMate's one-time commitment answer to a partner's 850: what it accepted, what
    /// it changed, what it will not ship, and why. The contract states the mapping outright -
    /// "eSyncMate maps to BAK/PO1/ACK" - and names the element behind nearly every field, so this
    /// renderer follows those statements rather than a reading of the standard:
    ///
    ///   BAK02  AC / AE / RD         the order-level outcome
    ///   BAK03  poNumber             BAK08 salesOrderNo, BAK09 ackDate
    ///   ACK01  IA / IB / IR / IQ / IP / IS   the per-line outcome
    ///
    /// Four decisions worth stating.
    ///
    /// **BAK04 falls back to the acknowledgment date.** BAK01 to BAK04 are all mandatory in 004010,
    /// and BAK04 is the purchase order's own date - but the contract marks `poDate` optional while
    /// `ackDate` is required. A rejected order that arrived without a PO date would otherwise fail
    /// to render at the very moment the partner most needs to be told, so the acknowledgment date
    /// stands in. It is the closest true date we hold, and it is never invented.
    ///
    /// **The scheduled ship date uses qualifier 067, not 068.** The 865 renderer next door uses 068
    /// for what looks like the same thing. That is not an inconsistency to tidy up: the 855 schema
    /// names 067 for `scheduledShipDate` in as many words, and the contract wins over symmetry.
    ///
    /// **Reasons are written out in words, once per reason.** They are the part of a rejection the
    /// partner acts on. The canonical model carries a LIST of them at both header and line - unlike
    /// the 865's single reason - and each becomes its own N9/MSG pair rather than being flattened
    /// into one string, so a partner reading the document sees them separated as BizMate recorded
    /// them.
    ///
    /// **A rejected line still carries its quantity.** ACK02 takes `quantityAccepted` when present,
    /// which is a deliberate zero on a rejection rather than an absent element: "we accepted none
    /// of it" is the message, and an empty ACK02 would only say nothing.
    /// </summary>
    public static class BizMate855Renderer
    {
        public const string DocumentType = "855";

        /// <summary>GS01 for a purchase order acknowledgment.</summary>
        private const string FunctionalGroup = "PR";

        /// <summary>The canonical default when a line carries no unit of measure of its own.</summary>
        private const string DefaultUom = "EA";

        /// <summary>
        /// Scheduled ship date, header and line. The 855 schema names 067 outright - see the class
        /// note on why this does not follow the 865's 068.
        /// </summary>
        private const string ScheduledShipQualifier = "067";

        /// <summary>Estimated delivery, for the backorder ETA a line carries when it is short.</summary>
        private const string EstimatedDeliveryQualifier = "017";

        public static void Register()
        {
            BizMateDocumentRenderers.Register(DocumentType, Render);
        }

        public static RenderedDocument Render(OutboundRenderContext context)
        {
            JsonElement l_Ack = X12Render.Obj(context.Payload, "acknowledgment")
                ?? throw new X12RenderException("The canonical 855 carries no acknowledgment object.");

            var l_Map = new Maps_4010.M_855();
            var l_Trans = new EdiTrans(l_Map);

            string? l_PoNumber = X12Render.Str(l_Ack, "poNumber");
            string? l_AckDate = X12Render.Date8(X12Render.Str(l_Ack, "ackDate"));

            if (string.IsNullOrWhiteSpace(l_PoNumber))
            {
                throw new X12RenderException("The canonical 855 carries no poNumber; BAK03 is mandatory.");
            }

            if (string.IsNullOrWhiteSpace(l_AckDate))
            {
                throw new X12RenderException("The canonical 855 carries no usable ackDate; BAK09 is mandatory.");
            }

            // See the class note: BAK04 is mandatory and poDate is not guaranteed.
            string l_PoDate = X12Render.Date8(X12Render.Str(l_Ack, "poDate")) ?? l_AckDate;

            string? l_SalesOrderNo = X12Render.Num(l_Ack, "salesOrderNo");

            // ---- Header ------------------------------------------------------------------
            X12Render.Add(l_Trans, l_Map, "BAK",
                "00",                                          // 01 original
                AckType(X12Render.Str(l_Ack, "status")),       // 02 AC / AE / RD
                l_PoNumber,                                    // 03
                l_PoDate,                                      // 04
                null,                                          // 05 release
                null,                                          // 06 request reference
                null,                                          // 07 contract
                l_SalesOrderNo,                                // 08 BizMate SalesOrderNo
                l_AckDate);                                    // 09

            // The contract names BAK08 and REF VN for the same number. Both are written: BAK08 is
            // where it belongs structurally, REF VN is where a partner's map habitually looks.
            Ref(l_Trans, l_Map, "VN", l_SalesOrderNo, "SalesOrderNo");

            string? l_ShipDate = X12Render.Date8(X12Render.Str(l_Ack, "scheduledShipDate"));

            if (l_ShipDate is not null)
            {
                X12Render.Add(l_Trans, l_Map, "DTM", ScheduledShipQualifier, l_ShipDate);
            }

            JsonElement? l_Carrier = X12Render.Obj(l_Ack, "carrier");

            if (l_Carrier.HasValue)
            {
                string? l_Scac = X12Render.Str(l_Carrier.Value, "scac");

                if (!string.IsNullOrWhiteSpace(l_Scac))
                {
                    X12Render.Add(l_Trans, l_Map, "TD5", null, "2", l_Scac);
                }
            }

            // 91 and 92 - assigned by seller and by buyer - are the qualifiers the 856 renderer
            // already emits for these same two parties, so a partner sees one convention.
            Party(l_Trans, l_Map, "SF", X12Render.Obj(l_Ack, "shipFrom"), "91");
            Party(l_Trans, l_Map, "ST", X12Render.Obj(l_Ack, "shipTo"), "92");

            Reasons(l_Trans, l_Map, X12Render.Array(l_Ack, "reasons"));

            // ---- One PO1/ACK pair per acknowledged line ----------------------------------
            IReadOnlyList<JsonElement> l_Lines = X12Render.Array(l_Ack, "lines");

            foreach (JsonElement l_Line in l_Lines)
            {
                string? l_Uom = X12Render.Str(l_Line, "uom") ?? DefaultUom;

                X12Render.Add(l_Trans, l_Map, "PO1",
                    X12Render.Str(l_Line, "lineNo"),                        // 01
                    X12Render.Num(l_Line, "quantityOrdered"),               // 02
                    l_Uom,                                                  // 03
                    X12Render.Num(l_Line, "unitPrice"),                     // 04
                    null,                                                   // 05 basis of unit price
                    Qualifier(l_Line, "partnerSku", "BP"),                  // 06 - paired with 07
                    X12Render.Str(l_Line, "partnerSku"),
                    Qualifier(l_Line, "vendorSku", "VN"),                   // 08 - paired with 09
                    X12Render.Str(l_Line, "vendorSku"),
                    Qualifier(l_Line, "upc", "UP"),                         // 10 - paired with 11
                    X12Render.Str(l_Line, "upc"));

                // A deliberate zero on a rejection - see the class note.
                string? l_Quantity = X12Render.Num(l_Line, "quantityAccepted")
                    ?? X12Render.Num(l_Line, "quantityOrdered");

                string? l_LineShipDate = X12Render.Date8(X12Render.Str(l_Line, "scheduledShipDate"));

                X12Render.Add(l_Trans, l_Map, "ACK",
                    LineStatus(X12Render.Str(l_Line, "status")),            // 01
                    l_Quantity,                                             // 02 - paired with 03
                    l_Quantity is null ? null : l_Uom,                      // 03
                    l_LineShipDate is null ? null : ScheduledShipQualifier, // 04 - paired with 05
                    l_LineShipDate);                                        // 05

                string? l_Eta = X12Render.Date8(X12Render.Str(l_Line, "backorderEta"));

                if (l_Eta is not null)
                {
                    X12Render.Add(l_Trans, l_Map, "DTM", EstimatedDeliveryQualifier, l_Eta);
                }

                Reasons(l_Trans, l_Map, X12Render.Array(l_Line, "reasons"));
            }

            X12Render.Add(l_Trans, l_Map, "CTT", l_Lines.Count.ToString());

            // ---- Envelope ----------------------------------------------------------------
            long l_LedgerId = context.Ledger?.Id ?? 0;

            var l_Identity = X12EnvelopeIdentity.ForPartner(context.Customer, CommonUtils.ConnectionString);

            string l_Text = X12Render.Write(l_Trans, FunctionalGroup, l_Identity, l_LedgerId);
            string l_Control = X12Render.ControlNumber(l_LedgerId).ToString();

            return new RenderedDocument
            {
                Content = Encoding.UTF8.GetBytes(l_Text),
                ContentEncoding = "utf-8",
                Format = BizMateFormats.X12,
                InterchangeControlNo = l_Control,
                OrderId = BizMateRenderers.ResolveOrderId(context.Customer, l_PoNumber),
                MapName = "X12_004010.M_855",
                MapVersion = l_Identity.GsVersion,
                FileName = $"855_{context.PartnerId}_{l_Control}.edi"
            };
        }

        /// <summary>The qualifier for a product id, or null when the line does not carry that id.</summary>
        private static string? Qualifier(JsonElement line, string name, string qualifier)
        {
            return string.IsNullOrWhiteSpace(X12Render.Str(line, name)) ? null : qualifier;
        }

        /// <summary>REF, but only when there is something to put in REF02. R0203 needs 02 or 03.</summary>
        private static void Ref(EdiTrans trans, Maps_4010.M_855 map, string qualifier, string? value, string? description)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                X12Render.Add(trans, map, "REF", qualifier, X12Render.Text(value, 30), X12Render.Text(description, 80));
            }
        }

        /// <summary>
        /// Each reason as its own N9/MSG pair, in words.
        ///
        /// One pair per reason rather than one joined string: the canonical model separates them
        /// because BizMate recorded them separately, and a partner reading a rejection should see
        /// the same separation.
        /// </summary>
        private static void Reasons(EdiTrans trans, Maps_4010.M_855 map, IReadOnlyList<JsonElement> reasons)
        {
            foreach (JsonElement l_Reason in reasons)
            {
                string? l_Code = X12Render.Str(l_Reason, "code");
                string? l_Text = X12Render.Str(l_Reason, "text");

                if (string.IsNullOrWhiteSpace(l_Code) && string.IsNullOrWhiteSpace(l_Text))
                {
                    continue;
                }

                X12Render.Add(trans, map, "N9", "ZZ", X12Render.Text(l_Code ?? "Reason", 30));

                if (!string.IsNullOrWhiteSpace(l_Text))
                {
                    X12Render.Add(trans, map, "MSG", X12Render.Text(l_Text, 264));
                }
            }
        }

        /// <summary>
        /// A party as N1 and the segments that hang off it.
        ///
        /// The identifier falls back through the several names the canonical model uses for one,
        /// because a party with an address but no id is still worth naming and a party with only an
        /// id - which is how BizMate stages a resolved ship-to - is still a party.
        /// </summary>
        private static void Party(EdiTrans trans, Maps_4010.M_855 map, string entityQualifier, JsonElement? party, string defaultIdQualifier)
        {
            if (!party.HasValue)
            {
                return;
            }

            JsonElement l_Party = party.Value;

            string? l_Name = X12Render.Text(X12Render.Str(l_Party, "name"), 60);
            string? l_Gln = X12Render.Str(l_Party, "gln");

            string? l_Id = X12Render.Str(l_Party, "id")
                ?? l_Gln
                ?? X12Render.Str(l_Party, "locationCode")
                ?? X12Render.Str(l_Party, "contactId");

            // N1's R0203 wants a name or an identifier; with neither there is no party to name.
            if (string.IsNullOrWhiteSpace(l_Name) && string.IsNullOrWhiteSpace(l_Id))
            {
                return;
            }

            string? l_IdQualifier = X12Render.Str(l_Party, "idQualifier")
                ?? (l_Gln is not null && l_Id == l_Gln ? "UL" : defaultIdQualifier);

            X12Render.Add(trans, map, "N1",
                entityQualifier,
                l_Name,
                l_Id is null ? null : l_IdQualifier,   // 03 - paired with 04
                l_Id);

            string? l_Address1 = X12Render.Text(X12Render.Str(l_Party, "address1"), 55);

            if (!string.IsNullOrWhiteSpace(l_Address1))
            {
                X12Render.Add(trans, map, "N3", l_Address1, X12Render.Text(X12Render.Str(l_Party, "address2"), 55));
            }

            X12Render.Add(trans, map, "N4",
                X12Render.Text(X12Render.Str(l_Party, "city"), 30),
                X12Render.Str(l_Party, "state"),
                X12Render.Str(l_Party, "zip"),
                X12Render.Str(l_Party, "country"));
        }

        /// <summary>
        /// Order-level outcome to BAK02, exactly as the contract states it: AC / AE / RD.
        /// Anything unrecognised is a refusal to guess.
        /// </summary>
        private static string AckType(string? status)
        {
            return status switch
            {
                "Accepted" => "AC",
                "AcceptedWithChanges" => "AE",
                "Rejected" => "RD",
                _ => throw new X12RenderException(
                    $"'{status}' is not an 855 status. The contract allows Accepted, AcceptedWithChanges and Rejected.")
            };
        }

        /// <summary>Per-line outcome to ACK01, again as the contract states: IA / IB / IR / IQ / IP / IS.</summary>
        private static string LineStatus(string? status)
        {
            return status switch
            {
                "Accepted" => "IA",
                "Backordered" => "IB",
                "Rejected" => "IR",
                "QuantityChanged" => "IQ",
                "PriceChanged" => "IP",
                "Substituted" => "IS",
                _ => throw new X12RenderException(
                    $"'{status}' is not an 855 line status. The contract allows Accepted, Backordered, " +
                    "Rejected, QuantityChanged, PriceChanged and Substituted.")
            };
        }
    }
}
