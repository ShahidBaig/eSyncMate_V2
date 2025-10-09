namespace eSyncMate.Processor.Models
{
    public class WalmartInputCancellationModel
    {
        public Ordercancellation orderCancellation { get; set; }

        public class Ordercancellation
        {
            public Orderlines orderLines { get; set; }
        }

        public class Orderlines
        {
            public List<Orderline> orderLine { get; set; }

            public Orderlines()
            {
                this.orderLine = new List<Orderline>(); 
            }
        }

        public class Orderline
        {
            public string lineNumber { get; set; }
            public Orderlinestatuses orderLineStatuses { get; set; }
        }

        public class Orderlinestatuses
        {
            public List<Orderlinestatus> orderLineStatus { get; set; }

            public Orderlinestatuses()
            {
                this.orderLineStatus = new List<Orderlinestatus>(); 
            }
        }

        public class Orderlinestatus
        {
            public string status { get; set; }
            public string cancellationReason { get; set; }
            public Statusquantity statusQuantity { get; set; }
        }

        public class Statusquantity
        {
            public string unitOfMeasurement { get; set; }
            public string amount { get; set; }
        }

    }
}
