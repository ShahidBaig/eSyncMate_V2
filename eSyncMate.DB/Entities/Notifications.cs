using System;
using System.Data;

namespace eSyncMate.DB.Entities
{
    public class Notifications
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public int RouteId { get; set; }
        public string RouteName { get; set; } = "";
        public string Type { get; set; } = "TEST_RUN";
        public string Status { get; set; } = "RUNNING";
        public string Message { get; set; } = "";
        public bool IsRead { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? CompletedDate { get; set; }

        public static long CreateNotification(string connectionString, int userId, int routeId, string routeName, string type, string status, string message)
        {
            var conn = new DBConnector(connectionString);
            var dt = new DataTable();
            string utcNow = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            string query = $@"INSERT INTO Notifications (UserId, RouteId, RouteName, [Type], [Status], [Message], IsRead, CreatedDate)
                OUTPUT INSERTED.Id
                VALUES ({userId}, {routeId}, '{routeName.Replace("'", "''")}', '{type}', '{status}', '{message.Replace("'", "''")}', 0, '{utcNow}')";
            conn.GetData(query, ref dt);
            return dt.Rows.Count > 0 ? Convert.ToInt64(dt.Rows[0][0]) : 0;
        }

        public static void UpdateNotification(string connectionString, long notificationId, string status, string message)
        {
            var conn = new DBConnector(connectionString);
            string utcNow = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            conn.Execute($@"UPDATE Notifications SET [Status] = '{status}', [Message] = '{message.Replace("'", "''")}', CompletedDate = '{utcNow}' WHERE Id = {notificationId}");
        }

        public static void MarkAsRead(string connectionString, long notificationId, int userId)
        {
            var conn = new DBConnector(connectionString);
            conn.Execute($"UPDATE Notifications SET IsRead = 1 WHERE Id = {notificationId} AND UserId = {userId}");
        }

        public static void MarkAllAsRead(string connectionString, int userId)
        {
            var conn = new DBConnector(connectionString);
            conn.Execute($"UPDATE Notifications SET IsRead = 1 WHERE UserId = {userId} AND IsRead = 0");
        }

        public static DataTable GetUserNotifications(string connectionString, int userId, int limit = 20)
        {
            var conn = new DBConnector(connectionString);
            var dt = new DataTable();
            string query = $@"SELECT TOP {limit} Id, UserId, RouteId, RouteName, [Type], [Status], [Message], IsRead, CreatedDate, CompletedDate
                FROM Notifications WHERE UserId = {userId} ORDER BY CreatedDate DESC";
            conn.GetData(query, ref dt);
            return dt;
        }

        public static int GetUnreadCount(string connectionString, int userId)
        {
            var conn = new DBConnector(connectionString);
            var dt = new DataTable();
            conn.GetData($"SELECT COUNT(*) FROM Notifications WHERE UserId = {userId} AND IsRead = 0", ref dt);
            return dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0][0]) : 0;
        }
    }
}
