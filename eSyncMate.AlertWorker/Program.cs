using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Managers;
using eSyncMate.Processor.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace eSyncMate.AlertWorker
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // TO RUN A HARDCODED TEST:
            // 1. Uncomment the lines below.
            // 2. Change 9 to your desired Alert ID.
            // 3. Run the project (F5 or dotnet run).

            // await ExecuteAlert(9);
            // Console.WriteLine("Test completed. Press any key to exit.");
            // Console.ReadKey();
            // return 0;

            var alertIdOption = new Option<int>(
                name: "--alertId",
                description: "The Alert ID to execute")
            { IsRequired = true };

            var rootCommand = new RootCommand("eSyncMate Alert Worker - Executes alerts in isolated process");
            rootCommand.AddOption(alertIdOption);

            rootCommand.SetHandler(async (alertId) =>
            {
                await ExecuteAlert(alertId);
            }, alertIdOption);

            return await rootCommand.InvokeAsync(args);
        }

        static async Task ExecuteAlert(int alertId)
        {
            try
            {
                var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(exeDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                CommonUtils.ConnectionString = configuration.GetConnectionString("DefaultConnection");

                AlertEngine engine = new AlertEngine(configuration);
                await engine.ExecuteLocal(alertId);
            }
            catch (Exception ex)
            {
                Environment.ExitCode = 1;
            }
        }
    }
}
