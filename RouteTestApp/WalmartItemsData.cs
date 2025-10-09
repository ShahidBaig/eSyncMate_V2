using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteTestApp
{
    public class WalmartItemsData
    {
        public List<Itemresponse> ItemResponse { get; set; }
        public int totalItems { get; set; }
        public string nextCursor { get; set; }

        public WalmartItemsData() 
        { 
           this.ItemResponse = new List<Itemresponse>();    
        }
    }

    public class Itemresponse
    {
        public string mart { get; set; }
        public string sku { get; set; }
        public string condition { get; set; }
        public string wpid { get; set; }
        public string upc { get; set; }
        public string gtin { get; set; }
        public string productName { get; set; }
        public string shelf { get; set; }
        public string productType { get; set; }
        public WalmartPrice price { get; set; }
        public string publishedStatus { get; set; }
        public string lifecycleStatus { get; set; }
        public bool isDuplicate { get; set; }
        public string variantGroupId { get; set; }
        public Variantgroupinfo variantGroupInfo { get; set; }
        public Unpublishedreasons unpublishedReasons { get; set; }
    }

    public class WalmartPrice
    {
        public string currency { get; set; }
        public float amount { get; set; }
    }

    public class Variantgroupinfo
    {
        public bool isPrimary { get; set; }
        public List<Groupingattribute> groupingAttributes { get; set; }

        public Variantgroupinfo()
        {
            this.groupingAttributes = new List<Groupingattribute>();
        }
    }

    public class Groupingattribute
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class Unpublishedreasons
    {
        public string[] reason { get; set; }
    }

}
