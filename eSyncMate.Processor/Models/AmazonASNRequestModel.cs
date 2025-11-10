namespace eSyncMate.Processor.Models
{
    public class AmazonASNRequestModel
    {
        public string marketplaceId { get; set; }
        public AmazonPackagedetail packageDetail { get; set; } = new AmazonPackagedetail();

        public class AmazonPackagedetail
        {
            public string packageReferenceId { get; set; }
            public string carrierCode { get; set; }
            public string trackingNumber { get; set; }
            public string shipDate { get; set; }
            public List<AmazonOrderitem> orderItems { get; set; }
        
            public AmazonPackagedetail()
            {
                this.orderItems = new List<AmazonOrderitem>();
            }

        }

        public class AmazonOrderitem
        {
            public string orderItemId { get; set; }
            public string quantity { get; set; }
        }

    }
}
