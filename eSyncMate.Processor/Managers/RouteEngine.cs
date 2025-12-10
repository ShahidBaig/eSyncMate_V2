using eSyncMate.DB;
using eSyncMate.DB.Entities;
using eSyncMate.Processor.Models;
using Hangfire;

namespace eSyncMate.Processor.Managers
{
    public class RouteEngine
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        private static Dictionary<int, Routes> currentRoutes = new Dictionary<int, Routes>();

        public RouteEngine(IConfiguration config)
        {
            _config = config;
            //_logger = logger;
        }

        public void Execute(int routeId)
        {
            Routes route = new Routes();

            try
            {
                route.UseConnection(CommonUtils.ConnectionString);

                route.Id = routeId;
                if (!route.GetObject().IsSuccess)
                {
                    this._logger?.LogError($"Invalid Route! [{routeId}]");
                    return;
                }

                //if (route.Status.ToUpper() == "IN-ACTIVE")
                //{
                //    this.RemoveRouteJob(route);
                //    return;
                //}

                if (currentRoutes.ContainsKey(routeId))
                {
                    route.SaveLog(Declarations.LogTypeEnum.Info, "This route is already in execution.", "", 1);
                    return;
                }

                currentRoutes[routeId] = route;

                if (route.TypeId == Convert.ToInt32(RouteTypesEnum.InventoryFeed))
                {
                    InventoryFeedRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GetOrders))
                {
                    GetOrdersRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CreateOrder))
                {
                    CreateOrderRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GetOrderStatus))
                {
                    GetOrderStatusRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ASN))
                {
                    ASNRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CreateInvoice))
                {
                    CreateInvoiceRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSFullInventoryFeed) || route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSDifferentialInventoryFeed))
                {
                    SCSFullInventoryFeedRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSPlaceOrder))
                {
                    SCSPlaceOrderRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSOrderStatus))
                {
                    SCSOrderStatusRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSASN))
                {
                    SCSASNRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSInvoice))
                {
                    SCSInvoiceRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ItemTypesReportRequest))
                {
                    ItemTypesReportRequest.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ItemTypesProcessing))
                {
                    ItemTypesProcessing.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ProductTypeAttributes))
                {
                    ProductTypeAttributes.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ProductCatalog))
                {
                    ProductCatalog.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ProductCatalogStatus))
                {
                    ProductCatalogStatus.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSItemPrices))
                {
                    SCSItemPrices.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSUpdateInventory))
                {
                    SCSUpdateInventory.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSGetOrders))
                {
                    SCSGetOrders.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender))
                {
                    CarrierLoadTenderRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender990))
                {
                    CarrierLoadTenderAcknowledgmentRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender214))
                {
                    CarrierLoadTenderResponseRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CarrierLoadTender214X6))
                {
                    CarrierLoadTender214X6Route.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CLTAddressUpdate))
                {
                    CLTAddressUpdateRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.BulkUploadPrices))
                {
                    BulkUploadPricesRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.BulkUploadOldPrices))
                {
                    BulkUploadOldPricesRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ASNShipmentNotification))
                {
                    ASNShipmentNotificationRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSCancelOrder))
                {
                    SCSCancelOrderRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.CancellationLines))
                {
                    CancellationLinesRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.DeleteCustomerProductCatalog))
                {
                    DeleteCustomerProductCatalogRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartUploadInventory))
                {
                    WalmartUploadInventoryRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartGetOrders))
                {
                    WalmartGetOrdersRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartASNShipmentNotification))
                {
                    WalmartASNShipmentNotificationRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.WalmartCancellationLines))
                {
                    WalmartCancellationLinesRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.DownloadItemsData))
                {
                    DownloadTargetItemsRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.SCSBulkItemPrices))
                {
                    SCSBulkItemPricesRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GetPurchaseOrder850))
                {
                    GetPurchaseOrder850Route.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download855FromFTP))
                {
                    Download855FromFTPRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download846FromFTP))
                {
                    Download846FromFTPRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download856FromFTP))
                {
                    Download856FromFTPRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download810FromFTP))
                {
                    Download810FromFTPRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.VeeqoUpdateProductsQTY))
                {
                    Task task = VeeqoUpdatedProductsQTYRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.VeeqoGetSO))
                {
                    Task task = VeeqoGetSORoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.VeeqoCreateNewProducts))
                {
                    Task task = VeeqoCreateNewProductsRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.ShipStationUpdateSKUStocklevels))
                {
                    Task task = ShipStationUpdateSKUStocklevelsRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysInventoryUpload))
                {
                    MacysUpdateInventory.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysBulkItemPrices))
                {
                    MacysBulkItemPricesRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysGetOrders))
                {
                    MacysGetOrderRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysASNShipmentNotification))
                {
                    MacysASNShipmentNotificationRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MacysCancellationLines))
                {
                    MacysCancellationRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.TargetPlusInventoryFeedWHSWise))
                {
                    TargetPlusInventoryFeedWHSWiseRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonInventoryUpload))
                {
                    AmazonUploadInventoryRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonGetOrders))
                {
                    AmazonGetOrdersRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonInventoryStatus))
                {
                    AmazonInventoryStatusRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.AmazonASNShipmentNotification))
                {
                    AmazonASNShipmentNotificationRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesInventoryUpload))
                {
                    LowesUpdateInventory.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesBulkItemPrices))
                {
                    LowesBulkItemPricesRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesGetOrders))
                {
                    LowesGetOrderRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesASNShipmentNotification))
                {
                    LowesASNShipmentNotificationRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.LowesCancellationLines))
                {
                    LowesCancellationRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.RepaintGetOrders))
                {
                    RepaintGetOrderRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.RepaintCreateOrder))
                {
                    RepaintCreateOrderRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.RepaintGenerate855))
                {
                    RepaintGenerate855Route.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.Download856FromShipStation))
                {
                    Download856FromShipStationRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GenerateEDI856ForRepaintRoute))
                {
                    GenerateEDI856ForRepaintRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.GenerateEDI810ForRepaintRoute))
                {
                    GenerateEDI810ForRepaintRoute.Execute(_config, _logger, route);
                }
                ////////Knot Api///
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotInventoryUpload))
                {
                    KnotUpdateInventory.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotBulkItemPrices))
                {
                    KnotBulkItemPricesRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotGetOrders))
                {
                    KnotGetOrderRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotASNShipmentNotification))
                {
                    KnotASNShipmentNotificationRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.KnotCancellationLines))
                {
                    KnotCancellationRoute.Execute(_config, _logger, route);
                }

                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealInventoryUpload))
                {
                    MichealUpdateInventory.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealBulkItemPrices))
                {
                    MichealUpdatePrice.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealGetOrders))
                {
                    MichealGetOrderRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealASNShipmentNotification))
                {
                    MichealASNShipmentNotificationRoute.Execute(_config, _logger, route);
                }
                else if (route.TypeId == Convert.ToInt32(RouteTypesEnum.MichealCancellationLines))
                {
                    MichealCancellationRoute.Execute(_config, _logger, route);
                }
            }
            catch (Exception ex)
            {
                this._logger?.LogCritical(ex, ex.Message);
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
                    this._logger?.LogError($"Invalid Route! [{routeId}]");
                    return;
                }

                this.SetupRouteJob(route);
            }
            catch (Exception ex)
            {
                this._logger?.LogCritical(ex, ex.Message);
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
