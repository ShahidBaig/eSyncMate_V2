using EdiEngine;
using EdiEngine.Runtime;

namespace eSyncMate.Processor.Connections
{
    /// <summary>One condition the partner's application reported against a document.</summary>
    public sealed class AdviceError
    {
        /// <summary>TED01 - the partner's application error condition code.</summary>
        public string? Code { get; set; }

        /// <summary>TED02 - the sentence a person reads on the triage worklist.</summary>
        public string? Text { get; set; }

        /// <summary>TED03 - the segment the partner says is at fault.</summary>
        public string? Segment { get; set; }

        /// <summary>TED05 - the element's position within that segment.</summary>
        public int? ElementPosition { get; set; }

        /// <summary>TED07 - the partner's copy of the data element it objected to.</summary>
        public string? BadValue { get; set; }
    }

    /// <summary>One OTI loop: a document the partner is advising us about, and what they said.</summary>
    public sealed class AdvisedDocument
    {
        /// <summary>OTI01 - TA accepted, TE accepted with errors, TR rejected.</summary>
        public string? AcknowledgmentCode { get; set; }

        /// <summary>OTI02 - what kind of reference OTI03 is.</summary>
        public string? ReferenceQualifier { get; set; }

        /// <summary>OTI03 - the partner's handle on the document, often our own document number.</summary>
        public string? ReferenceId { get; set; }

        /// <summary>OTI08 - the group control number of the transmission being advised.</summary>
        public string? GroupControlNo { get; set; }

        /// <summary>OTI09 - the transaction set control number within that group.</summary>
        public string? TransactionSetControlNo { get; set; }

        /// <summary>OTI10 - which transaction set this advice is about (856, 810 …).</summary>
        public string? TransactionSetId { get; set; }

        public List<AdviceError> Errors { get; } = new();
    }

    /// <summary>One inbound 824, read.</summary>
    public sealed class AdviceReading
    {
        /// <summary>BGN02 - the partner's own reference for this advice.</summary>
        public string? AdviceNo { get; set; }

        /// <summary>BGN03 and BGN04.</summary>
        public string? Date8 { get; set; }
        public string? Time4 { get; set; }

        public List<AdvisedDocument> Documents { get; } = new();
    }

    /// <summary>
    /// Reads an inbound 824 (W3-18, E13).
    ///
    /// An 824 is the partner's business system saying what it made of a document that already passed
    /// the 997 syntax check - accepted, accepted with errors, or rejected outright, with the reasons.
    /// It is the difference between "your envelope was well formed" and "we cannot act on this".
    ///
    /// Correlation is on **OTI08, the group control number**, for the same reason the 997 correlates
    /// on AK102: eSyncMate writes the ledger row id into GS06, and that is what a partner echoes when
    /// it names the transmission. OTI03 is the fallback, because plenty of partners put the document
    /// number a human recognises there instead - an invoice or shipment number - and a correlation
    /// that only understood one of the two would fail on half the partners for no good reason.
    ///
    /// Parsing is tolerant. An advice we cannot read is worth recording as an unreadable advice; it
    /// is never worth discarding a partner telling us they refused something.
    /// </summary>
    public static class X12Advice
    {
        public const string DocumentType = "824";

        /// <summary>
        /// Reduces an 824 to the documents it advises and what the partner said about each.
        ///
        /// OTI opens a document and TED segments belong to whichever OTI came last, so this is a
        /// small state machine over the flat segment list rather than a walk of the loop structure -
        /// the reader gives the segments in document order, which is all the state machine needs.
        /// </summary>
        public static AdviceReading Read(string text)
        {
            var l_Reading = new AdviceReading();

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

                        AdvisedDocument? l_Current = null;

                        foreach (EdiBaseEntity l_Entity in Flatten(l_Trans))
                        {
                            if (l_Entity is not EdiSegment l_Segment)
                            {
                                continue;
                            }

                            switch (l_Segment.Name)
                            {
                                case "BGN":
                                    l_Reading.AdviceNo ??= Element(l_Segment, 1);
                                    l_Reading.Date8 ??= Element(l_Segment, 2);
                                    l_Reading.Time4 ??= Element(l_Segment, 3);
                                    break;

                                case "OTI":
                                    l_Current = new AdvisedDocument
                                    {
                                        AcknowledgmentCode = Element(l_Segment, 0),
                                        ReferenceQualifier = Element(l_Segment, 1),
                                        ReferenceId = Element(l_Segment, 2),
                                        GroupControlNo = Element(l_Segment, 7),
                                        TransactionSetControlNo = Element(l_Segment, 8),
                                        TransactionSetId = Element(l_Segment, 9)
                                    };

                                    l_Reading.Documents.Add(l_Current);
                                    break;

                                case "TED":
                                    l_Current?.Errors.Add(new AdviceError
                                    {
                                        Code = Element(l_Segment, 0),
                                        Text = Element(l_Segment, 1),
                                        Segment = Element(l_Segment, 2),
                                        ElementPosition = Number(Element(l_Segment, 4)),
                                        BadValue = Element(l_Segment, 6)
                                    });
                                    break;
                            }
                        }
                    }
                }
            }

            return l_Reading;
        }

        /// <summary>
        /// OTI01 to the three words the canonical model uses.
        ///
        /// Only Rejected moves the original message on BizMate's side, so the distinction between TE
        /// and TR is not cosmetic: one is a document the partner acted on while noting faults, the
        /// other is one they did not act on at all.
        ///
        /// An unrecognised code reads as Rejected on purpose. If we cannot tell what the partner
        /// said, the safe reading is that they did not accept it.
        /// </summary>
        public static string Result(string? code)
        {
            return code switch
            {
                "TA" => "Accepted",
                "TE" => "AcceptedWithErrors",
                _ => "Rejected"
            };
        }

        /// <summary>
        /// The control number this advice refers to, preferring the group number over the free
        /// reference. Null when the partner named neither, which is an advice we cannot resolve.
        /// </summary>
        public static string? ControlNumber(AdvisedDocument document)
        {
            return !string.IsNullOrWhiteSpace(document.GroupControlNo)
                ? document.GroupControlNo
                : document.ReferenceId;
        }

        private static int? Number(string? value)
        {
            return int.TryParse(value, out int l_Parsed) ? l_Parsed : null;
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
