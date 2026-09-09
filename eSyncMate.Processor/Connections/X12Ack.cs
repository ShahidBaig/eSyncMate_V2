using EdiEngine;
using EdiEngine.Runtime;

namespace eSyncMate.Processor.Connections
{
    /// <summary>What one AK1..AK9 group in a 997 says about one transmission we sent.</summary>
    public sealed class AcknowledgedGroup
    {
        /// <summary>AK101 - the functional identifier of the group being acknowledged (SH, PR, IB…).</summary>
        public string? FunctionalCode { get; set; }

        /// <summary>
        /// AK102 - the group control number being acknowledged. Under EQ-01 this is our GS06, which
        /// is the ledger row id, so it is the key the correlation resolves on.
        /// </summary>
        public string? GroupControlNo { get; set; }

        /// <summary>AK901 - A, E, P, R, M, W or X.</summary>
        public string? Status { get; set; }

        /// <summary>AK5 and AK9 codes and counts, in the order the partner wrote them.</summary>
        public List<string> Notes { get; } = new();
    }

    /// <summary>One inbound 997, read.</summary>
    public sealed class AcknowledgementReading
    {
        /// <summary>ISA13 of the acknowledgment itself - the partner's own control number.</summary>
        public string? InterchangeControlNo { get; set; }

        public List<AcknowledgedGroup> Groups { get; } = new();
    }

    /// <summary>
    /// Reads an inbound 997 (W2-10, E11).
    ///
    /// A 997 is not a business document and has no canonical form: nothing about it goes to
    /// <c>/inbound</c>. What it carries is a verdict on something we already sent, so the only
    /// question worth asking of it is "which transmission, and what did they say" - which is what
    /// this reduces it to.
    ///
    /// The correlation key is the GROUP control number in AK102, not the interchange number. Both
    /// carry the ledger row id when eSyncMate writes the envelope (X12Render.Write puts the same
    /// number in ISA13 and GS06), but AK102 is the one the standard guarantees is present, and a
    /// partner that rebuilds interchanges can legitimately change ISA13 while preserving the group.
    ///
    /// Parsing is deliberately tolerant. A 997 that we cannot read is worth recording as an
    /// unreadable acknowledgment; it is never worth throwing away a partner's answer because one
    /// segment was not where the standard says.
    /// </summary>
    public static class X12Ack
    {
        public const string DocumentType = "997";

        /// <summary>
        /// The transaction set id of the first transaction in the file - what ST01 says this is.
        ///
        /// Returns null when the file does not parse, which the caller reads as "not an
        /// acknowledgment" and puts down the ordinary document path, where an unparseable
        /// interchange already fails visibly with its reason.
        /// </summary>
        public static string? TransactionSetId(string text)
        {
            try
            {
                EdiBatch l_Batch = new EdiDataReader().FromString(text);

                foreach (EdiInterchange l_Interchange in l_Batch.Interchanges)
                {
                    foreach (EdiGroup l_Group in l_Interchange.Groups)
                    {
                        foreach (EdiTrans l_Trans in l_Group.Transactions)
                        {
                            string? l_Id = Element(l_Trans.ST, 0);

                            if (!string.IsNullOrWhiteSpace(l_Id))
                            {
                                return l_Id;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Reduces a 997 to the groups it acknowledges and what it said about each.
        ///
        /// AK1 opens a group and AK9 closes it, so this is a small state machine over the flat
        /// segment list rather than a walk of the loop structure - the reader gives the segments in
        /// document order, which is all the state machine needs.
        /// </summary>
        public static AcknowledgementReading Read(string text)
        {
            var l_Reading = new AcknowledgementReading();

            EdiBatch l_Batch = new EdiDataReader().FromString(text);

            foreach (EdiInterchange l_Interchange in l_Batch.Interchanges)
            {
                l_Reading.InterchangeControlNo ??= Element(l_Interchange.ISA, 12);

                foreach (EdiGroup l_Group in l_Interchange.Groups)
                {
                    foreach (EdiTrans l_Trans in l_Group.Transactions)
                    {
                        if (Element(l_Trans.ST, 0) != DocumentType)
                        {
                            continue;
                        }

                        AcknowledgedGroup? l_Current = null;

                        foreach (EdiBaseEntity l_Entity in Flatten(l_Trans))
                        {
                            if (l_Entity is not EdiSegment l_Segment)
                            {
                                continue;
                            }

                            switch (l_Segment.Name)
                            {
                                case "AK1":
                                    l_Current = new AcknowledgedGroup
                                    {
                                        FunctionalCode = Element(l_Segment, 0),
                                        GroupControlNo = Element(l_Segment, 1)
                                    };

                                    l_Reading.Groups.Add(l_Current);
                                    break;

                                case "AK2":
                                    Note(l_Current, "set " + Join(l_Segment, 0, 1));
                                    break;

                                case "AK5":
                                    Note(l_Current, "AK5 " + Join(l_Segment, 0, 5));
                                    break;

                                case "AK9":
                                    if (l_Current is not null)
                                    {
                                        l_Current.Status = Element(l_Segment, 0);
                                    }

                                    Note(l_Current, "AK9 " + Join(l_Segment, 0, 8));
                                    break;
                            }
                        }
                    }
                }
            }

            return l_Reading;
        }

        /// <summary>
        /// AK901 or AK501 to the three words the ack contract uses.
        ///
        /// E and P are acceptance with something wrong in it, which is a materially different fact
        /// from either a clean accept or a refusal - a partner that took the document but noted
        /// errors will still act on it. M, W and X are rejections for authentication, assurance and
        /// decryption; they are security failures rather than content failures, but from the
        /// document's point of view the partner does not have it, so they are rejections.
        ///
        /// An unrecognised code reads as Rejected on purpose. If we cannot tell what the partner
        /// said, the safe reading is that the document did not land.
        /// </summary>
        public static string Result(string? code)
        {
            return code switch
            {
                "A" => "Accepted",
                "E" => "AcceptedWithErrors",
                "P" => "AcceptedWithErrors",
                _ => "Rejected"
            };
        }

        private static void Note(AcknowledgedGroup? group, string note)
        {
            if (group is not null && !string.IsNullOrWhiteSpace(note))
            {
                group.Notes.Add(note.Trim());
            }
        }

        /// <summary>Elements first through last, empties dropped, as one space-separated string.</summary>
        private static string Join(EdiSegment segment, int first, int last)
        {
            var l_Parts = new List<string>();

            for (int i = first; i <= last; i++)
            {
                string? l_Value = Element(segment, i);

                if (!string.IsNullOrWhiteSpace(l_Value))
                {
                    l_Parts.Add(l_Value!);
                }
            }

            return string.Join(" ", l_Parts);
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
