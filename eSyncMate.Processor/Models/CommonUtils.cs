using System.Data;
using System.Reflection.PortableExecutable;
using static Hangfire.Storage.JobStorageFeatures;
using System.Xml.Linq;

namespace eSyncMate.Processor.Models
{
    internal enum ResponseCodes
    {
        Success = 200,
        Error = 400,
        Exception = 500,
        CustomerAlreadyExists = 401
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
        AmazonInventoryUpload = 48,
        AmazonGetOrders = 49,
        AmazonInventoryStatus = 50,
        AmazonASNShipmentNotification = 51,
        RepaintGetOrders = 500,
        RepaintCreateOrder = 501,
        RepaintGenerate855 = 502,
        Download856FromShipStation = 503,
        GenerateEDI856ForRepaintRoute = 504,
        GenerateEDI810ForRepaintRoute = 505,


    }
    public enum ConnectorTypesEnum
    {
        SqlServer = 1,
        Rest = 2,
        SFTP = 3,
        FTP = 4
    }

    internal class CommonUtils
    {
        //public static string ConnectionString { get; set; } = "Server=163.47.11.130;Database=ESYNCMATE;UID=sa;PWD=BizMate#1234scs;";
        //public static string ConnectionString { get; set; } = "Server=.;Database=GECKOTECH;UID=sa;PWD=angel;";
        //public static string ConnectionString { get; set; } = "Server=rxo.geckotech.com.mx;Database=EDIProcessor;UID=sa;PWD=Gecko8079;";
       // public static string ConnectionString { get; set; } = "Server=192.168.0.44,7100;Database=ESYNCMATE;UID=esyncmate;PWD=eSyncMate786$$$;";
        //public static string ConnectionString { get; set; } = "Server=209.74.79.232;Database=SURGIMAC;UID=sa;PWD=Surgimac8079;";
        public static string ConnectionString { get; set; } = "Server=192.168.0.44,7100;Database=ESYNCMATE;UID=esyncmate;PWD=eSyncMate786$$$;";
        public static string MySqlConnectionString { get; set; } = "Server=162.241.63.30;Database=geckote1_edi;User=geckote1_esyncmate;Password=Gecko8079;";

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
