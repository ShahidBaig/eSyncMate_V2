using System.Text;
using System.Text.Json;
using EdiEngine.Runtime;
using eSyncMate.Processor.Models;
using Maps_4010 = EdiEngine.Standards.X12_004010.Maps;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Canonical 856 to X12 004010 (W3-05).
    ///
    /// The ASN is the one outbound document whose shape is a tree, and the single thing to get right
    /// about the engine is that the tree is NOT built out of nested objects. Every segment goes into
    /// <c>EdiTrans.Content</c> as one flat list, in order, and the hierarchy exists only in the HL01
    /// and HL02 values: HL01 is this level's number, HL02 is its parent's. That is how
    /// OrderManager.AddASNSegments already emits the existing ASNs, and the writer does nothing else
    /// with structure. Get the numbering right and the levels are right.
    ///
    ///   S  shipment   carrier, dates, ship-from and ship-to, totals
    ///   O  order      one per PO in the shipment
    ///   T  pallet     only when structure is SOTPI
    ///   P  pack       the carton, its licence plate and its tracking number
    ///   I  item       what is actually inside
    ///
    /// BSN05 declares which of those the partner should expect - 0001 SOPI, 0002 SOTPI, 0004 SOI -
    /// so it is derived from what the document really contains rather than echoed blindly: a
    /// structure code that disagrees with the segments is worse than no code, because the partner's
    /// parser trusts it.
    ///
    /// Where the canonical model carries something 004010 has no field for - a carrier service
    /// level, a VAT number - it is written as a qualified REF rather than dropped. Losing a field
    /// silently is the failure mode this whole integration is built to avoid.
    /// </summary>
    public static class BizMate856Renderer
    {
        public const string DocumentType = "856";

        /// <summary>GS01 for a ship notice.</summary>
        private const string FunctionalGroup = "SH";

        private const string DefaultUom = "EA";

        /// <summary>Gross weight. The canonical model carries one weight per level and it is gross.</summary>
        private const string GrossWeight = "G";

        public static void Register()
        {
            BizMateDocumentRenderers.Register(DocumentType, Render);
        }

        public static RenderedDocument Render(OutboundRenderContext context)
        {
            JsonElement l_Shipment = X12Render.Obj(context.Payload, "shipment")
                ?? throw new X12RenderException("The canonical 856 carries no shipment object.");

            var l_Map = new Maps_4010.M_856();
            var l_Trans = new EdiTrans(l_Map);

            string? l_ShipmentNo = X12Render.Str(l_Shipment, "shipmentNo");
            string? l_ShipDate = X12Render.Str(l_Shipment, "shipDate");
            string? l_ShipDate8 = X12Render.Date8(l_ShipDate);

            if (string.IsNullOrWhiteSpace(l_ShipmentNo))
            {
                throw new X12RenderException("The canonical 856 carries no shipmentNo; BSN02 is mandatory.");
            }

            if (string.IsNullOrWhiteSpace(l_ShipDate8))
            {
                throw new X12RenderException("The canonical 856 carries no usable shipDate; BSN03 is mandatory.");
            }

            IReadOnlyList<JsonElement> l_Orders = X12Render.Array(l_Shipment, "orders");

            if (l_Orders.Count == 0)
            {
                throw new X12RenderException("The canonical 856 carries no orders; an ASN with no order level is not a document the partner can post.");
            }

            // What the document actually contains decides BSN05, not what it claims to contain.
            bool l_HasPallets = l_Orders.Any(o => X12Render.Array(o, "pallets").Count > 0);
            bool l_HasPackages = l_Orders.Any(o => X12Render.Array(o, "packages").Count > 0);
            string l_Structure = l_HasPallets ? "0002" : l_HasPackages ? "0001" : "0004";

            // ---- Header -------------------------------------------------------------------
            X12Render.Add(l_Trans, l_Map, "BSN",
                Purpose(X12Render.Str(l_Shipment, "purpose")),   // 01
                l_ShipmentNo,                                    // 02
                l_ShipDate8,                                     // 03
                X12Render.Time4(l_ShipDate) ?? "0000",           // 04 - mandatory, so midnight when the date carries no time
                l_Structure);                                    // 05

            int l_Hl = 0;

            // ---- S: the shipment ----------------------------------------------------------
            int l_ShipmentHl = ++l_Hl;

            X12Render.Add(l_Trans, l_Map, "HL", l_ShipmentHl.ToString(), null, "S", "1");

            // Segment order inside an HL follows the map's own L_HL order - TD1, TD5, REF, DTM,
            // then the N1 loop - because a partner's parser walks the loop in that order and a
            // segment out of place reads as a segment in the wrong loop.
            Totals(l_Trans, l_Map, l_Shipment, l_HasPallets);
            Carrier(l_Trans, l_Map, X12Render.Obj(l_Shipment, "carrier"));

            Ref(l_Trans, l_Map, "BM", X12Render.Str(l_Shipment, "billOfLading"), null);
            Ref(l_Trans, l_Map, "CN", X12Render.Str(l_Shipment, "proNumber"), null);
            Ref(l_Trans, l_Map, "2I", X12Render.Str(l_Shipment, "masterTrackingNo"), null);
            Ref(l_Trans, l_Map, "ZZ", X12Render.Str(l_Shipment, "totalPieces"), "TotalPieces");
            References(l_Trans, l_Map, l_Shipment);

            X12Render.Add(l_Trans, l_Map, "DTM", "011", l_ShipDate8, X12Render.Time4(l_ShipDate));

            string? l_Eta = X12Render.Date8(X12Render.Str(l_Shipment, "estimatedDeliveryDate"));

            if (l_Eta is not null)
            {
                X12Render.Add(l_Trans, l_Map, "DTM", "017", l_Eta);
            }

            Party(l_Trans, l_Map, "SF", X12Render.Obj(l_Shipment, "shipFrom"), "91");
            Party(l_Trans, l_Map, "ST", X12Render.Obj(l_Shipment, "shipTo"), "92");

            // ---- O / T / P / I ------------------------------------------------------------
            foreach (JsonElement l_Order in l_Orders)
            {
                int l_OrderHl = ++l_Hl;

                X12Render.Add(l_Trans, l_Map, "HL", l_OrderHl.ToString(), l_ShipmentHl.ToString(), "O", "1");

                string? l_PoNumber = X12Render.Str(l_Order, "poNumber");

                if (string.IsNullOrWhiteSpace(l_PoNumber))
                {
                    throw new X12RenderException("An order in the canonical 856 carries no poNumber; PRF01 is mandatory.");
                }

                X12Render.Add(l_Trans, l_Map, "PRF",
                    l_PoNumber, null, null, X12Render.Date8(X12Render.Str(l_Order, "poDate")));

                Ref(l_Trans, l_Map, "VN", X12Render.Str(l_Order, "salesOrderNo"), "SalesOrderNo");
                Ref(l_Trans, l_Map, "CO", X12Render.Str(l_Order, "consignmentNo"), "ConsignmentNo");
                Ref(l_Trans, l_Map, "IV", X12Render.Str(l_Order, "invoiceNo"), null);
                References(l_Trans, l_Map, l_Order);

                // One shipment can carry orders for different store locations; the order's own
                // ship-to overrides the shipment's when it is there.
                Party(l_Trans, l_Map, "ST", X12Render.Obj(l_Order, "shipTo"), "92");

                IReadOnlyList<JsonElement> l_Pallets = X12Render.Array(l_Order, "pallets");
                IReadOnlyList<JsonElement> l_Packages = X12Render.Array(l_Order, "packages");

                foreach (JsonElement l_Pallet in l_Pallets)
                {
                    int l_PalletHl = ++l_Hl;

                    X12Render.Add(l_Trans, l_Map, "HL", l_PalletHl.ToString(), l_OrderHl.ToString(), "T", "1");

                    Weight(l_Trans, l_Map, "PLT", null, X12Render.Obj(l_Pallet, "weight"));
                    Man(l_Trans, l_Map, "GM", X12Render.Str(l_Pallet, "sscc"));

                    foreach (JsonElement l_Package in X12Render.Array(l_Pallet, "packages"))
                    {
                        Package(l_Trans, l_Map, l_Package, l_PalletHl, ref l_Hl);
                    }
                }

                // Cartons not on a pallet - every carton under SOPI, the loose ones under SOTPI.
                foreach (JsonElement l_Package in l_Packages)
                {
                    Package(l_Trans, l_Map, l_Package, l_OrderHl, ref l_Hl);
                }

                // SOI, or an order whose cartons BizMate has not broken out: the lines hang off the
                // order itself. Under SOPI the same lines are already carried inside the cartons,
                // so emitting them here too would double the ASN's quantities.
                if (l_Pallets.Count == 0 && l_Packages.Count == 0)
                {
                    foreach (JsonElement l_Line in X12Render.Array(l_Order, "lines"))
                    {
                        Item(l_Trans, l_Map, l_Line, l_OrderHl, ref l_Hl);
                    }
                }
            }

            X12Render.Add(l_Trans, l_Map, "CTT", l_Hl.ToString());

            // ---- Envelope -----------------------------------------------------------------
            long l_LedgerId = context.Ledger?.Id ?? 0;

            var l_Identity = X12EnvelopeIdentity.ForPartner(
                context.Customer, CommonUtils.ConnectionString, NullIfBlank(context.Customer?.ISA856ReceiverId));

            string l_Text = X12Render.Write(l_Trans, FunctionalGroup, l_Identity, l_LedgerId);
            string l_Control = X12Render.ControlNumber(l_LedgerId).ToString();

            return new RenderedDocument
            {
                Content = Encoding.UTF8.GetBytes(l_Text),
                ContentEncoding = "utf-8",
                Format = BizMateFormats.X12,
                InterchangeControlNo = l_Control,
                OrderId = BizMateRenderers.ResolveOrderId(context.Customer, X12Render.Str(l_Orders[0], "poNumber")),
                MapName = "X12_004010.M_856",
                MapVersion = l_Identity.GsVersion,
                FileName = $"856_{context.PartnerId}_{l_Control}.edi"
            };
        }

        /// <summary>P: one carton, its licence plate, its tracking number and what is inside it.</summary>
        private static void Package(EdiTrans trans, Maps_4010.M_856 map, JsonElement package, int parentHl, ref int hl)
        {
            int l_PackageHl = ++hl;

            X12Render.Add(trans, map, "HL", l_PackageHl.ToString(), parentHl.ToString(), "P", "1");

            Weight(trans, map, Packaging(X12Render.Str(package, "packagingType")), "1", X12Render.Obj(package, "weight"));

            Carrier(trans, map, X12Render.Obj(package, "carrier"));

            Ref(trans, map, "PK", X12Render.Str(package, "packageNo"), "PackageNo");
            Dimensions(trans, map, X12Render.Obj(package, "dimensions"));

            Man(trans, map, "GM", X12Render.Str(package, "sscc"));
            Man(trans, map, "CP", X12Render.Str(package, "trackingNo"));

            foreach (JsonElement l_Item in X12Render.Array(package, "items"))
            {
                Item(trans, map, l_Item, l_PackageHl, ref hl);
            }
        }

        /// <summary>I: the line inside a carton, or under the order directly for a flat SOI ASN.</summary>
        private static void Item(EdiTrans trans, Maps_4010.M_856 map, JsonElement line, int parentHl, ref int hl)
        {
            int l_ItemHl = ++hl;

            X12Render.Add(trans, map, "HL", l_ItemHl.ToString(), parentHl.ToString(), "I", "0");

            // LIN01 is the partner's own PO line id, then qualifier/value pairs for each identity we
            // hold. LIN02 and LIN03 are mandatory, so a line with no identifier at all cannot be
            // written - which is right: an ASN line nobody can match to a PO line is not receivable.
            var l_Ids = new List<string?>();

            AddId(l_Ids, "VN", X12Render.Str(line, "vendorSku"));
            AddId(l_Ids, "IN", X12Render.Str(line, "partnerSku"));
            AddId(l_Ids, "UP", X12Render.Str(line, "upc"));

            if (l_Ids.Count == 0)
            {
                throw new X12RenderException(
                    $"Line [{X12Render.Str(line, "lineNo")}] of the canonical 856 carries no vendorSku, partnerSku or upc; " +
                    "LIN02 and LIN03 are mandatory and there is nothing to put in them.");
            }

            var l_Lin = new List<string?> { X12Render.Str(line, "lineNo") };
            l_Lin.AddRange(l_Ids);

            X12Render.Add(trans, map, "LIN", l_Lin.ToArray());

            string? l_Uom = X12Render.Str(line, "uom") ?? DefaultUom;
            string? l_Shipped = X12Render.Num(line, "quantityShipped");
            string? l_Ordered = X12Render.Num(line, "quantityOrdered");

            if (l_Shipped is null)
            {
                throw new X12RenderException(
                    $"Line [{X12Render.Str(line, "lineNo")}] of the canonical 856 carries no quantityShipped; SN102 is mandatory.");
            }

            X12Render.Add(trans, map, "SN1",
                null,                                    // 01 - the identity is in LIN01
                l_Shipped,                               // 02
                l_Uom,                                   // 03
                null,                                    // 04
                l_Ordered,                               // 05 - paired with 06
                l_Ordered is null ? null : l_Uom);       // 06

            string? l_Description = X12Render.Text(X12Render.Str(line, "description"), 80);

            if (!string.IsNullOrWhiteSpace(l_Description))
            {
                X12Render.Add(trans, map, "PID", "F", null, null, null, l_Description);
            }

            // Piece-level identity. Both are lists, and both matter for a receiving scan, so each
            // gets its own segment rather than being folded into the first value.
            foreach (JsonElement l_Serial in X12Render.Array(line, "serialNumbers"))
            {
                Ref(trans, map, "SE", l_Serial.GetString(), null);
            }

            foreach (JsonElement l_Sscc in X12Render.Array(line, "pieceSsccs"))
            {
                Man(trans, map, "GM", l_Sscc.GetString());
            }
        }

        private static void AddId(List<string?> ids, string qualifier, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ids.Add(qualifier);
                ids.Add(value);
            }
        }

        /// <summary>TD1 at the shipment level: how many packages and what the whole load weighs.</summary>
        private static void Totals(EdiTrans trans, Maps_4010.M_856 map, JsonElement shipment, bool hasPallets)
        {
            string? l_Count = X12Render.Str(shipment, "totalPackages");

            // TD101 and TD102 are a conditional pair, so the packaging code only goes in when there
            // is a count to go with it.
            Weight(trans, map,
                l_Count is null ? null : (hasPallets ? "PLT" : "CTN"),
                l_Count,
                X12Render.Obj(shipment, "totalWeight"));
        }

        /// <summary>
        /// TD1 carrying a packaging code, a lading quantity and a gross weight, in whatever
        /// combination is actually present. TD1's C0102 and C0607 pairs are respected by
        /// construction, so a partial weight never produces an invalid segment.
        /// </summary>
        private static void Weight(EdiTrans trans, Maps_4010.M_856 map, string? packagingCode, string? ladingQuantity, JsonElement? weight)
        {
            string? l_Value = weight.HasValue ? X12Render.Num(X12Render.Str(weight.Value, "value")) : null;
            string? l_Uom = weight.HasValue ? X12Render.Str(weight.Value, "uom") : null;

            bool l_HasWeight = l_Value is not null && !string.IsNullOrWhiteSpace(l_Uom);
            bool l_HasPackaging = !string.IsNullOrWhiteSpace(packagingCode) && !string.IsNullOrWhiteSpace(ladingQuantity);

            if (!l_HasWeight && !l_HasPackaging)
            {
                return;
            }

            X12Render.Add(trans, map, "TD1",
                l_HasPackaging ? packagingCode : null,   // 01 - paired with 02
                l_HasPackaging ? ladingQuantity : null,  // 02
                null, null, null,
                l_HasWeight ? GrossWeight : null,        // 06 - conditional on 07
                l_HasWeight ? l_Value : null,            // 07
                l_HasWeight ? l_Uom : null);             // 08
        }

        /// <summary>TD5. TD502/03 are a conditional pair, so the SCAC brings its qualifier with it.</summary>
        private static void Carrier(EdiTrans trans, Maps_4010.M_856 map, JsonElement? carrier)
        {
            if (!carrier.HasValue)
            {
                return;
            }

            string? l_Scac = X12Render.Str(carrier.Value, "scac");
            string? l_Mode = X12Render.Str(carrier.Value, "transportMode");
            string? l_Routing = X12Render.Text(X12Render.Str(carrier.Value, "routing"), 35);

            if (l_Scac is not null || l_Mode is not null || l_Routing is not null)
            {
                X12Render.Add(trans, map, "TD5",
                    null,                                 // 01 routing sequence
                    l_Scac is null ? null : "2",          // 02 SCAC qualifier - paired with 03
                    l_Scac,                               // 03
                    l_Mode,                               // 04
                    l_Routing);                           // 05
            }

            // 004010 has nowhere for a partner-vocabulary service level or a carrier account, and
            // both are things the partner acts on, so they go out as qualified references rather
            // than being dropped.
            Ref(trans, map, "ZZ", X12Render.Str(carrier.Value, "serviceLevel"), "ServiceLevel");
            Ref(trans, map, "CA", X12Render.Str(carrier.Value, "accountNo"), "CarrierAccount");
        }

        /// <summary>The N1 loop for a canonical party: N1, N2, N3, N4, PER and a VAT reference.</summary>
        private static void Party(EdiTrans trans, Maps_4010.M_856 map, string entityQualifier, JsonElement? party, string defaultIdQualifier)
        {
            if (!party.HasValue)
            {
                return;
            }

            JsonElement l_Party = party.Value;

            string? l_Name = X12Render.Text(X12Render.Str(l_Party, "name"), 60);
            string? l_Gln = X12Render.Str(l_Party, "gln");
            // contactId is BizMate's own resolved ship-to id, and on this partner's staged 856s it
            // is often the ONLY thing identifying the consignee - the name and address come through
            // empty. Treating it as an identifier is the difference between an N1*ST and no
            // consignee at all.
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

            // A drop-ship consumer arrives as first and last name rather than a company name.
            string? l_Second = X12Render.Text(
                Join(X12Render.Str(l_Party, "firstName"), X12Render.Str(l_Party, "lastName"))
                ?? X12Render.Str(l_Party, "attention"), 60);

            if (!string.IsNullOrWhiteSpace(l_Second))
            {
                X12Render.Add(trans, map, "N2", l_Second);
            }

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

            string? l_Phone = X12Render.Str(l_Party, "phone");
            string? l_Email = X12Render.Str(l_Party, "email");

            if (l_Phone is not null || l_Email is not null)
            {
                X12Render.Add(trans, map, "PER",
                    "IC",                                  // 01 information contact
                    X12Render.Text(X12Render.Str(l_Party, "attention"), 60),
                    l_Phone is null ? null : "TE",         // 03 - paired with 04
                    l_Phone,
                    l_Email is null ? null : "EM",         // 05 - paired with 06
                    l_Email);
            }

            Ref(trans, map, "VA", X12Render.Str(l_Party, "vatNumber"), null);
        }

        /// <summary>The canonical references array, carried through untouched (REF / RFF).</summary>
        private static void References(EdiTrans trans, Maps_4010.M_856 map, JsonElement parent)
        {
            foreach (JsonElement l_Reference in X12Render.Array(parent, "references"))
            {
                Ref(trans, map,
                    X12Render.Str(l_Reference, "qualifier") ?? "ZZ",
                    X12Render.Str(l_Reference, "value"),
                    X12Render.Str(l_Reference, "description"));
            }
        }

        /// <summary>Carton dimensions as a qualified reference; 004010's TD3 has no room for them.</summary>
        private static void Dimensions(EdiTrans trans, Maps_4010.M_856 map, JsonElement? dimensions)
        {
            if (!dimensions.HasValue)
            {
                return;
            }

            string? l_Length = X12Render.Num(X12Render.Str(dimensions.Value, "length"));
            string? l_Width = X12Render.Num(X12Render.Str(dimensions.Value, "width"));
            string? l_Height = X12Render.Num(X12Render.Str(dimensions.Value, "height"));

            if (l_Length is null && l_Width is null && l_Height is null)
            {
                return;
            }

            string l_Uom = X12Render.Str(dimensions.Value, "uom") ?? "CM";

            Ref(trans, map, "ZZ", $"{l_Length}x{l_Width}x{l_Height}{l_Uom}", "Dimensions");
        }

        /// <summary>MAN, but only when there is a number. MAN01 and MAN02 are both mandatory.</summary>
        private static void Man(EdiTrans trans, Maps_4010.M_856 map, string qualifier, string? number)
        {
            if (!string.IsNullOrWhiteSpace(number))
            {
                X12Render.Add(trans, map, "MAN", qualifier, number);
            }
        }

        /// <summary>REF, but only when there is something to put in REF02. R0203 needs 02 or 03.</summary>
        private static void Ref(EdiTrans trans, Maps_4010.M_856 map, string qualifier, string? value, string? description)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                X12Render.Add(trans, map, "REF", qualifier, X12Render.Text(value, 30), X12Render.Text(description, 80));
            }
        }

        /// <summary>BSN01, as the contract states it: 00 original, 05 replace, 01 cancel.</summary>
        private static string Purpose(string? purpose)
        {
            return purpose switch
            {
                null or "" or "Original" => "00",
                "Replace" => "05",
                "Cancel" => "01",
                _ => throw new X12RenderException(
                    $"'{purpose}' is not an 856 purpose. The contract allows Original, Replace and Cancel.")
            };
        }

        /// <summary>
        /// The canonical packagingType is partner prose - "Cardboard", "Pallet" - and TD101 is a
        /// three to five character code. Mapping the words we see keeps the segment valid; an
        /// unrecognised word becomes CTN, because a carton is what an ASN carton almost always is
        /// and the tracking number identifies it regardless.
        /// </summary>
        private static string Packaging(string? packagingType)
        {
            if (string.IsNullOrWhiteSpace(packagingType))
            {
                return "CTN";
            }

            string l_Type = packagingType.ToLowerInvariant();

            if (l_Type.Contains("pallet") || l_Type.Contains("skid")) return "PLT";
            if (l_Type.Contains("case")) return "CAS";
            if (l_Type.Contains("bag") || l_Type.Contains("sack")) return "BAG";
            if (l_Type.Contains("drum")) return "DRM";
            if (l_Type.Contains("crate")) return "CRT";
            if (l_Type.Contains("roll")) return "ROL";
            if (l_Type.Contains("bundle")) return "BDL";
            if (l_Type.Contains("tube")) return "TBE";

            return "CTN";
        }

        private static string? Join(string? first, string? last)
        {
            string l_Joined = string.Join(" ", new[] { first, last }.Where(p => !string.IsNullOrWhiteSpace(p)));

            return l_Joined.Length == 0 ? null : l_Joined;
        }

        private static string? NullIfBlank(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
