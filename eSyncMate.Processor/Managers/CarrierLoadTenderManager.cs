using EdiEngine;
using EdiEngine.Common.Definitions;
using EdiEngine.Runtime;
using eSyncMate.DB.Entities;
using Intercom.Data;
using MySql.Data.MySqlClient;
using System.Threading;
using Maps_4010 = EdiEngine.Standards.X12_004010.Maps;
using SegmentDefinitions = EdiEngine.Standards.X12_004010.Segments;

namespace eSyncMate.Processor.Managers
{
    public class CarrierLoadTenderManager
    {
        public static bool SaveCarrierLoadTender(CarrierLoadTender carrierLoadTender, Customers customer, InboundEDI ediInfo, int userNo, string edi, string json)
        {
            bool result = false;

            carrierLoadTender.Status = "NEW";
            carrierLoadTender.CustomerId = customer.Id;
            carrierLoadTender.InboundEDIId = ediInfo.Id;
            carrierLoadTender.CreatedBy = userNo;
            carrierLoadTender.CreatedDate = DateTime.Now;

            if (carrierLoadTender.SaveNew().IsSuccess)
            {
                CarrierLoadTenderData l_Data = new CarrierLoadTenderData();

                l_Data.UseConnection(string.Empty, carrierLoadTender.Connection);

                l_Data.DeleteWithType(carrierLoadTender.Id, "204-EDI");

                l_Data.Type = "204-EDI";
                l_Data.Data = edi;
                l_Data.CreatedBy = userNo;
                l_Data.CreatedDate = DateTime.Now;
                l_Data.CarrierLoadTenderId = carrierLoadTender.Id;

                if (l_Data.SaveNew().IsSuccess)
                {
                    l_Data.DeleteWithType(carrierLoadTender.Id, "204-JSON");

                    l_Data.Type = "204-JSON";
                    l_Data.Data = json;
                    l_Data.CreatedBy = userNo;
                    l_Data.CreatedDate = DateTime.Now;
                    l_Data.CarrierLoadTenderId = carrierLoadTender.Id;

                    result = l_Data.SaveNew().IsSuccess;
                }
            }

            return result;
        }

