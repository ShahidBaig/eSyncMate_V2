using EdiEngine;
using EdiEngine.Runtime;

namespace eSyncMate.Processor.Connections
{
    /// <summary>One POC loop: a line the partner is changing, and what they are changing about it.</summary>
    public sealed class ChangeOrderLine
    {
        /// <summary>POC01 - the line this change is about, as the partner numbers it.</summary>
        public string? LineNo { get; set; }

        /// <summary>POC02 - the change code itself. Kept raw as well as mapped, so a refusal can quote it.</summary>
        public string? ChangeCode { get; set; }

        /// <summary>POC03 - quantity ordered after the change.</summary>
        public decimal? QuantityOrdered { get; set; }

        /// <summary>POC04 - quantity left to receive, which most partners use as the delta.</summary>
        public decimal? QuantityChange { get; set; }

        /// <summary>
        /// POC06. NOT POC05, which is a composite unit-of-measure element (F-36). The same mistake
        /// on the 865 put a unit of measure where a price belonged.
        /// </summary>
        public decimal? UnitPrice { get; set; }

        /// <summary>The unit of measure out of the POC05 composite.</summary>
        public string? Uom { get; set; }

        public string? PartnerSku { get; set; }
        public string? VendorSku { get; set; }
        public string? Upc { get; set; }
        public string? Description { get; set; }

        /// <summary>DTM inside the POC loop, by qualifier.</summary>
        public string? RequestedShipDate { get; set; }
        public string? CancelAfterDate { get; set; }
    }

    /// <summary>One inbound 860, read.</summary>
    public sealed class ChangeOrderReading
    {
        /// <summary>BCH01 - the transaction set purpose, which decides Change, CancelOrder or Replace.</summary>
        public string? PurposeCode { get; set; }

        /// <summary>BCH02 - SA stand-alone, DS drop ship. Carried through, never translated (W3-01).</summary>
        public string? PoTypeCode { get; set; }

        /// <summary>BCH03 - the purchase order this changes. Without it the document names nothing.</summary>
        public string? PoNumber { get; set; }

        /// <summary>BCH05 - the partner's own sequence for changes to this order.</summary>
        public string? ChangeSequence { get; set; }

        /// <summary>BCH06 - the date of the ORIGINAL order.</summary>
        public string? PoDate8 { get; set; }

        /// <summary>BCH09 - the date of this change.</summary>
        public string? ChangeDate8 { get; set; }

        // ---- header-scoped changes, the six the canonical model carries ----

        public Dictionary<string, string?>? ShipTo { get; set; }
        public string? RequestedShipDate8 { get; set; }
        public string? RequestedDeliveryDate8 { get; set; }
        public string? CancelAfterDate8 { get; set; }
        public string? Carrier { get; set; }
        public string? Instructions { get; set; }

        /// <summary>True when the partner sent an MSG at all, even an empty one - see W3-14.</summary>
        public bool InstructionsStated { get; set; }

        public List<ChangeOrderLine> Lines { get; } = new();
    }

    /// <summary>
    /// Reads an inbound 860 (W3-14, E14).
    ///
    /// An 860 is the partner changing an order we already hold: quantities up or down, lines added
    /// or cancelled, a price or a date moved. It is the one inbound document whose meaning depends
    /// entirely on codes - POC02 per line and BCH01 for the whole document - so the reading rule
    /// throughout is **map what the standard defines and refuse what it does not**, never guess. A
    /// change we cannot read is a change we must not apply, because the alternative is silently
    /// shipping the wrong quantity.
    ///
    /// Confirmed needed rather than assumed: BizMate's own configuration has `860` `In`,
    /// `enabled: true` for this partner (F-50).
    /// </summary>
    public static class X12ChangeOrder
    {
        public const string DocumentType = "860";

        /// <summary>
        /// BCH01 to the canonical purpose. The three the contract defines and nothing else.
        ///
        /// Null for any other code, and the caller refuses. 00 Original and 22 Information Copy are
        /// deliberately not mapped: an 860 that says it is an original is not a change, and an
        /// information copy is explicitly not an instruction to act.
        /// </summary>
        public static string? Purpose(string? bch01)
        {
            return bch01 switch
            {
                "01" => "CancelOrder",
                "04" => "Change",
                "05" => "Replace",
                _ => null
            };
        }

