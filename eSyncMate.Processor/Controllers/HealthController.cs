using System.Data.SqlClient;
using eSyncMate.Processor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eSyncMate.Processor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public HealthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = new
            {
                status = "Healthy",
                timestamp = DateTime.Now,
                utcTimestamp = DateTime.UtcNow,
                server = Environment.MachineName,
                database = await CheckDatabase(),
                hangfire = await CheckHangfireDatabase(),
                uptime = GetUptime()
            };

            var isHealthy = result.database.connected && result.hangfire.connected;

            if (!isHealthy)
                return StatusCode(503, result with { status = "Unhealthy" });

            return Ok(result);
        }

        private async Task<dynamic> CheckDatabase()
        {
            try
            {
                using var conn = new SqlConnection(CommonUtils.ConnectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT 1", conn);
                await cmd.ExecuteScalarAsync();
                return new { connected = true, error = (string?)null };
            }
            catch (Exception ex)
            {
                return new { connected = false, error = ex.Message };
            }
        }

        private async Task<dynamic> CheckHangfireDatabase()
        {
            try
            {
                var hangfireConn = _config.GetConnectionString("HangfireConnection");
                using var conn = new SqlConnection(hangfireConn);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("SELECT COUNT(*) FROM [HangFire].[Server] WITH (NOLOCK)", conn);
                var serverCount = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                return new { connected = true, activeServers = serverCount, error = (string?)null };
            }
            catch (Exception ex)
            {
                return new { connected = false, activeServers = 0, error = ex.Message };
            }
        }

        private static string GetUptime()
        {
            var uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime;
            return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
        }
    }
}
