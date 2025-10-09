using Org.BouncyCastle.Bcpg;
using System.Data;

namespace eSyncMate.Processor.Models
{
    public class GetInvFeedFromNDCResponseModel : ResponseModel
    {
        //public List<InvFeedFromNDCDataModel> Inv { get; set; }

        public DataTable Inv { get; set; }
    }
}
