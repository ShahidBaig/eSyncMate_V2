using static eSyncMate.Processor.Models.SCSPlaceOrderResponseModel;

namespace eSyncMate.Processor.Models
{
    public class SCSPlaceOrderResponseModel
    {
        public class SCSPlaceOrderResponse
        {
            public Output OutPut { get; set; }
        }

        public class Output
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public List<Errordetail> ErrorDetail { get; set; }
            public Output()
            {
                this.ErrorDetail = new List<Errordetail>();
            }
            
        
            public string ObjectID { get; set; }
        }

        public class Errordetail
        {
            public string ErrorNo { get; set; }
            public string ErrorDescription { get; set; }
        }


    }
}