        /// <summary>
        /// POC02 to the canonical change type. Only the five whose correspondence is unambiguous.
        ///
        /// Null for anything else, and the caller refuses the whole document rather than the line:
        /// applying the changes we understood and dropping the one we did not would leave the order
        /// in a state neither side agreed to, which is worse than refusing all of it.
        ///
        /// **Two of the seven canonical types have no mapping here, deliberately (EQ-22).** X12
        /// 004010 defines twenty-nine values for POC02 - the engine's own `E_0670` allowed list is
        /// AI, CA, CB, CC, CE, CF, CG, CH, CI, CT, DI, MU, NC, OA, OC, PC, PQ, PR, QD, QI, RA, RB,
        /// RC, RE, RM, RQ, RS, RZ, TI - and nobody has stated which of them BizMate means by
        /// `Reschedule` and `DateChange`. `RS` and `RZ` are both plausible for a reschedule and the
        /// standard's descriptions are not in the engine, so choosing between them would be a guess
        /// about what a partner is asking us to do to an order. The first version of this method
        /// mapped `DT` to `DateChange`; `DT` is not a valid POC02 code at all, which is what
        /// guessing looks like when it is wrong and nobody checks.
        ///
        /// **CA is absent for a different reason and would be even with the answer.** "Changes to
        /// Line Items" says a line changed without saying what changed, so it cannot reach any of
        /// the seven without inferring from the rest of the segment.
        ///
        /// Refusing a legitimate change is the safer error than applying a wrong one, and it fails
        /// loudly with the code named, so it gets corrected rather than shipped.
        /// </summary>
        public static string? ChangeType(string? poc02)
        {
            return poc02 switch
            {
                "QI" => "QuantityIncrease",
                "QD" => "QuantityDecrease",
                "DI" => "CancelLine",
                "AI" => "AddLine",
                "PC" => "PriceChange",
                _ => null
            };
        }

        /// <summary>CCYYMMDD to the canonical YYYY-MM-DD, or null when it is not a date.</summary>
        public static string? Date(string? ccyymmdd)
        {
            if (ccyymmdd is null || ccyymmdd.Length != 8 || !ccyymmdd.All(char.IsDigit))
            {
                return null;
            }

            return ccyymmdd[..4] + "-" + ccyymmdd.Substring(4, 2) + "-" + ccyymmdd.Substring(6, 2);
        }

        /// <summary>The transaction set id of the first transaction, or null. Shared with the 997 reader.</summary>
        public static string? TransactionSetId(string text) => X12Ack.TransactionSetId(text);

        /// <summary>
        /// Reduces an 860 to the order it changes, the header changes and the line changes.
        ///
        /// A small state machine over the flat segment list, like the 824 reader: POC opens a line
        /// and the DTM and product-id segments that follow belong to whichever POC came last. The
        /// N1 loop is header-scoped and only the ship-to is carried, because that is the only party
        /// the canonical headerChanges models.
        /// </summary>
        public static ChangeOrderReading Read(string text)
        {
            var l_Reading = new ChangeOrderReading();

            EdiBatch l_Batch = new EdiDataReader().FromString(text);

            foreach (EdiInterchange l_Interchange in l_Batch.Interchanges)
            {
                foreach (EdiGroup l_Group in l_Interchange.Groups)
                {
                    foreach (EdiTrans l_Trans in l_Group.Transactions)
                    {
                        if (Element(l_Trans.ST, 0) != DocumentType)
                        {
                            continue;
                        }

                        ChangeOrderLine? l_Line = null;
                        Dictionary<string, string?>? l_Party = null;

                        foreach (EdiBaseEntity l_Entity in Flatten(l_Trans))
                        {
                            if (l_Entity is not EdiSegment l_Segment)
                            {
                                continue;
                            }

                            switch (l_Segment.Name)
                            {
                                case "BCH":
                                    l_Reading.PurposeCode ??= Element(l_Segment, 0);
                                    l_Reading.PoTypeCode ??= Element(l_Segment, 1);
                                    l_Reading.PoNumber ??= Element(l_Segment, 2);
                                    l_Reading.ChangeSequence ??= Element(l_Segment, 4);
                                    l_Reading.PoDate8 ??= Element(l_Segment, 5);
                                    l_Reading.ChangeDate8 ??= Element(l_Segment, 8);
                                    break;

                                case "N1":
                                    // Header parties only: a POC loop carries no N1 in 004010, so a
                                    // party seen here is always about the order as a whole.
                                    l_Party = Element(l_Segment, 0) == "ST"
                                        ? new Dictionary<string, string?>
                                        {
                                            ["name"] = Element(l_Segment, 1),
                                            ["idQualifier"] = Element(l_Segment, 2),
                                            ["id"] = Element(l_Segment, 3)
                                        }
                                        : null;

                                    if (l_Party is not null)
                                    {
                                        l_Reading.ShipTo = l_Party;
                                    }

                                    break;

                                case "N3":
                                    if (l_Party is not null)
                                    {
                                        l_Party["address1"] = Element(l_Segment, 0);
                                        l_Party["address2"] = Element(l_Segment, 1);
                                    }

                                    break;

                                case "N4":
                                    if (l_Party is not null)
                                    {
                                        l_Party["city"] = Element(l_Segment, 0);
                                        l_Party["state"] = Element(l_Segment, 1);
                                        l_Party["zip"] = Element(l_Segment, 2);
                                        l_Party["country"] = Element(l_Segment, 3);
                                    }

                                    break;

                                case "TD5":
                                    // TD502/TD503 is the carrier identification pair; TD505 is the
                                    // routing description a person reads.
                                    l_Reading.Carrier ??= Element(l_Segment, 2) ?? Element(l_Segment, 4);
                                    break;

                                case "MSG":
                                    // Stated even when empty. An 860 that blanks the instructions is
                                    // telling us to blank them, which is not the same as an 860 that
                                    // says nothing about them (W3-14).
                                    l_Reading.InstructionsStated = true;
                                    l_Reading.Instructions ??= Element(l_Segment, 0);
                                    break;

                                case "DTM":
                                    Date(l_Segment, l_Reading, l_Line);
                                    break;

                                case "POC":
                                    l_Line = new ChangeOrderLine
                                    {
                                        LineNo = Element(l_Segment, 0),
                                        ChangeCode = Element(l_Segment, 1),
                                        QuantityOrdered = Decimal(Element(l_Segment, 2)),
                                        QuantityChange = Decimal(Element(l_Segment, 3)),
                                        Uom = Element(l_Segment, 4),

                                        // POC06, not POC05 (F-36).
                                        UnitPrice = Decimal(Element(l_Segment, 5))
                                    };

                                    ReadProductIds(l_Segment, l_Line);
                                    l_Reading.Lines.Add(l_Line);
                                    break;

                                case "PID":
                                    if (l_Line is not null)
                                    {
                                        l_Line.Description ??= Element(l_Segment, 4);
                                    }

                                    break;
                            }
                        }
                    }
                }
            }

            return l_Reading;
        }

