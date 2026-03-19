using eSyncMate.Processor.Models;
using System.Data;
using System.Security.Claims;
using System.Linq;
using eSyncMate.DB.Entities;
using eSyncMate.DB;
using Hangfire;
using Microsoft.Extensions.Configuration;

namespace eSyncMate.Processor.Managers
{
    public class FlowsManager
    {
        public static UsersClaimData GetCustomerNames(ClaimsIdentity claimsIdentity)
        {
            UsersClaimData userData = new UsersClaimData();

            var customerNameClaim = claimsIdentity.FindFirst("customerName")?.Value;

            if (!string.IsNullOrEmpty(customerNameClaim))
            {
                string[] valuesArray = customerNameClaim.Split(',').Select(id => $"'{id.Trim()}'").ToArray();
                userData.Flows = string.Join(",", valuesArray);
            }

            userData.UserType = claimsIdentity.FindFirst("userType")?.Value;

            return userData;
        }

        public static int GetUserId(ClaimsIdentity identity)
        {
            var l_UserIdStr = identity?.FindFirst("id")?.Value;
            return l_UserIdStr != null ? int.Parse(l_UserIdStr) : 0;
        }

        public static Result ProcessUpdateDetail(EditFlowDataModel flowModel, SaveFlowDetailsDataModel detailModel, Dictionary<int, DataRow> existingRows, DBConnector connection, int userId, IConfiguration config)
        {
            if (detailModel.RouteId == null) return Result.GetSuccessResult();

            int l_RouteId = detailModel.RouteId.Value;
            FlowDetails l_Row = new();
            l_Row.UseConnection(string.Empty, connection);

            if (existingRows.ContainsKey(l_RouteId))
            {
                DataRow l_ExistingRow = existingRows[l_RouteId];
                l_Row.Id = Convert.ToInt64(l_ExistingRow["Id"]);
                l_Row.FlowId = flowModel.Id;
                l_Row.RouteId = l_RouteId;
                l_Row.Status = detailModel.Status;
                l_Row.ModifiedDate = DateTime.Now;
                l_Row.ModifiedBy = flowModel.ModifiedBy ?? 0;
                l_Row.CreatedDate = Convert.ToDateTime(l_ExistingRow["CreatedDate"]);
                l_Row.CreatedBy = Convert.ToInt32(l_ExistingRow["CreatedBy"]);

                l_Row.In_Out = detailModel.In_Out;
                l_Row.FrequencyType = detailModel.FrequencyType;
                l_Row.StartDate = detailModel.StartDate;
                l_Row.EndDate = detailModel.EndDate;
                l_Row.RepeatCount = detailModel.RepeatCount;
                l_Row.WeekDays = detailModel.WeekDays;
                l_Row.OnDay = detailModel.OnDay;
                l_Row.ExecutionTime = detailModel.ExecutionTime;
            }
            else
            {
                PublicFunctions.CopyTo(detailModel, l_Row);
                l_Row.FlowId = flowModel.Id;
                l_Row.CreatedDate = DateTime.Now;
                l_Row.CreatedBy = flowModel.ModifiedBy ?? 0;
                l_Row.ModifiedDate = DateTime.Now;
                l_Row.ModifiedBy = flowModel.ModifiedBy ?? 0;
                // Ensure no null fields that DB rejects
                l_Row.Status = l_Row.Status ?? "Active";
                l_Row.In_Out = l_Row.In_Out ?? "";
                l_Row.FrequencyType = l_Row.FrequencyType ?? "";
                l_Row.WeekDays = l_Row.WeekDays ?? "";
                l_Row.OnDay = l_Row.OnDay ?? "";
                l_Row.ExecutionTime = l_Row.ExecutionTime ?? "";
                if (l_Row.StartDate == default(DateTime)) l_Row.StartDate = new DateTime(1900, 1, 1);
                if (l_Row.EndDate == default(DateTime)) l_Row.EndDate = new DateTime(1900, 1, 1);
            }

            eSyncMate.DB.Entities.Routes l_RoutesEntity = new();
            l_RoutesEntity.UseConnection(string.Empty, connection);
            DataTable l_RoutesDT = new DataTable();
            l_RoutesEntity.GetList($"Id = {l_RouteId}", "*", ref l_RoutesDT);

            string l_OldJobId = null;
            string l_NewJobID = null;

            if (l_RoutesDT.Rows.Count > 0)
            {
                DataRow l_RouteRow = l_RoutesDT.Rows[0];
                string l_OldStatus = l_RouteRow["Status"]?.ToString();
                l_OldJobId = l_RouteRow["JobID"]?.ToString();
                l_NewJobID = l_OldJobId;

                RouteEngine l_Engine = new RouteEngine(config);
                string l_DetailStatus = (detailModel.Status ?? "active").ToLower();

                if (l_DetailStatus == "active")
                {
                    if (!string.IsNullOrEmpty(l_OldJobId)) BackgroundJob.Delete(l_OldJobId);
                    var l_StartDate = detailModel.StartDate == default(DateTime) ? DateTime.Now : detailModel.StartDate;
                    l_NewJobID = l_Engine.ScheduleWaitJob(new Routes { Id = l_RouteId, StartDate = l_StartDate });
                }
                else if ((l_OldStatus ?? "").Equals("active", StringComparison.CurrentCultureIgnoreCase) && l_DetailStatus != "active")
                {
                    if (!string.IsNullOrEmpty(l_OldJobId)) BackgroundJob.Delete(l_OldJobId);
                    l_NewJobID = null;
                }
            }

            if (l_Row.Id > 0)
            {
                return l_Row.UpdateWithRoute(userId, l_OldJobId, l_NewJobID);
            }
            else
            {
                return l_Row.SaveWithRoute(userId, l_OldJobId, l_NewJobID);
            }
        }
    }
}
