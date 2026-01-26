using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Hangfire;
using System.Diagnostics;

namespace eSyncMate.Processor.Managers
{
    public class RouteEngine
    {
        private readonly IConfiguration _config;

        private static Dictionary<int, Routes> currentRoutes = new Dictionary<int, Routes>();

        // Configuration flag to switch between in-process and external process execution
        private bool UseExternalProcess => _config?.GetValue<bool>("RouteEngine:UseExternalProcess") ?? false;
        private string ExternalProcessPath
        {
            get
            {
                var configPath = _config?.GetValue<string>("RouteEngine:ExternalProcessPath") ?? "RouteWorker\\eSyncMate.RouteWorker.exe";

                // If it's a relative path, resolve it relative to the application directory
                if (!Path.IsPathRooted(configPath))
                {
                    var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    return Path.Combine(appDirectory, configPath);
                }

                return configPath;
            }
        }

        public RouteEngine(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Execute route using external console app process
        /// This provides process isolation and better resource management
        /// </summary>
        public void ExecuteExternal(int routeId)
        {
            Routes route = new Routes();

            try
            {
                route.UseConnection(CommonUtils.ConnectionString);
                route.Id = routeId;

                if (!route.GetObject().IsSuccess)
                {
                    return;
                }

                if (route.Status.ToUpper() == "IN-ACTIVE")
                {
                    this.RemoveRouteJob(route);
                    return;
                }

                // Check if route is already running
                if (currentRoutes.ContainsKey(routeId))
                {
                    route.SaveLog(Declarations.LogTypeEnum.Info, "This route is already in execution.", "", 1);
                    return;
                }

                currentRoutes[routeId] = route;

                // Debug: Log paths
                var exePath = ExternalProcessPath;
                var workingDir = Path.GetDirectoryName(exePath) ?? Directory.GetCurrentDirectory();

                route.SaveLog(Declarations.LogTypeEnum.Info, $"Starting external process for route [{routeId}]. Path: {exePath}", "", 1);

                if (!File.Exists(exePath))
                {
                    var errorMsg = $"RouteWorker exe not found at: {exePath}";
                    Console.WriteLine($"[ERROR] {errorMsg}");
                    route.SaveLog(Declarations.LogTypeEnum.Error, errorMsg, "", 1);
                    return;
                }

                // Spawn external process
                var processInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"--routeId {routeId}",
                    UseShellExecute = false,
                    CreateNoWindow = false,  // Show window for debugging
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDir
                };

                Console.WriteLine($"[DEBUG] ExecuteExternal - Starting process...");

                using var process = Process.Start(processInfo);

                if (process != null)
                {
                    Console.WriteLine($"[DEBUG] ExecuteExternal - Process started, PID: {process.Id}");

                    // Read output asynchronously
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                        Console.WriteLine($"[DEBUG] ExecuteExternal - Error: {error}");

                    if (process.ExitCode != 0)
                    {
                        route.SaveLog(Declarations.LogTypeEnum.Error, $"External process failed for route [{routeId}]", $"Output: {output}\nError: {error}", 1);
                    }
                    else
                    {
                        route.SaveLog(Declarations.LogTypeEnum.Info, $"External process completed for route [{routeId}]", output, 1);
                    }
                }
                else
                {
                    route.SaveLog(Declarations.LogTypeEnum.Error, $"Failed to start external process for route [{routeId}]", "", 1);
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(Declarations.LogTypeEnum.Exception, $"Error spawning external process for route [{routeId}]", ex.ToString(), 1);
            }
            finally
            {
                if (currentRoutes.ContainsKey(routeId))
                    currentRoutes.Remove(routeId);
            }
        }

        public void Execute(int routeId)
        {
            if (UseExternalProcess)
            {
                ExecuteExternal(routeId);
                return;
            }

            Routes route = new Routes();

            try
            {
                route.UseConnection(CommonUtils.ConnectionString);

                route.Id = routeId;
                if (!route.GetObject().IsSuccess)
                {
                    return;
                }
                if (route.Status.ToUpper() == "IN-ACTIVE")
                {
                    this.RemoveRouteJob(route);
                    return;
                }

                if (currentRoutes.ContainsKey(routeId))
                {
                    route.SaveLog(Declarations.LogTypeEnum.Info, "This route is already in execution.", "", 1);
                    return;
                }

                currentRoutes[routeId] = route;

                if (route.TypeId == Convert.ToInt32(RouteTypesEnum.InventoryFeed))
                {
                    InventoryFeedRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GetOrders))
                {
                    GetOrdersRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CreateOrder))
                {
                    CreateOrderRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GetOrderStatus))
                {
                    GetOrderStatusRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ASN))
                {
                    ASNRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CreateInvoice))
                {
                    CreateInvoiceRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSFullInventoryFeed) || route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSDifferentialInventoryFeed))
                {
                    SCSFullInventoryFeedRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSPlaceOrder))
                {
                    SCSPlaceOrderRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSOrderStatus))
                {
                    SCSOrderStatusRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSASN))
                {
                    SCSASNRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSInvoice))
                {
                    SCSInvoiceRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ItemTypesReportRequest))
                {
                    ItemTypesReportRequest.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ItemTypesProcessing))
                {
                    ItemTypesProcessing.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ProductTypeAttributes))
                {
                    ProductTypeAttributes.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ProductCatalog))
                {
                    ProductCatalog.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ProductCatalogStatus))
                {
                    ProductCatalogStatus.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSItemPrices))
                {
                    SCSItemPrices.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSUpdateInventory))
                {
                    SCSUpdateInventory.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSGetOrders))
                {
                    SCSGetOrders.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender))
                {
                    CarrierLoadTenderRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender990))
                {
                    CarrierLoadTenderAcknowledgmentRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender214))
                {
                    CarrierLoadTenderResponseRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender214X6))
                {
                    CarrierLoadTender214X6Route.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CLTAddressUpdate))
                {
                    CLTAddressUpdateRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.BulkUploadPrices))
                {
                    BulkUploadPricesRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.BulkUploadOldPrices))
                {
                    BulkUploadOldPricesRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ASNShipmentNotification))
                {
                    ASNShipmentNotificationRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSCancelOrder))
                {
                    SCSCancelOrderRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CancellationLines))
                {
                    CancellationLinesRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.DeleteCustomerProductCatalog))
                {
                    DeleteCustomerProductCatalogRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartUploadInventory))
                {
                    WalmartUploadInventoryRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartGetOrders))
                {
                    WalmartGetOrdersRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartASNShipmentNotification))
                {
                    WalmartASNShipmentNotificationRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartCancellationLines))
                {
                    WalmartCancellationLinesRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.DownloadItemsData))
                {
                    DownloadTargetItemsRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSBulkItemPrices))
                {
                    SCSBulkItemPricesRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GetPurchaseOrder850))
                {
                    GetPurchaseOrder850Route.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download855FromFTP))
                {
                    Download855FromFTPRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download846FromFTP))
                {
                    Download846FromFTPRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download856FromFTP))
                {
                    Download856FromFTPRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download810FromFTP))
                {
                    Download810FromFTPRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.VeeqoUpdateProductsQTY))
                {
                    Task task = VeeqoUpdatedProductsQTYRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.VeeqoGetSO))
                {
                    Task task = VeeqoGetSORoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.VeeqoCreateNewProducts))
                {
                    Task task = VeeqoCreateNewProductsRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ShipStationUpdateSKUStocklevels))
                {
                    Task task = ShipStationUpdateSKUStocklevelsRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysInventoryUpload))
                {
                    MacysUpdateInventory.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysBulkItemPrices))
                {
                    MacysBulkItemPricesRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysGetOrders))
                {
                    MacysGetOrderRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysASNShipmentNotification))
                {
                    MacysASNShipmentNotificationRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysCancellationLines))
                {
                    MacysCancellationRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.TargetPlusInventoryFeedWHSWise))
                {
                    TargetPlusInventoryFeedWHSWiseRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonInventoryUpload))
                {
                    AmazonUploadInventoryRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonGetOrders))
                {
                    AmazonGetOrdersRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonInventoryStatus))
                {
                    AmazonInventoryStatusRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonASNShipmentNotification))
                {
                    AmazonASNShipmentNotificationRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesInventoryUpload))
                {
                    LowesUpdateInventory.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesBulkItemPrices))
                {
                    LowesBulkItemPricesRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesGetOrders))
                {
                    LowesGetOrderRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesASNShipmentNotification))
                {
                    LowesASNShipmentNotificationRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesCancellationLines))
                {
                    LowesCancellationRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.RepaintGetOrders))
                {
                    RepaintGetOrderRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.RepaintCreateOrder))
                {
                    RepaintCreateOrderRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.RepaintGenerate855))
                {
                    RepaintGenerate855Route.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download856FromShipStation))
                {
                    Download856FromShipStationRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GenerateEDI856ForRepaintRoute))
                {
                    GenerateEDI856ForRepaintRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GenerateEDI810ForRepaintRoute))
                {
                    GenerateEDI810ForRepaintRoute.Execute(_config, route);
                }
                ////////Knot Api///
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotInventoryUpload))
                {
                    KnotUpdateInventory.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotBulkItemPrices))
                {
                    KnotBulkItemPricesRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotGetOrders))
                {
                    KnotGetOrderRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotASNShipmentNotification))
                {
                    KnotASNShipmentNotificationRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotCancellationLines))
                {
                    KnotCancellationRoute.Execute(_config, route);
                }

                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealInventoryUpload))
                {
                    MichealUpdateInventory.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealBulkItemPrices))
                {
                    MichealUpdatePrice.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealGetOrders))
                {
                    MichealGetOrderRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealASNShipmentNotification))
                {
                    MichealASNShipmentNotificationRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealCancellationLines))
                {
                    MichealCancellationRoute.Execute(_config, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonWHSWInventoryUpload))
                {
                    AmazonUploadWarehouseWiseInventoryRoute.Execute(_config, route);
                }
            }
            catch (Exception ex)
            {
                route.SaveLog(Declarations.LogTypeEnum.Exception, ex.Message, ex.ToString(), 1);
            }
            finally
            {
                if (currentRoutes.ContainsKey(routeId))
                    currentRoutes.Remove(routeId);
            }
        }

        public void Schedule(int routeId)
        {
            Routes route = new Routes();

            try
            {
                route.UseConnection(CommonUtils.ConnectionString);

                route.Id = routeId;
                if (!route.GetObject().IsSuccess)
                {
                    Console.WriteLine($"[ERROR] Invalid Route! [{routeId}]");
                    return;
                }

                this.SetupRouteJob(route);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Schedule failed for route [{routeId}]: {ex.Message}");
            }
        }

        private void SetupRouteJob(Routes route)
        {

            RouteEngine l_Engine = this;

            if (route.FrequencyType == "Minutely")
            {
                RecurringJob.AddOrUpdate($"Route [{route.Id}]",() => l_Engine.Execute(route.Id),$"*/{route.RepeatCount} * * * *",TimeZoneInfo.Local);
            }
            else if (route.FrequencyType == "Hourly")
            {
                RecurringJob.AddOrUpdate($"Route [{route.Id}]",() => l_Engine.Execute(route.Id),$"0 */{route.RepeatCount} * * *",TimeZoneInfo.Local);
            }
            else if (route.FrequencyType == "Daily")
            {
                if (string.IsNullOrEmpty(route.ExecutionTime))
                {
                    RecurringJob.AddOrUpdate($"Route [{route.Id}]", () => l_Engine.Execute(route.Id), Cron.Daily, TimeZoneInfo.Local);
                }
                else
                {
                    foreach (string time in route.ExecutionTime.Split(","))
                    {
                        string[] split = time.Split(":");
                        RecurringJob.AddOrUpdate($"Route [{route.Id}] at [{time}]", () => l_Engine.Execute(route.Id), Cron.Daily(Convert.ToInt32(split[0]), Convert.ToInt32(split[1])), TimeZoneInfo.Local);
                    }
                }
            }
            else if (route.FrequencyType == "Weekly")
            {
                foreach (string weekday in route.WeekDays.Split(","))
                {
                    if (string.IsNullOrEmpty(route.ExecutionTime))
                    {
                        RecurringJob.AddOrUpdate($"Route [{route.Id}] on [{weekday}]", () => l_Engine.Execute(route.Id), Cron.Weekly(Enum.Parse<DayOfWeek>(weekday)), TimeZoneInfo.Local);
                    }
                    else
                    {
                        foreach (string time in route.ExecutionTime.Split(","))
                        {
                            string[] split = time.Split(":");
                            RecurringJob.AddOrUpdate($"Route [{route.Id}] on [{weekday}] at [{time}]", () => l_Engine.Execute(route.Id), Cron.Weekly(Enum.Parse<DayOfWeek>(weekday), Convert.ToInt32(split[0]), Convert.ToInt32(split[1])), TimeZoneInfo.Local);
                        }
                    }
                }
            }
            else if (route.FrequencyType == "Monthly")
            {
                foreach (string day in route.OnDay.Split(","))
                {
                    if (string.IsNullOrEmpty(route.ExecutionTime))
                    {
                        RecurringJob.AddOrUpdate($"Route [{route.Id}] on [{day}]", () => l_Engine.Execute(route.Id), Cron.Monthly(Convert.ToInt32(day)), TimeZoneInfo.Local);
                    }
                    else
                    {
                        foreach (string time in route.ExecutionTime.Split(","))
                        {
                            string[] split = time.Split(":");
                            RecurringJob.AddOrUpdate($"Route [{route.Id}] on [{day}] at [{time}]", () => l_Engine.Execute(route.Id), Cron.Monthly(Convert.ToInt32(day), Convert.ToInt32(split[0]), Convert.ToInt32(split[1])), TimeZoneInfo.Local);
                        }
                    }
                }
            }
        }

        public void RemoveRouteJob(Routes route)
        {
            if (route.FrequencyType == "Minutely")
            {
                RecurringJob.RemoveIfExists($"Route [{route.Id}]");
            }
            else if (route.FrequencyType == "Hourly")
            {
                RecurringJob.RemoveIfExists($"Route [{route.Id}]");
            }
            else if (route.FrequencyType == "Daily")
            {
                if (string.IsNullOrEmpty(route.ExecutionTime))
                {
                    RecurringJob.RemoveIfExists($"Route [{route.Id}]");
                }
                else
                {
                    foreach (string time in route.ExecutionTime.Split(","))
                    {
                        RecurringJob.RemoveIfExists($"Route [{route.Id}] at [{time}]");
                    }
                }
            }
            else if (route.FrequencyType == "Weekly")
            {
                foreach (string weekday in route.WeekDays.Split(","))
                {
                    if (string.IsNullOrEmpty(route.ExecutionTime))
                    {
                        RecurringJob.RemoveIfExists($"Route [{route.Id}] on [{weekday}]");
                    }
                    else
                    {
                        foreach (string time in route.ExecutionTime.Split(","))
                        {
                            RecurringJob.RemoveIfExists($"Route [{route.Id}] on [{weekday}] at [{time}]");
                        }
                    }
                }
            }
            else if (route.FrequencyType == "Monthly")
            {
                foreach (string day in route.OnDay.Split(","))
                {
                    if (string.IsNullOrEmpty(route.ExecutionTime))
                    {
                        RecurringJob.RemoveIfExists($"Route [{route.Id}] on [{day}]");
                    }
                    else
                    {
                        foreach (string time in route.ExecutionTime.Split(","))
                        {
                            RecurringJob.RemoveIfExists($"Route [{route.Id}] on [{day}] at [{time}]");
                        }
                    }
                }
            }
        }

        private void TriggerJob(Routes route)
        {

            RouteEngine l_Engine = this;

            if (route.FrequencyType == "Minutely")
            {
                RecurringJob.Trigger(route.JobID);
            }
            else if (route.FrequencyType == "Hourly")
            {
                RecurringJob.Trigger(route.JobID);
            }
        }
    }
}