        /// <summary>
        /// POC08 onward are qualifier/value pairs, the same shape as PO1-06 onward on an 850.
        /// Unknown qualifiers are skipped rather than guessed at: an identifier we cannot name is
        /// not an identifier we should put in a canonical field.
        /// </summary>
        private static void ReadProductIds(EdiSegment segment, ChangeOrderLine line)
        {
            for (int l_Index = 7; l_Index + 1 < segment.Content.Count; l_Index += 2)
            {
                string? l_Qualifier = Element(segment, l_Index);
                string? l_Value = Element(segment, l_Index + 1);

                if (l_Qualifier is null || l_Value is null)
                {
                    continue;
                }

                switch (l_Qualifier)
                {
                    case "VP":
                        line.VendorSku ??= l_Value;
                        break;

                    case "UP":
                    case "EN":
                        line.Upc ??= l_Value;
                        break;

                    case "BP":
                    case "IN":
                        line.PartnerSku ??= l_Value;
                        break;
                }
            }
        }

        /// <summary>
        /// A DTM belongs to the line it follows, or to the header when no line has opened yet.
        /// 010 requested ship, 002 requested delivery, 001 cancel after.
        /// </summary>
        private static void Date(EdiSegment segment, ChangeOrderReading reading, ChangeOrderLine? line)
        {
            string? l_Qualifier = Element(segment, 0);
            string? l_Date = Element(segment, 1);

            if (l_Qualifier is null || l_Date is null)
            {
                return;
            }

            if (line is not null)
            {
                switch (l_Qualifier)
                {
                    case "010":
                        line.RequestedShipDate ??= l_Date;
                        break;

                    case "001":
                        line.CancelAfterDate ??= l_Date;
                        break;
                }

                return;
            }

            switch (l_Qualifier)
            {
                case "010":
                    reading.RequestedShipDate8 ??= l_Date;
                    break;

                case "002":
                    reading.RequestedDeliveryDate8 ??= l_Date;
                    break;

                case "001":
                    reading.CancelAfterDate8 ??= l_Date;
                    break;
            }
        }

        private static decimal? Decimal(string? value)
        {
            return decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal l_Parsed)
                ? l_Parsed
                : null;
        }

        private static string? Element(EdiSegment? segment, int index)
        {
            if (segment is null || index < 0 || index >= segment.Content.Count)
            {
                return null;
            }

            string l_Value = segment.Content[index].ToString()?.Trim() ?? string.Empty;

            return l_Value.Length == 0 ? null : l_Value;
        }

        /// <summary>Every segment under a transaction, in document order, loops flattened.</summary>
        private static IEnumerable<EdiBaseEntity> Flatten(EdiLoop loop)
        {
            foreach (EdiBaseEntity l_Entity in loop.Content)
            {
                yield return l_Entity;

                if (l_Entity is EdiLoop l_Child)
                {
                    foreach (EdiBaseEntity l_Deep in Flatten(l_Child))
                    {
                        yield return l_Deep;
                    }
                }
            }
        }
    }
}
