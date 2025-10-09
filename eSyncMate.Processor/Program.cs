using eSyncMate.Processor.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using System.Text;

namespace eSyncMate.Processor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    var builder = WebApplication.CreateBuilder(args);

                    builder.Services.AddHangfire(configuration => configuration
                           .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                           .UseSimpleAssemblyNameTypeSerializer()
                           .UseRecommendedSerializerSettings()
                           .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
                           {
                               CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                               SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                               QueuePollInterval = TimeSpan.Zero,
                               UseRecommendedIsolationLevel = true,
                               DisableGlobalLocks = true
                           }));
                    builder.Services.AddHangfireServer();

                    // Add services to the container.

                    builder.Services.AddControllers().AddNewtonsoftJson();
                    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                    builder.Services.AddEndpointsApiExplorer();
                    builder.Services.AddSwaggerGen(c => {
                        c.SwaggerDoc("v1", new OpenApiInfo
                        {
                            Title = "JWTToken_Auth_API",
                            Version = "v1"
                        });
                        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                        {
                            Name = "Authorization",
                            Type = SecuritySchemeType.ApiKey,
                            Scheme = "Bearer",
                            BearerFormat = "JWT",
                            In = ParameterLocation.Header,
                            Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
                        });
                        c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
                    });

                    builder.Services.AddCors(options =>
                    {
                        options.AddDefaultPolicy(builder =>
                        {
                            builder.AllowAnyOrigin()
                                   .AllowAnyHeader()
                                   .AllowAnyMethod();
                        });
                    });

                    IConfigurationRoot configuration = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json")
                                .Build();

                    builder.Services.AddAuthentication(option =>
                    {
                        option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                        option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    }).AddJwtBearer(option =>
                    {
                        option.RequireHttpsMetadata = true;
                        option.SaveToken = true;
                        option.TokenValidationParameters = new TokenValidationParameters
                        {
                            SaveSigninToken = true,
                            ValidateIssuer = true,
                            ValidateAudience = false,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = configuration["Jwt:Issuer"],       // Jwt:Issuer - config value 
                            ValidAudience = configuration["Jwt:Issuer"],     // Jwt:Issuer - config value 
                            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:Key"])) // Jwt:Key - config value 
                        };
                    });

                    var app = builder.Build();

                    app.UseForwardedHeaders(new ForwardedHeadersOptions
                    {
                        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                    });

                    CommonUtils.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
					CommonUtils.Company = builder.Configuration["CompanyName"];
                    CommonUtils.UploadInventoryTotalThread = Convert.ToInt32(builder.Configuration["UploadInventoryTotalThread"]);
                    CommonUtils.MySqlConnectionString = builder.Configuration["MySQLConnection"];

                    // Configure the HTTP request pipeline.
                    if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
                    {
                        app.UseSwagger();
                        app.UseSwaggerUI();
                    }
                    else
                    {
                        app.UseHttpsRedirection();
                    }

                    app.UseHangfireDashboard("/dashboard");
                    app.UseCors();

                    app.UseAuthorization();

                    app.MapControllers();

                    app.Run();

                    webBuilder.UseStartup<Startup>();
                });
    }
}