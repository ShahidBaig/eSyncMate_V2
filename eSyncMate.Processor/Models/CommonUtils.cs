using System.Collections.Concurrent;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using static Hangfire.Storage.JobStorageFeatures;
using System.Xml.Linq;

namespace eSyncMate.Processor.Models
{
    /// <summary>
    /// Shared HttpClient factory to prevent socket exhaustion across 100+ concurrent routes.
    /// HttpClient instances are expensive to create and should be reused.
    /// </summary>
    public static class SharedHttpClientFactory
    {
        // Thread-safe dictionary to store HttpClients per service/host
        private static readonly ConcurrentDictionary<string, HttpClient> _clients = new();

        // Default handler configuration for all clients
        private static SocketsHttpHandler CreateHandler() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 50,
            EnableMultipleHttp2Connections = true,
            ConnectTimeout = TimeSpan.FromSeconds(30),
            ResponseDrainTimeout = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// Gets or creates a shared HttpClient for general use
        /// </summary>
        public static HttpClient Default => GetOrCreateClient("default");

        /// <summary>
        /// Gets or creates a shared HttpClient for Amazon API calls
        /// </summary>
        public static HttpClient Amazon => GetOrCreateClient("amazon");

        /// <summary>
        /// Gets or creates a shared HttpClient for Walmart API calls
        /// </summary>
        public static HttpClient Walmart => GetOrCreateClient("walmart");

        /// <summary>
        /// Gets or creates a shared HttpClient for Veeqo API calls
        /// </summary>
        public static HttpClient Veeqo => GetOrCreateClient("veeqo");

        /// <summary>
        /// Gets or creates a shared HttpClient for ShipStation API calls
        /// </summary>
        public static HttpClient ShipStation => GetOrCreateClient("shipstation");

        /// <summary>
        /// Gets or creates a shared HttpClient for Lowes/Mirakl API calls
        /// </summary>
        public static HttpClient Lowes => GetOrCreateClient("lowes");

        /// <summary>
        /// Gets or creates a shared HttpClient for a specific service key
        /// </summary>
        public static HttpClient GetOrCreateClient(string serviceKey)
        {
            return _clients.GetOrAdd(serviceKey, key =>
            {
                var client = new HttpClient(CreateHandler(), disposeHandler: false)
                {
                    Timeout = TimeSpan.FromMinutes(5)
                };
                return client;
            });
        }

        /// <summary>
        /// Gets or creates a shared HttpClient with a specific base address
        /// </summary>
        public static HttpClient GetOrCreateClient(string serviceKey, string baseAddress)
        {
            return _clients.GetOrAdd($"{serviceKey}_{baseAddress}", key =>
            {
                var client = new HttpClient(CreateHandler(), disposeHandler: false)
                {
                    Timeout = TimeSpan.FromMinutes(5),
                    BaseAddress = new Uri(baseAddress)
                };
                return client;
            });
        }
    }

