using Newtonsoft.Json;

namespace eSyncMate.Processor.Models
{
    public class SegmentNode
    {
        public string Name { get; set; }
        public List<string> Data { get; set; }
    }

    public class Items : List<LoopItem>
    {
    }

    public class LoopItem
    {
        public List<SegmentNode> Data { get; set; }
    }

    public class LoopPackage
    {
        public List<SegmentNode> Data { get; set; }
        public Items Items { get; set; }
    }

    public class Packs : List<LoopPackage>
    {
    }

    public class LoopOrder
    {
        public List<SegmentNode> Data { get; set; }
        public Packs Packs { get; set; }
    }

    public class StoreOrders : List<LoopOrder>
    {
    }

    public class ASN
    {
        public List<SegmentNode> StartNodes { get; set; }
        public Packs Packs { get; set; }
        public StoreOrders Orders { get; set; }
        public List<SegmentNode> EndNodes { get; set; }
    }

    public class Invoice810
    {
        public List<SegmentNode> StartNodes { get; set; }
        public Items Items { get; set; }
        public List<SegmentNode> EndNodes { get; set; }
    }

    public class OrderStatus855
    {
        public List<SegmentNode> StartNodes { get; set; }
        public Items Items { get; set; }
        public List<SegmentNode> EndNodes { get; set; }
    }

    public class CarrierLoadTenderAckowledgement990
    {
        public List<SegmentNode> StartNodes { get; set; }
    }

    public class CarrierLoadTenderResponse214
    {
        public List<SegmentNode> StartNodes { get; set; }
        public PacksWrapper Packs { get; set; }
    }
        
    public class CLTPacks
    {
        public List<SegmentNode> Data { get; set; }
    }

    public class PacksWrapper
    {
        public List<SegmentNode> Data { get; set; }
    }


    public class Vecko850PurchaseOrder
    {
        public List<SegmentNode> StartNodes { get; set; }
        public PacksWrapper Lines { get; set; }
    }

}
