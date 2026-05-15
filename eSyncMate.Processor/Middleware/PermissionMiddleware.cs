using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Data.SqlClient;

namespace eSyncMate.Processor.Middleware
{
    public class PermissionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;

        // These paths skip permission check entirely
        private static readonly HashSet<string> _publicPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "login", "refreshtoken", "logout", "verifymfa", "registeruser",
            "health", "swagger", "dashboard", "hangfire"
        };

        public PermissionMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context, IConfiguration config)
        {
            var path = context.Request.Path.Value?.TrimStart('/') ?? "";

            // Skip public endpoints
            if (_publicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Only check if user is authenticated (JWT present)
            var userIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "id");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                await _next(context);
                return;
            }

            // Check token blacklist (revoked/logged-out tokens)
            var jti = context.User.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;
            if (!string.IsNullOrEmpty(jti) && await IsTokenRevokedAsync(jti, config.GetConnectionString("DefaultConnection")))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Token has been revoked." });
                return;
            }

            // Load permissions from cache or DB
            var cacheKey = $"user_perms_{userId}";
            if (!_cache.TryGetValue(cacheKey, out List<MenuPermission> permissions))
            {
                permissions = await LoadPermissionsAsync(
                    userId,
                    config.GetConnectionString("DefaultConnection")
                );
                _cache.Set(cacheKey, permissions, TimeSpan.FromMinutes(5));
            }

            // Match current request path to a menu
            var matched = permissions?.FirstOrDefault(p =>
                !string.IsNullOrEmpty(p.ApiPrefix) &&
                path.StartsWith(p.ApiPrefix, StringComparison.OrdinalIgnoreCase));

            // If path belongs to a menu, check the required permission
            if (matched != null)
            {
                bool allowed = ResolvePermission(path, context.Request.Method, matched);
                if (!allowed)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "Access denied. You don't have permission for this action."
                    });
                    return;
                }
            }

            await _next(context);
        }

        // Decide which permission flag to check based on URL keywords
        private static bool ResolvePermission(string path, string method, MenuPermission perm)
        {
            var lower = path.ToLower();

            if (lower.Contains("delete") || lower.Contains("remove"))
                return perm.CanDelete;

            if (lower.Contains("update") || lower.Contains("edit") ||
                lower.Contains("modify") || lower.Contains("save") ||
                lower.Contains("changeblock") || lower.Contains("changeenable") ||
                lower.Contains("changestatus") || lower.Contains("updatepassword"))
                return perm.CanEdit;

            if (lower.Contains("add") || lower.Contains("create") ||
                lower.Contains("insert") || lower.Contains("register"))
                return perm.CanAdd;

            return perm.CanView;
        }

        private static async Task<bool> IsTokenRevokedAsync(string jti, string connectionString)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand(
                    "SELECT COUNT(1) FROM TokenBlacklist WHERE TokenJti = @jti AND ExpiresAt > GETUTCDATE()", conn);
                cmd.Parameters.AddWithValue("@jti", jti);
                var count = (int)await cmd.ExecuteScalarAsync();
                return count > 0;
            }
            catch { return false; }
        }

        private static async Task<List<MenuPermission>> LoadPermissionsAsync(
            int userId, string connectionString)
        {
            var result = new List<MenuPermission>();
            try
            {
                using var conn = new SqlConnection(connectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand("Sp_GetUserPermissions", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new MenuPermission
                    {
                        MenuId    = reader.GetInt32(0),
                        ApiPrefix = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        CanView   = reader.GetBoolean(2),
                        CanAdd    = reader.GetBoolean(3),
                        CanEdit   = reader.GetBoolean(4),
                        CanDelete = reader.GetBoolean(5)
                    });
                }
            }
            catch
            {
                // On DB error, allow request through (fail open) — log in production
            }
            return result;
        }
    }

    public class MenuPermission
    {
        public int    MenuId    { get; set; }
        public string ApiPrefix { get; set; } = "";
        public bool   CanView   { get; set; }
        public bool   CanAdd    { get; set; }
        public bool   CanEdit   { get; set; }
        public bool   CanDelete { get; set; }
    }
}
