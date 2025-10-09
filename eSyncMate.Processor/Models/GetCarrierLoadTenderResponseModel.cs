using System.Data;
using static eSyncMate.Processor.Models.CarrierLoadTenderViewModel;

namespace eSyncMate.Processor.Models
{
    public class GetCarrierLoadTenderResponseModel : ResponseModel
    {
        public List<CarrierLoadTenderDataModel> CarrierLoadTender { get; set; }
        public DataTable CarrierLoadTenderData { get; set; }
    }

    public class GetCarrierLoadTenderFilesResponseModel : ResponseModel
    {
        public List<CarrierLoadTenderFileModel> Files { get; set; }
    }

    public class GetCarrierLoadTenderAckDataResponseModel : ResponseModel
    {
        public List<CarrierLoadTenderAckDataModel> AckData { get; set; }
    }

    public class GetEdiFilesCounterResponseModel : ResponseModel
    {
        public List<EdiFilesCounterDataModel> EdiCounterData { get; set; }
    }

    public class GetSatesDataResponseModel : ResponseModel
    {
        public List<StatesDataModel> StatesData { get; set; }
    }

    public class GetCustomerWiseShipmentIDResponseModel : ResponseModel
    {
        public DataTable CustomerWiseShipmentID { get; set; }
        public DataTable CustomerWiseShipperNo { get; set; }

    }
}
