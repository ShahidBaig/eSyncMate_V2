using eSyncMate.DB.Entities;

namespace eSyncMate.Processor.Models
{
    public class GetProductUploadPrices : ResponseModel
    {
        public List<ProductUploadPricesDataModel> ProductUploadPrices { get; set; }
    }
}
