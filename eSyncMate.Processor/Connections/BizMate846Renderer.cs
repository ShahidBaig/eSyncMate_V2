using System.Text;
using System.Text.Json;
using EdiEngine.Runtime;
using eSyncMate.Processor.Models;
using Maps_4010 = EdiEngine.Standards.X12_004010.Maps;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Canonical 846 to X12 004010 - the inventory advice renderer (W3-27).
    ///
    /// The 846 publishes AVAILABLE-TO-SELL, never on-hand: BizMate has already subtracted its
    /// buffers and applied its caps, so every quantity here is passed through exactly as staged.
    /// Recomputing anything at render time would put a number on the wire that BizMate cannot
    /// account for, and "why did we tell them three" is answered by `feedRunNo`, which rides in
    /// BIA03 for exactly that reason.
    ///
    /// The contract names most of the mapping - BIA01/03/04, LIN VN/IN/UP, QTY 33, QTY 20, DTM 018
    /// - and this renderer follows those statements. Five decisions it does NOT name are made here,
    /// and all five are stated out loud because each is a place a partner could be misled.
    ///
    /// **BIA02 is "DD".** The report type code is mandatory in 004010 and the contract is silent on
    /// it. DD - distributor inventory report - is what a supplier's inventory advice conventionally
    /// carries. It is the one value in this document a partner guideline is likely to override, so
    /// it is a named constant rather than a literal buried in the segment.
    ///
    /// **An empty feed is refused, not sent.** A Full run with no items is not an inventory advice
    /// that says nothing; it is an advice a partner cannot read, because nothing distinguishes "no
    /// items were in scope" from "every item went to zero". It fails visibly with its reason on the
    /// ledger and stays Staged at BizMate, which is what W2-08 is for.
    ///
    /// **A non-Active item carries its status in words.** The contract sends Discontinued and
    /// Inactive items with quantity zero rather than omitting them - but an Active item is very
    /// often zero too (766 of the 996 staged availability entries are), so zero alone cannot tell a
    /// partner "discontinued" from "back in October". The status rides a REF ZZ inside the item
    /// loop rather than being dropped on the floor.
    ///
    /// **A per-location quantity becomes SDQ, qualified 91.** Aggregate grain writes a plain QTY;
    /// PerLocation and SelectedLocations write one SDQ per location. 91 is "assigned by seller",
    /// which is what a BizMate `InventoryLocations` code is, and it matches the qualifier the 855
    /// and 856 renderers already use for the seller's own ship-from location.
    ///
    /// **`leadTimeDays` refuses rather than guesses.** The contract names the LDT segment but not
    /// the LDT01 lead time code, and 004010 offers dozens. No staged document has ever carried the
    /// field, so rather than invent a code a partner would act on, this throws if one ever turns
    /// up. Loud beats wrong on a number that drives a buyer's ordering.
    /// </summary>
    public static class BizMate846Renderer
    {
        public const string DocumentType = "846";

        /// <summary>GS01 for an inventory inquiry / advice.</summary>
        private const string FunctionalGroup = "IB";

        /// <summary>
        /// BIA02, the report type. Not named by the contract - see the class note. The likeliest
        /// per-partner override in this document, so it lives here rather than inline.
        /// </summary>
        private const string ReportType = "DD";

        /// <summary>The canonical default when an item carries no unit of measure of its own.</summary>
        private const string DefaultUom = "EA";

        /// <summary>QTY 33, available to sell - the quantity this whole document exists to carry.</summary>
        private const string AvailableQualifier = "33";

        /// <summary>QTY 20, the quantity arriving on the availability date.</summary>
        private const string EtaQuantityQualifier = "20";

        /// <summary>DTM 018, the next availability date.</summary>
        private const string AvailableDateQualifier = "018";

        /// <summary>Identification code qualifier 91 - assigned by seller - for a BizMate location.</summary>
        private const string SellerLocationQualifier = "91";

        public static void Register()
        {
            BizMateDocumentRenderers.Register(DocumentType, Render);
        }

        public static RenderedDocument Render(OutboundRenderContext context)
        {
            JsonElement l_Feed = X12Render.Obj(context.Payload, "feed")
                ?? throw new X12RenderException("The canonical 846 carries no feed object.");

            var l_Map = new Maps_4010.M_846();
            var l_Trans = new EdiTrans(l_Map);

            string? l_FeedRunNo = X12Render.Num(l_Feed, "feedRunNo");
            string? l_GeneratedAt = X12Render.Str(l_Feed, "generatedAt");
            string? l_Date = X12Render.Date8(l_GeneratedAt);

            if (string.IsNullOrWhiteSpace(l_FeedRunNo))
            {
                throw new X12RenderException("The canonical 846 carries no feedRunNo; BIA03 is mandatory.");
            }

            if (string.IsNullOrWhiteSpace(l_Date))
            {
                throw new X12RenderException("The canonical 846 carries no usable generatedAt; BIA04 is mandatory.");
            }

            IReadOnlyList<JsonElement> l_Items = X12Render.Array(l_Feed, "items");

            // See the class note: an advice with nothing in it is unreadable, not harmless.
            if (l_Items.Count == 0)
            {
                throw new X12RenderException(
                    $"Feed run {l_FeedRunNo} carries no items. An inventory advice with no items cannot be read - "
                    + "nothing in it distinguishes 'no item was in scope' from 'every item went to zero' - so it is "
                    + "refused rather than sent.");
            }

            // ---- Header ------------------------------------------------------------------
            X12Render.Add(l_Trans, l_Map, "BIA",
                Purpose(X12Render.Str(l_Feed, "mode")),      // 01 00 original / 04 change
                ReportType,                                   // 02
                l_FeedRunNo,                                  // 03 the audit key
                l_Date,                                       // 04
                X12Render.Time4(l_GeneratedAt));              // 05

            Ref(l_Trans, l_Map, X12Render.Num(l_Feed, "sequence"), "Sequence");
            Ref(l_Trans, l_Map, X12Render.Num(l_Feed, "baselineFeedRunNo"), "BaselineFeedRunNo");

            Supplier(l_Trans, l_Map, X12Render.Obj(l_Feed, "supplier"));

            // ---- One LIN loop per item ---------------------------------------------------
            int l_Line = 0;

            foreach (JsonElement l_Item in l_Items)
            {
                string? l_VendorSku = X12Render.Str(l_Item, "vendorSku");

                if (string.IsNullOrWhiteSpace(l_VendorSku))
                {
                    throw new X12RenderException(
                        $"Item {l_Line + 1} of feed run {l_FeedRunNo} carries no vendorSku, which the contract states "
                        + "is always present and is the stable key - UPCs are reused over time in BizMate.");
                }

                l_Line++;

                X12Render.Add(l_Trans, l_Map, "LIN",
                    l_Line.ToString(),                                   // 01
                    "VN", l_VendorSku,                                   // 02/03
                    Qualifier(l_Item, "partnerSku", "IN"),               // 04/05
                    X12Render.Str(l_Item, "partnerSku"),
                    Qualifier(l_Item, "upc", "UP"),                      // 06/07
                    X12Render.Str(l_Item, "upc"));

                string? l_Description = X12Render.Text(X12Render.Str(l_Item, "description"), 80);

                if (!string.IsNullOrWhiteSpace(l_Description))
                {
                    X12Render.Add(l_Trans, l_Map, "PID", "F", null, null, null, l_Description);
                }

                // Not Active is worth saying in words - see the class note.
                string? l_Status = X12Render.Str(l_Item, "status");

                if (!string.IsNullOrWhiteSpace(l_Status) && l_Status != "Active")
                {
                    Ref(l_Trans, l_Map, l_Status, "ItemStatus");
                }

                string l_Uom = X12Render.Str(l_Item, "uom") ?? DefaultUom;

                foreach (JsonElement l_Availability in X12Render.Array(l_Item, "availability"))
                {
                    Availability(l_Trans, l_Map, l_Availability, l_Uom, l_VendorSku!);
                }
            }

            X12Render.Add(l_Trans, l_Map, "CTT", l_Line.ToString());

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

                // An inventory feed belongs to no order, so it is never linked to OutboundEDI: that
                // table's orderId is NOT NULL (F-22). The artifact alone is the record.
                OrderId = null,

                MapName = "X12_004010.M_846",
                MapVersion = l_Identity.GsVersion,
                FileName = $"846_{context.PartnerId}_{l_Control}.edi"
            };
        }

        /// <summary>
        /// One availability entry: the quantity, where it sits, and when more arrives.
        ///
        /// Aggregate grain has no location and writes a plain QTY; a per-location entry writes SDQ
        /// instead, so the partner sees the breakdown rather than a total it cannot decompose.
        /// </summary>
        private static void Availability(
            EdiTrans trans, Maps_4010.M_846 map, JsonElement availability, string uom, string vendorSku)
        {
            if (X12Render.Num(availability, "leadTimeDays") is not null)
            {
                throw new X12RenderException(
                    $"Item {vendorSku} carries leadTimeDays, and the LDT01 lead time code for it has not been agreed "
                    + "with the partner. Refused rather than guessed: the code drives when a buyer reorders.");
            }

            string? l_Quantity = X12Render.Num(availability, "availableQty");
            string? l_Location = X12Render.Str(availability, "locationCode");

            if (l_Location is null)
            {
                X12Render.Add(trans, map, "QTY", AvailableQualifier, l_Quantity);
            }
            else
            {
                X12Render.Add(trans, map, "SDQ", uom, SellerLocationQualifier, l_Location, l_Quantity);
            }

            string? l_Eta = X12Render.Date8(X12Render.Str(availability, "etaDate"));

            if (l_Eta is not null)
            {
                X12Render.Add(trans, map, "DTM", AvailableDateQualifier, l_Eta);
            }

            string? l_EtaQuantity = X12Render.Num(availability, "etaQty");

            if (l_EtaQuantity is not null)
            {
                X12Render.Add(trans, map, "QTY", EtaQuantityQualifier, l_EtaQuantity);
            }
        }

        /// <summary>The supplier as N1 SU and its address, when the feed names one.</summary>
        private static void Supplier(EdiTrans trans, Maps_4010.M_846 map, JsonElement? supplier)
        {
            if (!supplier.HasValue)
            {
                return;
            }

            JsonElement l_Supplier = supplier.Value;

            string? l_Name = X12Render.Text(X12Render.Str(l_Supplier, "name"), 60);
            string? l_Id = X12Render.Str(l_Supplier, "id")
                ?? X12Render.Str(l_Supplier, "gln")
                ?? X12Render.Str(l_Supplier, "locationCode");

            // N1's R0203 wants a name or an identifier; with neither there is no party to name.
            if (string.IsNullOrWhiteSpace(l_Name) && string.IsNullOrWhiteSpace(l_Id))
            {
                return;
            }

            X12Render.Add(trans, map, "N1",
                "SU",
                l_Name,
                l_Id is null ? null : SellerLocationQualifier,   // 03 - paired with 04
                l_Id);

            string? l_Address1 = X12Render.Text(X12Render.Str(l_Supplier, "address1"), 55);

            if (!string.IsNullOrWhiteSpace(l_Address1))
            {
                X12Render.Add(trans, map, "N3", l_Address1, X12Render.Text(X12Render.Str(l_Supplier, "address2"), 55));
            }

            X12Render.Add(trans, map, "N4",
                X12Render.Text(X12Render.Str(l_Supplier, "city"), 30),
                X12Render.Str(l_Supplier, "state"),
                X12Render.Str(l_Supplier, "zip"),
                X12Render.Str(l_Supplier, "country"));
        }

        /// <summary>The qualifier for a product id, or null when the item does not carry that id.</summary>
        private static string? Qualifier(JsonElement item, string name, string qualifier)
        {
            return string.IsNullOrWhiteSpace(X12Render.Str(item, name)) ? null : qualifier;
        }

        /// <summary>REF ZZ, mutually defined, with what it is spelled out in REF03.</summary>
        private static void Ref(EdiTrans trans, Maps_4010.M_846 map, string? value, string description)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                X12Render.Add(trans, map, "REF", "ZZ", X12Render.Text(value, 30), description);
            }
        }

        /// <summary>
        /// Feed mode to BIA01, as the contract states it: a Full run is an original, a Differential
        /// is a change. Anything unrecognised is a refusal to guess - sending a differential as a
        /// full feed would tell the partner every item it did not hear about had gone to zero.
        /// </summary>
        private static string Purpose(string? mode)
        {
            return mode switch
            {
                "Full" => "00",
                "Differential" => "04",
                _ => throw new X12RenderException(
                    $"'{mode}' is not an 846 feed mode. The contract allows Full and Differential.")
            };
        }
    }
}
