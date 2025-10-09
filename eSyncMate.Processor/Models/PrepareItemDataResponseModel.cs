using System.Data;

namespace eSyncMate.Processor.Models
{
    public class PrepareItemDataResponseModel : ResponseModel
    {
        public DataTable ItemDataResponseDatatable { get; set; }
    }
}
