namespace eSyncMate.Processor.Models
{
    /// <summary>Item ids pasted or read from the uploaded CSV. Deletion is item-id based across all customers.</summary>
    public class DeleteProductsRequestModel
    {
        public List<string>? ItemIDs { get; set; }
    }

    public class DeleteProductsPreviewRow
    {
        public string ItemID { get; set; }
        public int ProductCount { get; set; }
        public int CustomerCount { get; set; }
        public string Customers { get; set; }
    }

    public class DeleteProductsPreviewResponseModel : ResponseModel
    {
        /// <summary>Item ids that exist in the catalog, with where they were found.</summary>
        public List<DeleteProductsPreviewRow> Found { get; set; }

        /// <summary>Item ids from the file that match nothing.</summary>
        public List<string> NotFound { get; set; }

        public int TotalItemIDs { get; set; }
        public int TotalProducts { get; set; }
    }

    public class DeleteProductsResponseModel : ResponseModel
    {
        public int DeletedProducts { get; set; }
        public int DeletedProductData { get; set; }
    }
}
