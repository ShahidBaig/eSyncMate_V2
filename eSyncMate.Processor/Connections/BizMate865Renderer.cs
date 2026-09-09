using System.Text;
using System.Text.Json;
using EdiEngine.Runtime;
using eSyncMate.Processor.Models;
using Maps_4010 = EdiEngine.Standards.X12_004010.Maps;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Canonical 865 to X12 004010 (W3-11 outbound half).
    ///
    /// The 865 is BizMate's answer to a partner's 860: one result per requested change, with the
    /// reason on every refusal so the partner is told WHY rather than just no. The contract states
    /// the mapping outright - "eSyncMate maps to BCA/POC/ACK" - and names each element, so this
    /// renderer follows those statements rather than a reading of the standard:
    ///
    ///   BCA02  AC / AE / RD        the overall outcome across all requested changes
    ///   BCA03  poNumber            BCA05 changeSequence, BCA06 ackDate
    ///   ACK01  IA / IC / IR        the per-line outcome
    ///
    /// Two decisions worth stating, because both are places a naive mapping goes wrong.
    ///
    /// **The acknowledged quantity goes in ACK02, not POC03.** POC's syntax note C030405 says that
    /// if POC03 is present then POC04 - quantity left to receive - and POC05 are required too, and
    /// BizMate does not send a quantity left to receive. Writing a zero there would be inventing a
    /// number the partner would believe. ACK02 is the acknowledgment's own quantity field and needs
    /// nothing we do not have. And the unit price is POC06, not POC05: POC05 is the composite
    /// unit of measure, which the engine models as a composite element and nothing here fills.
    ///
    /// **DateChange and Reschedule both render as RZ.** X12 004010 element 670 has one code for
    /// "the dates moved" and the canonical model has two names for it. The distinction is not
    /// expressible, so it is collapsed deliberately here rather than lost silently somewhere else.
    /// </summary>
    public static class BizMate865Renderer
    {
        public const string DocumentType = "865";

        /// <summary>GS01 for a purchase order change acknowledgment.</summary>
        private const string FunctionalGroup = "CH";

        /// <summary>The canonical default when a line carries no unit of measure of its own.</summary>
        private const string DefaultUom = "EA";

        public static void Register()
        {
            BizMateDocumentRenderers.Register(DocumentType, Render);
        }

        public static RenderedDocument Render(OutboundRenderContext context)
        {
            JsonElement l_Ack = X12Render.Obj(context.Payload, "acknowledgment")
                ?? throw new X12RenderException("The canonical 865 carries no acknowledgment object.");

            var l_Map = new Maps_4010.M_865();
            var l_Trans = new EdiTrans(l_Map);

            string? l_PoNumber = X12Render.Str(l_Ack, "poNumber");
            string? l_AckDate = X12Render.Date8(X12Render.Str(l_Ack, "ackDate"));

            if (string.IsNullOrWhiteSpace(l_PoNumber))
            {
                throw new X12RenderException("The canonical 865 carries no poNumber; BCA03 is mandatory.");
            }

            if (string.IsNullOrWhiteSpace(l_AckDate))
            {
                throw new X12RenderException("The canonical 865 carries no usable ackDate; BCA06 is mandatory.");
            }

            // ---- Header ------------------------------------------------------------------
            X12Render.Add(l_Trans, l_Map, "BCA",
                "00",                                        // 01 original
                AckType(X12Render.Str(l_Ack, "result")),     // 02 AC / AE / RD
                l_PoNumber,                                  // 03
                null,                                        // 04 release
                X12Render.Str(l_Ack, "changeSequence"),      // 05
                l_AckDate);                                  // 06

            Ref(l_Trans, l_Map, "VN", X12Render.Str(l_Ack, "salesOrderNo"), "SalesOrderNo");

            // ---- Header-level change results ---------------------------------------------
            // These have no acknowledgment segment of their own in the 865 - ACK lives inside the
            // POC loop - so each is carried as an N9/MSG note, which is lossless and readable.
            // Dropping them would be the alternative, and the whole point of the document is to
            // tell the partner what happened to every change they asked for.
            foreach (JsonElement l_Result in X12Render.Array(l_Ack, "headerResults"))
            {
                string? l_Change = X12Render.Str(l_Result, "change");

                if (string.IsNullOrWhiteSpace(l_Change))
                {
                    continue;
                }

                X12Render.Add(l_Trans, l_Map, "N9", "ZZ", X12Render.Text(l_Change, 30));
                Message(l_Trans, l_Map, X12Render.Str(l_Result, "result"), X12Render.Obj(l_Result, "reason"));
            }

            // ---- One POC loop per requested line change ----------------------------------
            IReadOnlyList<JsonElement> l_Lines = X12Render.Array(l_Ack, "lines");

            foreach (JsonElement l_Line in l_Lines)
            {
                string? l_LineNo = X12Render.Str(l_Line, "lineNo");

                X12Render.Add(l_Trans, l_Map, "POC",
                    l_LineNo,                                              // 01
                    ChangeType(X12Render.Str(l_Line, "changeType")),       // 02
                    null, null,                                            // 03/04 - see the class note
                    null,                                                  // 05 composite unit of measure
                    X12Render.Num(l_Line, "unitPrice"));                   // 06

                // The quantity BizMate settled on, falling back to what was asked for when the
                // change was refused: on a refusal the requested figure is the one the partner
                // needs to see reflected back.
                string? l_Quantity = X12Render.Num(l_Line, "quantityAccepted")
                    ?? X12Render.Num(l_Line, "quantityRequested");

                string? l_ShipDate = X12Render.Date8(X12Render.Str(l_Line, "scheduledShipDate"));

                X12Render.Add(l_Trans, l_Map, "ACK",
                    LineStatus(X12Render.Str(l_Line, "result")),           // 01
                    l_Quantity,                                            // 02
                    l_Quantity is null ? null : DefaultUom,                // 03 - paired with 02
                    l_ShipDate is null ? null : "068",                     // 04 current schedule ship
                    l_ShipDate);                                           // 05

                Message(l_Trans, l_Map, null, X12Render.Obj(l_Line, "reason"));
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
                MapName = "X12_004010.M_865",
                MapVersion = l_Identity.GsVersion,
                FileName = $"865_{context.PartnerId}_{l_Control}.edi"
            };
        }

        /// <summary>REF, but only when there is something to put in REF02. R0203 needs 02 or 03.</summary>
        private static void Ref(EdiTrans trans, Maps_4010.M_865 map, string qualifier, string? value, string? description)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                X12Render.Add(trans, map, "REF", qualifier, X12Render.Text(value, 30), X12Render.Text(description, 80));
            }
        }

        /// <summary>
        /// The outcome and, where there is one, the reason - as one free-form line.
        ///
        /// The reason is the part of this document the partner actually acts on, so it is written
        /// out in words rather than reduced to a code they would have to look up.
        /// </summary>
        private static void Message(EdiTrans trans, Maps_4010.M_865 map, string? result, JsonElement? reason)
        {
            var l_Parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(result))
            {
                l_Parts.Add(result!);
            }

            if (reason.HasValue)
            {
                string? l_Code = X12Render.Str(reason.Value, "code");
                string? l_Text = X12Render.Str(reason.Value, "text");

                if (!string.IsNullOrWhiteSpace(l_Code))
                {
                    l_Parts.Add(l_Code!);
                }

                if (!string.IsNullOrWhiteSpace(l_Text))
                {
                    l_Parts.Add(l_Text!);
                }
            }

            if (l_Parts.Count == 0)
            {
                return;
            }

            X12Render.Add(trans, map, "MSG", X12Render.Text(string.Join(": ", l_Parts), 264));
        }

        /// <summary>
        /// Overall outcome to BCA02, exactly as the contract states it: AC / AE / RD.
        ///
        /// Note that X12 reads AC as "acknowledge with detail and change" rather than "accepted";
        /// the contract's mapping is what BizMate and the partner have agreed, so it wins over the
        /// dictionary. Anything unrecognised is a refusal to guess.
        /// </summary>
        private static string AckType(string? result)
        {
            return result switch
            {
                "Accepted" => "AC",
                "AcceptedWithChanges" => "AE",
                "Rejected" => "RD",
                _ => throw new X12RenderException(
                    $"'{result}' is not an 865 result. The contract allows Accepted, AcceptedWithChanges and Rejected.")
            };
        }

        /// <summary>Per-line outcome to ACK01: IA / IC / IR, again as the contract states.</summary>
        private static string LineStatus(string? result)
        {
            return result switch
            {
                "Accepted" => "IA",
                "AcceptedWithChange" => "IC",
                "Rejected" => "IR",
                _ => throw new X12RenderException(
                    $"'{result}' is not an 865 line result. The contract allows Accepted, AcceptedWithChange and Rejected.")
            };
        }

        /// <summary>
        /// Requested change to POC02 (element 670).
        ///
        /// DateChange and Reschedule share RZ because 004010 has one code for a date move. Said out
        /// loud here so nobody later reads it as an oversight.
        /// </summary>
        private static string ChangeType(string? changeType)
        {
            return changeType switch
            {
                "QuantityIncrease" => "QI",
                "QuantityDecrease" => "QD",
                "CancelLine" => "DI",
                "AddLine" => "AI",
                "PriceChange" => "PC",
                "DateChange" => "RZ",
                "Reschedule" => "RZ",
                _ => throw new X12RenderException(
                    $"'{changeType}' is not an 865 line changeType. The contract allows QuantityIncrease, " +
                    "QuantityDecrease, CancelLine, AddLine, PriceChange, DateChange and Reschedule.")
            };
        }
    }
}
