using eSyncMate.Processor.Models;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

// -------------------- CORS --------------------
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()));

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

var app = builder.Build();

// -------------------- FORWARDED HEADERS --------------------
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// -------------------- BIND APP SETTINGS TO STATICS --------------------
CommonUtils.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
CommonUtils.Company = builder.Configuration["CompanyName"];
CommonUtils.UploadInventoryTotalThread = Convert.ToInt32(builder.Configuration["UploadInventoryTotalThread"]);
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

// -------------------- MIDDLEWARE ORDER --------------------
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

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
