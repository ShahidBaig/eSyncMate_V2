using eSyncMate.Processor.Models;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.AspNetCore.HttpOverrides;   // HA / Nginx reverse proxy — uncomment for Nginx deployment
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
// using System.Net;                            // HA / Nginx reverse proxy — uncomment for Nginx deployment
using System.Text;
using Hangfire.Dashboard;

var builder = WebApplication.CreateBuilder(args);

// -------------------- HANGFIRE --------------------
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"),
        new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
builder.Services.AddHangfireServer(); // background workers

// -------------------- MVC / JSON --------------------
builder.Services.AddControllers().AddNewtonsoftJson();

// -------------------- SWAGGER --------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "eSyncMate Processor API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header. Example: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// -------------------- MEMORY CACHE (for permission caching) --------------------
builder.Services.AddMemoryCache();

// -------------------- CORS --------------------
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// -------------------- JWT AUTH --------------------
builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    // you’re behind Nginx on HTTP → keep false
    o.RequireHttpsMetadata = false;
    o.SaveToken = true;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        SaveSigninToken = true,
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Issuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// -------------------- FORWARDED HEADERS (for Nginx reverse proxy) --------------------
// HA / Nginx mode — uncomment the block below + above `using` directives when deploying behind Nginx.
// Trust X-Forwarded-For / X-Forwarded-Proto only from configured Nginx IPs (NginxProxies in appsettings).
// var nginxIPs = builder.Configuration.GetSection("NginxProxies").Get<string[]>() ?? Array.Empty<string>();
// builder.Services.Configure<ForwardedHeadersOptions>(options =>
// {
//     options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
//     options.KnownProxies.Clear();
//     foreach (var ip in nginxIPs)
//     {
//         if (IPAddress.TryParse(ip, out var parsed))
//             options.KnownProxies.Add(parsed);
//     }
// });

var app = builder.Build();

// app.UseForwardedHeaders();   // HA / Nginx mode — uncomment when deploying behind Nginx

// -------------------- BIND APP SETTINGS TO STATICS --------------------
CommonUtils.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
CommonUtils.EncryptionKey   = builder.Configuration["EncryptionKey"] ?? "";
CommonUtils.Company = builder.Configuration["CompanyName"];
CommonUtils.UploadInventoryTotalThread = Convert.ToInt32(builder.Configuration["UploadInventoryTotalThread"]);
CommonUtils.AmazonFeedMaxMessages = Convert.ToInt32(builder.Configuration["AmazonFeedMaxMessages"] ?? "20000");
CommonUtils.MySqlConnectionString = builder.Configuration["MySQLConnection"];


CommonUtils.SMTPHost = builder.Configuration["SMTPHost"];
CommonUtils.SMTPPort = Convert.ToInt32(builder.Configuration["SMTPPort"]);
CommonUtils.FromEmailAccount = builder.Configuration["FromEmailAccount"];
CommonUtils.FromEmailPWD = builder.Configuration["FromEmailPWD"];

// -------------------- SWAGGER --------------------
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// -------------------- SECURITY HEADERS --------------------
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"]  = "nosniff";
    context.Response.Headers["X-XSS-Protection"]        = "1; mode=block";
    context.Response.Headers["Referrer-Policy"]         = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"]      = "camera=(), microphone=(), geolocation=()";

    bool isDashboard = context.Request.Path.StartsWithSegments("/dashboard");
    if (isDashboard)
    {
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; img-src 'self' data:; font-src 'self' data: https://cdnjs.cloudflare.com; connect-src 'self' ws: wss:;";
    }
    else
    {
        context.Response.Headers["X-Frame-Options"]         = "DENY";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    }

    if (context.Request.IsHttps)
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    await next();
});

// -------------------- MIDDLEWARE ORDER --------------------
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<eSyncMate.Processor.Middleware.PermissionMiddleware>();

// -------------------- HANGFIRE DASHBOARD --------------------
// (A) Secure with basic auth (recommended for server)
//app.UseHangfireDashboard("/dashboard", new DashboardOptions
//{
//    Authorization = new[]
//    {
//        new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
//        {
//            RequireSsl       = false, // set true if you serve HTTPS
//            SslRedirect      = false,
//            LoginCaseSensitive = true,
//            Users = new[]
//            {
//                new BasicAuthAuthorizationUser
//                {
//                    Login = "admin",
//                    PasswordClear = "ChangeMeStrong#2025"
//                }
//            }
//        })
//    }
//});

//(B)For quick tests only, allow everyone:
app.UseHangfireDashboard("/dashboard", new DashboardOptions
{
    Authorization = Array.Empty<IDashboardAuthorizationFilter>() // or a custom filter returning true
});


// -------------------- ROUTES --------------------
app.MapControllers();

// -------------------- RUN --------------------
app.Run();