    internal enum ResponseCodes
    {
        Success = 200,
        Error = 400,
        Exception = 500,
        CustomerAlreadyExists = 401,
        FlowAlreadyExists = 402,
        NotFound = 404
    }
    public enum RouteTypesEnum
    {
        InventoryFeed = 1,
        GetOrders = 2,
        CreateOrder = 3,
        GetOrderStatus = 4,
        ASN = 5,
        CreateInvoice = 6,
        SCSFullInventoryFeed = 7,
        SCSDifferentialInventoryFeed = 8,
        SCSPlaceOrder = 9,
        SCSOrderStatus = 10,
        SCSASN = 11,
        SCSInvoice = 12,
        ItemTypesReportRequest = 13,
        ItemTypesProcessing = 14,
        ProductTypeAttributes = 15,
        ProductCatalog = 16,
        ProductCatalogStatus = 17,
        SCSItemPrices = 18,
        SCSUpdateInventory = 19,
        SCSGetOrders = 20,
        CarrierLoadTender = 100,
        CarrierLoadTender990 = 101,
        CarrierLoadTender214 = 102,
        CLTAddressUpdate = 103,
        CLTUpdateStatus = 104,
        CarrierLoadTender214X6 = 105,
        BulkUploadPrices = 21,
        BulkUploadOldPrices = 22,
        ASNShipmentNotification = 23,
        SCSCancelOrder = 24,
        CancellationLines = 25,
        DeleteCustomerProductCatalog = 26,
        WalmartUploadInventory = 27,
        WalmartGetOrders = 28,
        WalmartASNShipmentNotification = 29,
        WalmartCancellationLines = 30,
        DownloadItemsData = 31,
        SCSBulkItemPrices = 32,
        GetPurchaseOrder850 = 300,
        Download855FromFTP = 301,
        Download846FromFTP = 302,
        Download856FromFTP = 303,
        VeeqoUpdateProductsQTY = 304,
        VeeqoGetSO = 305,
        VeeqoCreateNewProducts = 306,
        Download810FromFTP = 307,
        ShipStationUpdateSKUStocklevels = 308,
        MacysInventoryUpload = 33,
        MacysBulkItemPrices = 34,
        MacysGetOrders = 35,
        MacysASNShipmentNotification = 36,
        MacysCancellationLines = 37,
        TargetPlusInventoryFeedWHSWise = 38,
        LowesInventoryUpload = 43,
        LowesBulkItemPrices = 44,
        LowesGetOrders = 45,
        LowesASNShipmentNotification = 46,
        LowesCancellationLines = 47,
        LowesPriceImport = 66,
        LowesPriceImportStatus = 67,
        AmazonInventoryUpload = 48,
        AmazonGetOrders = 49,
        AmazonInventoryStatus = 50,
        AmazonASNShipmentNotification = 51,
        KnotInventoryUpload = 52,
        KnotBulkItemPrices = 53,
        KnotGetOrders = 54,
        KnotASNShipmentNotification = 55,
        KnotCancellationLines = 56,
        MichealInventoryUpload = 57,
        MichealBulkItemPrices = 58,
        MichealGetOrders = 59,
        MichealASNShipmentNotification = 60,
        MichealCancellationLines = 61,
        AmazonWHSWInventoryUpload = 62,
        WalmartInventoryStatus = 63,
        LowesWHSWInventoryUpload = 64,
        LowesWHSWInventoryStatus = 65,
        KnotWHSWInventoryUpload = 68,
        KnotWHSWInventoryStatus = 69,
        KnotPriceImport = 70,
        KnotPriceImportStatus = 71,
        RepaintGetOrders = 500,
        RepaintCreateOrder = 501,
        RepaintGenerate855 = 502,
        Download856FromShipStation = 503,
        GenerateEDI856ForRepaintRoute = 504,
        GenerateEDI810ForRepaintRoute = 505
    }

    public enum AlertTypesEnum
    { 
        Customer=1,
    }
    public enum ConnectorTypesEnum
    {
        SqlServer = 1,
        Rest = 2,
        SFTP = 3,
        FTP = 4
    }

    public class CommonUtils
    {
        // Connection strings — set via appsettings.json on target server
        public static string ConnectionString { get; set; } = "";
        public static string MySqlConnectionString { get; set; } = "";
        public static string SMTPHost { get; set; } = "smtp.office365.com";
        public static int SMTPPort { get; set; } = 587;
        public static string FromEmailAccount { get; set; } = "";
        public static string FromEmailPWD { get; set; } = "";

        public static string Company = "eSyncMate";

        public static Int32 UploadInventoryTotalThread = 10;
        public static RestSharp.Method GetRequestMethod(string p_Method)
        {
            RestSharp.Method l_Method = RestSharp.Method.Get;

            if(p_Method.Trim().ToLower() == "post")
            {
                l_Method = RestSharp.Method.Post;
            }
            else if (p_Method.Trim().ToLower() == "put")
            {
                l_Method = RestSharp.Method.Put;
            }

            return l_Method;
        }

        public static RestSharp.DataFormat GetRequestBodyFormat(string p_Format)
        {
            RestSharp.DataFormat l_Format = RestSharp.DataFormat.None;

            if (p_Format.Trim().ToLower() == "json")
            {
                l_Format = RestSharp.DataFormat.Json;
            }

            return l_Format;
        }

        public static DataTable ConvertCSVToDataTable(string fileData, string[] columnNames)
        {
            DataTable dataTable = new DataTable();
            int index = 0;

            foreach (string name in columnNames)
            {
                dataTable.Columns.Add(name);
            }

            foreach (string line in fileData.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                if(index > 0)
                {
                    string[] rows = line.Split(',');
                    dataTable.Rows.Add(rows);
                }

                index ++;
            }

            return dataTable;
        }

        
    }

    public static class Extensions
    {
        public static IEnumerable<IEnumerable<T>> ToChunks<T>(this IEnumerable<T> enumerable, int chunkSize)
        {
            int itemsReturned = 0;
            var list = enumerable.ToList(); // Prevent multiple execution of IEnumerable.
            int count = list.Count;
            while (itemsReturned < count)
            {
                int currentChunkSize = Math.Min(chunkSize, count - itemsReturned);
                yield return list.GetRange(itemsReturned, currentChunkSize);
                itemsReturned += currentChunkSize;
            }
        }
    }

}