        public static string Generate997(InboundEDIInfo inboundEDIInfo, CarrierLoadTender carrier, EdiTrans ctlTrans)
        {
            string edi_997 = string.Empty;

            Maps_4010.M_997 map = new Maps_4010.M_997();
            EdiTrans t = new EdiTrans(map);
            MapSegment l_SegmentDef = null;

            //AK1
            l_SegmentDef = (MapSegment)map.Content.First(s => s.Name == "AK1");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "SM", inboundEDIInfo.GSControlNumber })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            //AK2
            l_SegmentDef = (MapSegment)((MapLoop)map.Content.First(s => s.Name == "L_AK2")).Content.First(s => s.Name == "AK2");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "204", ctlTrans.ST.Content[1].Val })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            //AK5
            l_SegmentDef = (MapSegment)((MapLoop)map.Content.First(s => s.Name == "L_AK2")).Content.First(s => s.Name == "AK5");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "A" })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            //AK9
            l_SegmentDef = (MapSegment)map.Content.First(s => s.Name == "AK9");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "A", "1", "1", "1" })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            var g = new EdiGroup("FA");
            g.Transactions.Add(t);

            var i = new EdiInterchange();
            i.Groups.Add(g);

            EdiBatch b = new EdiBatch();
            b.Interchanges.Add(i);

            OutboundEDI l_Outbound = new OutboundEDI();

            l_Outbound.UseConnection(string.Empty, carrier.Connection);

            l_Outbound.Status = "NEW";
            l_Outbound.Data = string.Empty;
            l_Outbound.CreatedBy = carrier.CreatedBy;
            l_Outbound.CreatedDate = DateTime.Now;
            l_Outbound.OrderId = carrier.Id;

            if (l_Outbound.SaveNew().IsSuccess)
            {
                EdiDataWriterSettings settings = new EdiDataWriterSettings(
                new SegmentDefinitions.ISA(), new SegmentDefinitions.IEA(),
                new SegmentDefinitions.GS(), new SegmentDefinitions.GE(),
                new SegmentDefinitions.ST(), new SegmentDefinitions.SE(),
                inboundEDIInfo.ISAReceiverQual, inboundEDIInfo.ISAReceiverId, inboundEDIInfo.ISASenderQual, inboundEDIInfo.ISASenderId, inboundEDIInfo.GSReceiverId, inboundEDIInfo.GSSenderId,
                inboundEDIInfo.ISAEdiVersion, inboundEDIInfo.GSEdiVersion, inboundEDIInfo.ISAUsageIndicator, l_Outbound.Id, l_Outbound.Id, inboundEDIInfo.SegmentSeparator, inboundEDIInfo.ElementSeparator, "U", ">");

                EdiDataWriter w = new EdiDataWriter(settings);

                CarrierLoadTenderData l_OData = new CarrierLoadTenderData();

                l_OData.UseConnection(string.Empty, carrier.Connection);

                l_OData.DeleteWithType(carrier.Id, "997-EDI");

                l_OData.Type = "997-EDI";
                l_OData.Data = w.WriteToString(b);
                l_OData.CreatedBy = carrier.CreatedBy;
                l_OData.CreatedDate = DateTime.Now;
                l_OData.CarrierLoadTenderId = carrier.Id;

                if (l_OData.SaveNew().IsSuccess)
                {
                    l_Outbound.Data = l_OData.Data;
                    l_Outbound.Modify();

                    edi_997 = l_Outbound.Data;
                }
            }

            return edi_997;
        }

        public static string Surgimac855Generate997(InboundEDIInfo inboundEDIInfo, EdiTrans ctlTrans, string DocumentType)
        {
            string edi_997 = string.Empty;

            Maps_4010.M_997 map = new Maps_4010.M_997();
            EdiTrans t = new EdiTrans(map);
            MapSegment l_SegmentDef = null;

            //AK1
            l_SegmentDef = (MapSegment)map.Content.First(s => s.Name == "AK1");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "PR", inboundEDIInfo.GSControlNumber })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            //AK2
            l_SegmentDef = (MapSegment)((MapLoop)map.Content.First(s => s.Name == "L_AK2")).Content.First(s => s.Name == "AK2");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { DocumentType, ctlTrans.ST.Content[1].Val })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            //AK5
            l_SegmentDef = (MapSegment)((MapLoop)map.Content.First(s => s.Name == "L_AK2")).Content.First(s => s.Name == "AK5");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "A" })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            //AK9
            l_SegmentDef = (MapSegment)map.Content.First(s => s.Name == "AK9");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "A", "1", "1", "1" })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            var g = new EdiGroup("FA");
            g.Transactions.Add(t);

            var i = new EdiInterchange();
            i.Groups.Add(g);

            EdiBatch b = new EdiBatch();
            b.Interchanges.Add(i);

            EdiDataWriterSettings settings = new EdiDataWriterSettings(
            new SegmentDefinitions.ISA(), new SegmentDefinitions.IEA(),
            new SegmentDefinitions.GS(), new SegmentDefinitions.GE(),
            new SegmentDefinitions.ST(), new SegmentDefinitions.SE(),
            inboundEDIInfo.ISAReceiverQual, inboundEDIInfo.ISAReceiverId, inboundEDIInfo.ISASenderQual, inboundEDIInfo.ISASenderId, inboundEDIInfo.GSReceiverId, inboundEDIInfo.GSSenderId,
            inboundEDIInfo.ISAEdiVersion, inboundEDIInfo.GSEdiVersion, inboundEDIInfo.ISAUsageIndicator, 1, 1, inboundEDIInfo.SegmentSeparator, inboundEDIInfo.ElementSeparator, "U", ">");

            EdiDataWriter w = new EdiDataWriter(settings);

            edi_997 = w.WriteToString(b);

            return edi_997;
        }

        public static string Surgimac846Generate997(InboundEDIInfo inboundEDIInfo, EdiTrans ctlTrans, string DocumentType)
        {
            string edi_997 = string.Empty;

            Maps_4010.M_997 map = new Maps_4010.M_997();
            EdiTrans t = new EdiTrans(map);
            MapSegment l_SegmentDef = null;

            //AK1
            l_SegmentDef = (MapSegment)map.Content.First(s => s.Name == "AK1");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "IB", inboundEDIInfo.GSControlNumber })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            //AK2
            l_SegmentDef = (MapSegment)((MapLoop)map.Content.First(s => s.Name == "L_AK2")).Content.First(s => s.Name == "AK2");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { DocumentType, ctlTrans.ST.Content[1].Val })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            //AK5
            l_SegmentDef = (MapSegment)((MapLoop)map.Content.First(s => s.Name == "L_AK2")).Content.First(s => s.Name == "AK5");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "A" })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            //AK9
            l_SegmentDef = (MapSegment)map.Content.First(s => s.Name == "AK9");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "A", "1", "1", "1" })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            var g = new EdiGroup("FA");
            g.Transactions.Add(t);

            var i = new EdiInterchange();
            i.Groups.Add(g);

            EdiBatch b = new EdiBatch();
            b.Interchanges.Add(i);

            EdiDataWriterSettings settings = new EdiDataWriterSettings(
            new SegmentDefinitions.ISA(), new SegmentDefinitions.IEA(),
            new SegmentDefinitions.GS(), new SegmentDefinitions.GE(),
            new SegmentDefinitions.ST(), new SegmentDefinitions.SE(),
            inboundEDIInfo.ISAReceiverQual, inboundEDIInfo.ISAReceiverId, inboundEDIInfo.ISASenderQual, inboundEDIInfo.ISASenderId, inboundEDIInfo.GSReceiverId, inboundEDIInfo.GSSenderId,
            inboundEDIInfo.ISAEdiVersion, inboundEDIInfo.GSEdiVersion, inboundEDIInfo.ISAUsageIndicator, 1, 1, inboundEDIInfo.SegmentSeparator, inboundEDIInfo.ElementSeparator, "U", ">");

            EdiDataWriter w = new EdiDataWriter(settings);

            edi_997 = w.WriteToString(b);

            return edi_997;
        }

        public static string Generate997For810(InboundEDIInfo inboundEDIInfo, EdiTrans ctlTrans, string documentType)
        {
            string edi_997 = string.Empty;

            Maps_4010.M_997 map = new Maps_4010.M_997();
            EdiTrans t = new EdiTrans(map);
            MapSegment l_SegmentDef = null;

            // AK1
            l_SegmentDef = (MapSegment)map.Content.First(s => s.Name == "AK1");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "IN", inboundEDIInfo.GSControlNumber })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            // AK2
            l_SegmentDef = (MapSegment)((MapLoop)map.Content.First(s => s.Name == "L_AK2")).Content.First(s => s.Name == "AK2");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { documentType, ctlTrans.ST.Content[1].Val })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            // AK5
            l_SegmentDef = (MapSegment)((MapLoop)map.Content.First(s => s.Name == "L_AK2")).Content.First(s => s.Name == "AK5");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "A" })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            // AK9
            l_SegmentDef = (MapSegment)map.Content.First(s => s.Name == "AK9");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "A", "1", "1", "1" })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            EdiGroup g = new EdiGroup("FA");
            g.Transactions.Add(t);

            EdiInterchange i = new EdiInterchange();
            i.Groups.Add(g);

            EdiBatch b = new EdiBatch();
            b.Interchanges.Add(i);

            EdiDataWriterSettings settings = new EdiDataWriterSettings(
                new SegmentDefinitions.ISA(), new SegmentDefinitions.IEA(),
                new SegmentDefinitions.GS(), new SegmentDefinitions.GE(),
                new SegmentDefinitions.ST(), new SegmentDefinitions.SE(),
                inboundEDIInfo.ISAReceiverQual, inboundEDIInfo.ISAReceiverId, inboundEDIInfo.ISASenderQual, inboundEDIInfo.ISASenderId, inboundEDIInfo.GSReceiverId, inboundEDIInfo.GSSenderId,
                inboundEDIInfo.ISAEdiVersion, inboundEDIInfo.GSEdiVersion, inboundEDIInfo.ISAUsageIndicator, 1, 1, inboundEDIInfo.SegmentSeparator, inboundEDIInfo.ElementSeparator, "U", ">");

            EdiDataWriter w = new EdiDataWriter(settings);

            edi_997 = w.WriteToString(b);
            return edi_997;
        }

        public static string Generate997For856(InboundEDIInfo inboundEDIInfo, EdiTrans ctlTrans, string documentType)
        {
            string edi_997 = string.Empty;

            Maps_4010.M_997 map = new Maps_4010.M_997();
            EdiTrans t = new EdiTrans(map);
            MapSegment l_SegmentDef = null;

            // AK1 - Functional Group Response Header
            l_SegmentDef = (MapSegment)map.Content.First(s => s.Name == "AK1");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "SH", inboundEDIInfo.GSControlNumber }) // "SH" is typically used for shipping documents like 856
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            // AK2 - Transaction Set Response Header
            l_SegmentDef = (MapSegment)((MapLoop)map.Content.First(s => s.Name == "L_AK2")).Content.First(s => s.Name == "AK2");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { documentType, ctlTrans.ST.Content[1].Val })
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            // AK5 - Transaction Set Response Trailer
            l_SegmentDef = (MapSegment)((MapLoop)map.Content.First(s => s.Name == "L_AK2")).Content.First(s => s.Name == "AK5");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "A" })  // Assume 'A' for accepted
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            // AK9 - Functional Group Response Trailer
            l_SegmentDef = (MapSegment)map.Content.First(s => s.Name == "AK9");
            if (l_SegmentDef != null)
            {
                int l_Index = 0;
                var l_Segment = new EdiSegment(l_SegmentDef);

                foreach (string l_Value in new string[] { "A", "1", "1", "1" }) // Assuming a single transaction within the functional group
                {
                    l_Segment.Content.Add(new EdiSimpleDataElement((MapSimpleDataElement)l_SegmentDef.Content[l_Index++], l_Value));
                }

                t.Content.Add(l_Segment);
            }

            EdiGroup g = new EdiGroup("FA");
            g.Transactions.Add(t);

            EdiInterchange i = new EdiInterchange();
            i.Groups.Add(g);

            EdiBatch b = new EdiBatch();
            b.Interchanges.Add(i);

            EdiDataWriterSettings settings = new EdiDataWriterSettings(
                new SegmentDefinitions.ISA(), new SegmentDefinitions.IEA(),
                new SegmentDefinitions.GS(), new SegmentDefinitions.GE(),
                new SegmentDefinitions.ST(), new SegmentDefinitions.SE(),
                inboundEDIInfo.ISAReceiverQual, inboundEDIInfo.ISAReceiverId, inboundEDIInfo.ISASenderQual, inboundEDIInfo.ISASenderId, inboundEDIInfo.GSReceiverId, inboundEDIInfo.GSSenderId,
                inboundEDIInfo.ISAEdiVersion, inboundEDIInfo.GSEdiVersion, inboundEDIInfo.ISAUsageIndicator, 1, 1, inboundEDIInfo.SegmentSeparator, inboundEDIInfo.ElementSeparator, "U", ">");

            EdiDataWriter w = new EdiDataWriter(settings);

            edi_997 = w.WriteToString(b);
            return edi_997;
        }
    }
}
