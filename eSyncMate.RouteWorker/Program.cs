using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Managers;
using eSyncMate.Processor.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace eSyncMate.RouteWorker
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Define command line options
            var routeIdOption = new Option<int>(
                name: "--routeId",
                description: "The Route ID to execute")
            { IsRequired = true };

            var rootCommand = new RootCommand("eSyncMate Route Worker - Executes routes in isolated process");
            rootCommand.AddOption(routeIdOption);

            rootCommand.SetHandler(async (routeId) =>
            {
                await ExecuteRoute(routeId);
            }, routeIdOption);

            return await rootCommand.InvokeAsync(args);
        }

        static async Task ExecuteRoute(int routeId)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ========================================");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting Route Worker for Route ID: {routeId}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ========================================");

            // Debug: Show paths for testing
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DEBUG - Current Directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DEBUG - Exe Directory: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DEBUG - Exe Location: {System.Reflection.Assembly.GetExecutingAssembly().Location}");

            try
            {
                // Build configuration - use executable directory, not current directory
                var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DEBUG - Config Path: {Path.Combine(exeDirectory, "appsettings.json")}");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DEBUG - Config Exists: {File.Exists(Path.Combine(exeDirectory, "appsettings.json"))}");

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(exeDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                // Set connection string
                CommonUtils.ConnectionString = configuration.GetConnectionString("DefaultConnection");

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Connection string loaded");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DEBUG - Connection: {CommonUtils.ConnectionString?.Substring(0, Math.Min(50, CommonUtils.ConnectionString?.Length ?? 0))}...");

                // Create logger factory
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                var logger = loggerFactory.CreateLogger<Program>();

                // Get route details
                Routes route = new Routes();
                route.UseConnection(CommonUtils.ConnectionString);
                route.Id = routeId;

                if (!route.GetObject().IsSuccess)
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: Invalid Route ID: {routeId}");
                    Environment.ExitCode = 1;
                    return;
                }

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Route Name: {route.Name}");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Route Type: {route.TypeId}");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Route Status: {route.Status}");

                //if (route.Status.ToUpper() == "IN-ACTIVE")
                //{
                //    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARNING: Route is inactive, skipping execution");
                //    Environment.ExitCode = 0;
                //    return;
                //}

                // Execute the route based on type
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting route execution...");

                ExecuteRouteByType(configuration, logger, route);

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ========================================");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Route {routeId} completed successfully");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ========================================");
                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ========================================");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR executing Route {routeId}");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ========================================");
                Console.WriteLine(ex.ToString());
                Environment.ExitCode = 1;
            }
        }

        static void ExecuteRouteByType(IConfiguration config, ILogger logger, Routes route)
        {
            int userNo = 1; // System user

            route.SaveLog(Declarations.LogTypeEnum.Info, $"[RouteWorker] Started executing route [{route.Id}] - {route.Name}", string.Empty, userNo);

            try
            {
                // Execute based on route type - same logic as RouteEngine
                if (route.TypeId == Convert.ToInt32(RouteTypesEnum.InventoryFeed))
                {
                    InventoryFeedRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GetOrders))
                {
                    GetOrdersRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CreateOrder))
                {
                    CreateOrderRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GetOrderStatus))
                {
                    GetOrderStatusRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ASN))
                {
                    ASNRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CreateInvoice))
                {
                    CreateInvoiceRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSFullInventoryFeed) || route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSDifferentialInventoryFeed))
                {
                    SCSFullInventoryFeedRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSPlaceOrder))
                {
                    SCSPlaceOrderRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSOrderStatus))
                {
                    SCSOrderStatusRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSASN))
                {
                    SCSASNRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSInvoice))
                {
                    SCSInvoiceRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSGetOrders))
                {
                    SCSGetOrders.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSItemPrices))
                {
                    SCSItemPrices.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSUpdateInventory))
                {
                    SCSUpdateInventory.Execute(config, logger, route);
                }
                // Amazon Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonInventoryUpload))
                {
                    AmazonUploadInventoryRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonGetOrders))
                {
                    AmazonGetOrdersRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonInventoryStatus))
                {
                    AmazonInventoryStatusRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonASNShipmentNotification))
                {
                    AmazonASNShipmentNotificationRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonWHSWInventoryUpload))
                {
                    AmazonUploadWarehouseWiseInventoryRoute.Execute(config, logger, route);
                }
                // Walmart Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartUploadInventory))
                {
                    WalmartUploadInventoryRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartGetOrders))
                {
                    WalmartGetOrdersRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartASNShipmentNotification))
                {
                    WalmartASNShipmentNotificationRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartCancellationLines))
                {
                    WalmartCancellationLinesRoute.Execute(config, logger, route);
                }
                // Knot Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotInventoryUpload))
                {
                    KnotUpdateInventory.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotBulkItemPrices))
                {
                    KnotBulkItemPricesRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotGetOrders))
                {
                    KnotGetOrderRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotASNShipmentNotification))
                {
                    KnotASNShipmentNotificationRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotCancellationLines))
                {
                    KnotCancellationRoute.Execute(config, logger, route);
                }
                // Macys Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysInventoryUpload))
                {
                    MacysUpdateInventory.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysBulkItemPrices))
                {
                    MacysBulkItemPricesRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysGetOrders))
                {
                    MacysGetOrderRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysASNShipmentNotification))
                {
                    MacysASNShipmentNotificationRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysCancellationLines))
                {
                    MacysCancellationRoute.Execute(config, logger, route);
                }
                // Lowes Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesInventoryUpload))
                {
                    LowesUpdateInventory.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesBulkItemPrices))
                {
                    LowesBulkItemPricesRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesGetOrders))
                {
                    LowesGetOrderRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesASNShipmentNotification))
                {
                    LowesASNShipmentNotificationRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesCancellationLines))
                {
                    LowesCancellationRoute.Execute(config, logger, route);
                }
                // Micheal Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealInventoryUpload))
                {
                    MichealUpdateInventory.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealBulkItemPrices))
                {
                    MichealUpdatePrice.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealGetOrders))
                {
                    MichealGetOrderRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealASNShipmentNotification))
                {
                    MichealASNShipmentNotificationRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealCancellationLines))
                {
                    MichealCancellationRoute.Execute(config, logger, route);
                }
                // Carrier Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender))
                {
                    CarrierLoadTenderRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender990))
                {
                    CarrierLoadTenderAcknowledgmentRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender214))
                {
                    CarrierLoadTenderResponseRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender214X6))
                {
                    CarrierLoadTender214X6Route.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CLTAddressUpdate))
                {
                    CLTAddressUpdateRoute.Execute(config, logger, route);
                }
                // Repaint Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.RepaintGetOrders))
                {
                    RepaintGetOrderRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.RepaintCreateOrder))
                {
                    RepaintCreateOrderRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.RepaintGenerate855))
                {
                    RepaintGenerate855Route.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download856FromShipStation))
                {
                    Download856FromShipStationRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GenerateEDI856ForRepaintRoute))
                {
                    GenerateEDI856ForRepaintRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GenerateEDI810ForRepaintRoute))
                {
                    GenerateEDI810ForRepaintRoute.Execute(config, logger, route);
                }
                // Other Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ItemTypesReportRequest))
                {
                    ItemTypesReportRequest.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ItemTypesProcessing))
                {
                    ItemTypesProcessing.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ProductTypeAttributes))
                {
                    ProductTypeAttributes.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ProductCatalog))
                {
                    ProductCatalog.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ProductCatalogStatus))
                {
                    ProductCatalogStatus.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.BulkUploadPrices))
                {
                    BulkUploadPricesRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.BulkUploadOldPrices))
                {
                    BulkUploadOldPricesRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ASNShipmentNotification))
                {
                    ASNShipmentNotificationRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSCancelOrder))
                {
                    SCSCancelOrderRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CancellationLines))
                {
                    CancellationLinesRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.DeleteCustomerProductCatalog))
                {
                    DeleteCustomerProductCatalogRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.DownloadItemsData))
                {
                    DownloadTargetItemsRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSBulkItemPrices))
                {
                    SCSBulkItemPricesRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GetPurchaseOrder850))
                {
                    GetPurchaseOrder850Route.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download855FromFTP))
                {
                    Download855FromFTPRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download846FromFTP))
                {
                    Download846FromFTPRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download856FromFTP))
                {
                    Download856FromFTPRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download810FromFTP))
                {
                    Download810FromFTPRoute.Execute(config, logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.VeeqoUpdateProductsQTY))
                {
                    Task task = VeeqoUpdatedProductsQTYRoute.Execute(config, logger, route);
                    task.Wait();
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.VeeqoGetSO))
                {
                    Task task = VeeqoGetSORoute.Execute(config, logger, route);
                    task.Wait();
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.VeeqoCreateNewProducts))
                {
                    Task task = VeeqoCreateNewProductsRoute.Execute(config, logger, route);
                    task.Wait();
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ShipStationUpdateSKUStocklevels))
                {
                    Task task = ShipStationUpdateSKUStocklevelsRoute.Execute(config, logger, route);
                    task.Wait();
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.TargetPlusInventoryFeedWHSWise))
                {
                    TargetPlusInventoryFeedWHSWiseRoute.Execute(config, logger, route);
                }
                else
                {
                    string errorMsg = $"Unknown route type: {route.TypeId}";
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARNING: {errorMsg}");
                    route.SaveLog(Declarations.LogTypeEnum.Error, errorMsg, string.Empty, userNo);
                    throw new Exception(errorMsg);
                }

                route.SaveLog(Declarations.LogTypeEnum.Info, $"[RouteWorker] Completed executing route [{route.Id}] - {route.Name}", string.Empty, userNo);
            }
            catch (Exception ex)
            {
                route.SaveLog(Declarations.LogTypeEnum.Exception, $"[RouteWorker] Error executing route [{route.Id}]", ex.ToString(), userNo);
                throw;
            }
        }
    }
}
