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
        private readonly ILogger _logger;
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
            try
            {
                // Build configuration - use executable directory, not current directory
                var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(exeDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                CommonUtils.ConnectionString = configuration.GetConnectionString("DefaultConnection");
               
                Routes route = new Routes();
                route.UseConnection(CommonUtils.ConnectionString);
                route.Id = routeId;

                if (!route.GetObject().IsSuccess)
                {
                    Environment.ExitCode = 1;
                    return;
                }

                
                if (route.Status.ToUpper() == "IN-ACTIVE")
                {
                    Environment.ExitCode = 0;
                    return;
                }

              
                ExecuteRouteByType(configuration, route);

                Environment.ExitCode = 0;
            }
            catch (Exception ex)
            {
                Environment.ExitCode = 1;
            }
        }

        static void ExecuteRouteByType(IConfiguration config, Routes route)
        {
            int userNo = 1; // System user

            route.SaveLog(Declarations.LogTypeEnum.Info, $"[RouteWorker] Started executing route [{route.Id}] - {route.Name}", string.Empty, userNo);

            try
            {
                if (route.TypeId == Convert.ToInt32(RouteTypesEnum.InventoryFeed))
                {
                    InventoryFeedRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GetOrders))
                {
                    GetOrdersRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CreateOrder))
                {
                    CreateOrderRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GetOrderStatus))
                {
                    GetOrderStatusRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ASN))
                {
                    ASNRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CreateInvoice))
                {
                    CreateInvoiceRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSFullInventoryFeed) || route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSDifferentialInventoryFeed))
                {
                    SCSFullInventoryFeedRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSPlaceOrder))
                {
                    SCSPlaceOrderRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSOrderStatus))
                {
                    SCSOrderStatusRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSASN))
                {
                    SCSASNRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSInvoice))
                {
                    SCSInvoiceRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSGetOrders))
                {
                    SCSGetOrders.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSItemPrices))
                {
                    SCSItemPrices.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSUpdateInventory))
                {
                    SCSUpdateInventory.Execute(config, route);
                }
                // Amazon Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonInventoryUpload))
                {
                    AmazonUploadInventoryRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonGetOrders))
                {
                    AmazonGetOrdersRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonInventoryStatus))
                {
                    AmazonInventoryStatusRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonASNShipmentNotification))
                {
                    AmazonASNShipmentNotificationRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonWHSWInventoryUpload))
                {
                    AmazonUploadWarehouseWiseInventoryRoute.Execute(config, route);
                }
                // Walmart Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartUploadInventory))
                {
                    WalmartUploadInventoryRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartGetOrders))
                {
                    WalmartGetOrdersRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartASNShipmentNotification))
                {
                    WalmartASNShipmentNotificationRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartCancellationLines))
                {
                    WalmartCancellationLinesRoute.Execute(config, route);
                }
                // Knot Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotInventoryUpload))
                {
                    KnotUpdateInventory.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotBulkItemPrices))
                {
                    KnotBulkItemPricesRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotGetOrders))
                {
                    KnotGetOrderRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotASNShipmentNotification))
                {
                    KnotASNShipmentNotificationRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotCancellationLines))
                {
                    KnotCancellationRoute.Execute(config, route);
                }
                // Macys Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysInventoryUpload))
                {
                    MacysUpdateInventory.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysBulkItemPrices))
                {
                    MacysBulkItemPricesRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysGetOrders))
                {
                    MacysGetOrderRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysASNShipmentNotification))
                {
                    MacysASNShipmentNotificationRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysCancellationLines))
                {
                    MacysCancellationRoute.Execute(config, route);
                }
                // Lowes Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesInventoryUpload))
                {
                    LowesUpdateInventory.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesBulkItemPrices))
                {
                    LowesBulkItemPricesRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesGetOrders))
                {
                    LowesGetOrderRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesASNShipmentNotification))
                {
                    LowesASNShipmentNotificationRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesCancellationLines))
                {
                    LowesCancellationRoute.Execute(config, route);
                }
                // Micheal Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealInventoryUpload))
                {
                    MichealUpdateInventory.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealBulkItemPrices))
                {
                    MichealUpdatePrice.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealGetOrders))
                {
                    MichealGetOrderRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealASNShipmentNotification))
                {
                    MichealASNShipmentNotificationRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealCancellationLines))
                {
                    MichealCancellationRoute.Execute(config, route);
                }
                // Carrier Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender))
                {
                    CarrierLoadTenderRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender990))
                {
                    CarrierLoadTenderAcknowledgmentRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender214))
                {
                    CarrierLoadTenderResponseRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender214X6))
                {
                    CarrierLoadTender214X6Route.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CLTAddressUpdate))
                {
                    CLTAddressUpdateRoute.Execute(config, route);
                }
                // Repaint Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.RepaintGetOrders))
                {
                    RepaintGetOrderRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.RepaintCreateOrder))
                {
                    RepaintCreateOrderRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.RepaintGenerate855))
                {
                    RepaintGenerate855Route.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download856FromShipStation))
                {
                    Download856FromShipStationRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GenerateEDI856ForRepaintRoute))
                {
                    GenerateEDI856ForRepaintRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GenerateEDI810ForRepaintRoute))
                {
                    GenerateEDI810ForRepaintRoute.Execute(config, route);
                }
                // Other Routes
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ItemTypesReportRequest))
                {
                    ItemTypesReportRequest.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ItemTypesProcessing))
                {
                    ItemTypesProcessing.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ProductTypeAttributes))
                {
                    ProductTypeAttributes.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ProductCatalog))
                {
                    ProductCatalog.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ProductCatalogStatus))
                {
                    ProductCatalogStatus.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.BulkUploadPrices))
                {
                    BulkUploadPricesRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.BulkUploadOldPrices))
                {
                    BulkUploadOldPricesRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ASNShipmentNotification))
                {
                    ASNShipmentNotificationRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSCancelOrder))
                {
                    SCSCancelOrderRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CancellationLines))
                {
                    CancellationLinesRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.DeleteCustomerProductCatalog))
                {
                    DeleteCustomerProductCatalogRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.DownloadItemsData))
                {
                    DownloadTargetItemsRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSBulkItemPrices))
                {
                    SCSBulkItemPricesRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GetPurchaseOrder850))
                {
                    GetPurchaseOrder850Route.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download855FromFTP))
                {
                    Download855FromFTPRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download846FromFTP))
                {
                    Download846FromFTPRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download856FromFTP))
                {
                    Download856FromFTPRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download810FromFTP))
                {
                    Download810FromFTPRoute.Execute(config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.VeeqoUpdateProductsQTY))
                {
                    Task task = VeeqoUpdatedProductsQTYRoute.Execute(config, route);
                    task.Wait();
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.VeeqoGetSO))
                {
                    Task task = VeeqoGetSORoute.Execute(config, route);
                    task.Wait();
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.VeeqoCreateNewProducts))
                {
                    Task task = VeeqoCreateNewProductsRoute.Execute(config, route);
                    task.Wait();
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ShipStationUpdateSKUStocklevels))
                {
                    Task task = ShipStationUpdateSKUStocklevelsRoute.Execute(config, route);
                    task.Wait();
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.TargetPlusInventoryFeedWHSWise))
                {
                    TargetPlusInventoryFeedWHSWiseRoute.Execute(config, route);
                }
                else
                {
                    string errorMsg = $"Unknown route type: {route.TypeId}";
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
