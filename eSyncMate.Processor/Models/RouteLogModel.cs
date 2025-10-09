using Microsoft.VisualBasic;

namespace eSyncMate.Processor.Models
{
    public class RouteLogModel
    {
        public int Id { get; set; }
    }

    public class RouteLogDataModel
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public int Type { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string Details { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }


    }

    public class GetinvFeedFromNDCDataModel
    {
        public int Id { get; set; }              // Corresponds to the "Id" column
        public int OrderId { get; set; }         // Corresponds to the "OrderId" column
        public DateTime CreatedAt { get; set; }  // Corresponds to the "CreatedAt" column
        public int WarehouseId { get; set; }     // Corresponds to the "Warehoused" column
        public string WarehouseName { get; set; } // Corresponds to the "WarehouseName" column
        public string SKUCode { get; set; }      // Corresponds to the "SKUCode" column
        public string Title { get; set; }        // Corresponds to the "Title" column
        public decimal PricePerUnit { get; set; } // Corresponds to the "PricePerUnit" column
        public int Quantity { get; set; }        // Corresponds to the "Quantity" column
        public decimal Profit { get; set; }      // Corresponds to the "Profit" column
        public decimal Margin { get; set; }      // Corresponds to the "Margin" column
    }


    public class SaveRouteLogDataModel
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public int Type { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
    }

    public class UpdateRoutLogDataModel
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public int Type { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int ModifiedBy { get; set; }
    }

    public class RouteLogSearchModel
    {
        public string SearchOption { get; set; }
        public string SearchValue { get; set; }
    }
}
