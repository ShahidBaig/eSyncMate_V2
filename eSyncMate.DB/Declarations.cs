using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;

namespace eSyncMate.DB
{
    public static class Declarations
    {
        public const int cAdmin = 1;
        public static ActiveClient g_ActiveClient;
        public static string g_ApplicationPath = string.Empty;
        public static string g_ApplicationLogPath = string.Empty;
        public static string UserDefaultWHS = string.Empty;
        public static short adVarChar = 200;
        public static short adChar = 129;
        public static short adUnsignedTinyInt = 17;
        public static short adBoolean = 11;
        public static short adNumeric = 131;
        public static short adCurrency = 6;
        public static short adInteger = 3;
        public static short adSmallInt = 2;
        public static short adTinyInt = 16;
        public static short adDate = 7;
        public static short adDBDate = 133;
        public static short adDouble = 5;
        public static bool g_WriteQueries = true;
        public static string g_DocNoExceptions = string.Empty;
        public static FieldTypes icBOOLEAN = FieldTypes.Boolean;
        public static FieldTypes icSTRING = FieldTypes.String;
        public static FieldTypes icNUMBER = FieldTypes.Number;
        public static bool g_BDIDSupported = false;
        public static bool g_PKTConsolidateMessage = false;
        public static int g_DataRowsLimit;
        public static int g_ConnectionStringTimeOut = 0;
        public static string g_ConnectionString = string.Empty;
        public static string g_UserIDCookie = "eSyncMate";
        public static int g_UserTokenExpireTime;
        public static int g_UserTokenRenewTime;
        public static int g_Designer;
        public static int g_DesignID;
        public static int g_CustomerIDLength;
        public static int g_CategoryWeight;
        public static int g_CategoryThickness;
        public static int g_APPWebRugsATS;
        public static int g_IntTypeItemStatusKey;
        public static int g_OAKVendorType;
        public static bool g_UserManager = false;
        public static bool g_Connections = false;
        public static bool g_SalesCenters = false;
        public static bool g_Sync = false;
        public static bool g_QtyConversionSAP = false;
        public static bool g_SAPQuery = false;
        public static bool g_SyncSettings = false;
        public static bool g_ItemIdentification = false;
        public static bool g_QuotationAnalyzer = false;
        public static bool g_File = false;
        public static string g_SizeSymbol;
        public static string g_AreaSymbol;
        public static string g_BufferSetupWH;
        public static bool g_ItemSearchByLikeClause;
        public static bool g_SideMarkSearchByLikeClause;
        public static CultureInfo g_GlobalCulture;
        public static int g_EDIManualExportRowsLimit;
        public static bool g_DocumentSearchCombo = false;
        public static List<string> g_DocumentNameIgnoreWords = new List<string>();
        public static List<string> g_SkipControllerActions = new List<string>();
        public static List<string> g_SkipRequestParams = new List<string>();
        public static List<string> g_TrimNewLinesFieldNames = new List<string>();
        public static int g_UserNo;
        public static bool g_SPARSGenericErrorEnableSupportTicket { get; set; }
        public static string g_SPARSGenericErrorFolderPath { get; set; }
        public static string g_SPARSGenericErrorToEmailAddresses { get; set; }
        public static string g_SPARSGenericErrorEmailSetupCriteria { get; set; }
        public static List<string> g_IgnoreUnvalidatedRoutes { get; set; } = new List<string>();

        public delegate void ProgressChangedEvent(ProgressChangedEventArgs e);

        public enum LogTypeEnum
        {
            Info = 1,
            Warning = 2,
            Exception = 3,
            Debug = 4,
            Error = 5,
        }

        public enum UserGroups
        {
            Super = 0,
            Admin = 1,
            User = 2,
            PowerUser = 3
        }

        public enum InvoiceSelection
        {
            DateRange = 1,
            InvoiceRange = 2,
            CommaSeprated = 3,
            FileImport = 4
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public enum FieldTypes
        {
            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            NotSupported,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Number,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            NullableNumber,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            NumberFloat,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            NullableFloat,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            String,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            NullableString,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Date,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            NullableDate,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Boolean,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Byte,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            NullableByte,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            ShortNumber,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            NullableShortNumber,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            ByteArray,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Decimal,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            NullableDecimal,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            ReportDate,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            NullableReportDate,
            Combo,
            MultiSelect
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public enum ActiveClient
        {
            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Feizy = 0,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Momeni = 1,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            NOONOO = 2,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            LOLOI = 3,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Dilmaghani = 4,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Bokara = 6,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Safavieh = 7
        }

        public enum RowModes
        {
            /// <summary>
            /// The Row is Intact.
            /// </summary>
            Original,

            /// <summary>
            /// Row is Newly Added.
            /// </summary>
            Added,

            /// <summary>
            /// Row is Modified.
            /// </summary>
            Modified,

            /// <summary>
            /// Row is Deleted.
            /// </summary>
            Deleted
        }

        public enum WHS_Righs
        {
            icViewOnly,
            icFullAccess
        }

        public enum COL_TYPE
        {
            icDATE = FieldTypes.Date,
            icBOOLEAN = FieldTypes.Boolean,
            icFOREIGN_KEY = FieldTypes.Number,
            icNUMBER = FieldTypes.Number,
            icReportDate = FieldTypes.Date,
            icSTRING = FieldTypes.String,
            icNON_EMPTY_STRING = FieldTypes.String,
            icNULLABLE_STRING = FieldTypes.NullableString,
            icNULLABLE_NUMBER = FieldTypes.NullableNumber,
            icNULLABLE_date = FieldTypes.NullableDate
        }

        public enum EntityTypes
        {
            Customer = 1,
            SalesRep = 2
        }

        public enum ReportTypes
        {
            CustomerStatement = 1,
            SalesInvoice = 2,
            CreditMemo = 3,
            Consignments = 4,
            CustomerCredit = 5,
            CustomerDebit = 6,
            OpenOrderDetail = 7,
            SalesOrder = 8,
            ConsignmentOrder = 9,
            APWithCutOffDateForHamid = 10,
            ARWithCutOffDateForHamid = 11,
            ConsignmentReturn = 12,
            CancelOrders = 13
        }

        public enum ServiceTypes
        {
            Daily = 1,
            Weekly = 2,
            Monthly = 3,
            Minutely = 4,
            Hourly = 5
        }

        public const WHS_Righs icViewOnly = WHS_Righs.icViewOnly;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public enum DataSourceTypes
        {
            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            MSSQL = 1,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Excel = 2,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            CSV = 3,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            MSACCESS = 4,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            MySQL = 5,
            DirectoryFilesCount = 6
        }
        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public enum PanelTypes
        {
            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Table = 1,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            BarChart = 2,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            DitributionChart = 3,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            LineChart = 4,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Donut = 5,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            StackChart = 7
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public enum DataFunctions
        {
            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            None,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Month,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            MonthYear,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Year,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Day
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public enum DataGroupFunctions
        {
            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            None,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            COUNT,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            MIN,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            MAX,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            AVG,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            SUM
        }

        public enum VSSearchKey
        {
            STORE,
            DROPSHIPSO,
            DROPSHIPSI,
            Service,
            ItemUPC,
            SCUSTOMERITEMS
        }

        public enum TableNames
        {
            IMPORTSALESORDERS
        }

        public enum SPARSFeatures
        {
            SITempSalesOrders = 1
        }

        public enum LineModes
        {
            Create = 0,
            Modify = 1,
            Delete = 2,
            Blank = 3,
            Original = 4,
            CreateDelete = 5,
            CreateEdit = 6
        }

        public enum FileFormat
        {
            FixedWidth,
            CharSeperated
        }

        public enum FrequencyType
        {
            Minutes = 0,
            Hours = 1,
            Days = 2,
            Weeks = 3,
            Months = 4,
            Events = 5
        }

        public enum FormModes
        {
            Blank = 0,
            Create = 1,
            Modify = 2
        }

        public enum ErrorCodes
        {
            NoSession = 400,
            NoSearchData = 401,
            ViewDocument = 402,
            NoReportData = 403,
            VoidDocument = 404,
            CancelDocument = 405,
            NoRecord = 406,
            OperationFailed = 407,
            AddDetail = 408,
            EditDetail = 409,
            SaveDocument = 410,
            ModifyDocument = 411,
            DeleteDocument = 412,
            CancelDocumentLine = 413,
            DuplicateDocument = 414,
            PreSaveError = 415,
            CloseDocument = 416,
            InvalidSKU = 417,
            FileUpload = 418,
            CreateDocument = 419,
            EditDocument = 420,
            ImportDocument = 421,
            CustomerCreditLimit = 422,
            CustomerConsignmentLimit = 423,
            NotAuthorize = 424,
            CheckPriceCategory = 425,
            CheckDefaultAddress = 426,
            DeleteCustomerAddress = 427,
            DuplicateCarrierCode = 428,
            DesignIDLength = 429,
            MergeItems = 430,
            OAKItems = 431,
            OAKStockIDBlock = 432,
            ReceiveCompleteDocument = 433,
            ReceivePartialDocument = 434,
            GenericError = 499,
            DateValidation = 435,
            DuplicateUCCCode = 436,
            LocationDuplicateCode = 437,
            AllowOpenPeriod = 438,
            EmailSKUPO = 439,
            InvalidAPIKey = 440,
            LockedOrders = 441,
            AllowDuplicatePO = 442,
            AllowBlankPO = 443,
            DuplicateInvoice = 444,
            SessionImport = 445,
            NoError = 498,
            NoImportFound = 500,
            CyclicCountErrors = 449,
            ImportCompletedWithError = 450,
            ImportLengthExceedError = 451,
            ReportManagerRawDataFileSizeExceeded = 452,
            ItemSpecialIDMismatchCustomerID = 453,
            AdjustmentAmountNotMatch = 454,
            OpenDocumentsError = 455,
            PotentiallyDangerousRequest = 456
        }

        public enum SuccessCodes
        {
            OperationCompleted = 100,
            LoadDocument = 101,
            SearchDocument = 102,
            ViewDocument = 103,
            VoidDocument = 104,
            CancelDocument = 105,
            AddDetail = 106,
            EditDetail = 107,
            SaveDocument = 108,
            ModifyDocument = 109,
            DeleteDocument = 110,
            CancelDocumentLine = 111,
            CloseDocument = 112,
            FileUpload = 113,
            CreateDocument = 114,
            EditDocument = 115,
            ImportDocument = 116,
            ValidDocument = 117,
            ReceiveCompleteDocument = 118,
            ReceivePartialDocument = 119,
            ConfirmDocument = 120,
            AlreadyOAK = 121,
            ProgExistsICJournal = 122,
            ProgExistsVPS = 123,
            GenericSuccess = 199,
            ValidAPIKey = 140,
            CAAccountFound = 130,        // 130 - 135 Series is used only for Chart Of Accounts Stored Procedures
            CAAccountNotFound = 135,
            RecordNotFound = 141,
            EmptyShipVia = 142,
            NoWarehouseRights = 143,
            InvoicedBOL = 144,
            LockedBOL_TO = 145,
            LockedPS = 146,
            PSDifferentshippingAddress = 147,
            BOLFound = 148,
            PSFound = 149,
            ManifestPSFound = 150,
            VoidPS = 151,
            VoidedBOL = 152,
            NegativeAppliedPayment = 155,        // 155 - 157 Series is used only for Payment Screen Store Procedures
            NegativeAppliedDiscount = 156,
            OpenAmountExceeded = 157,
            DiscountAmountWithoutAppliedAmount = 158
        }

        public enum ExceptionCodes
        {
            ExceptionOccured = 900,
            LoadDocument = 901,
            SearchDocument = 902,
            ViewDocument = 903,
            ReportData = 904,
            VoidDocument = 905,
            CancelDocument = 906,
            AddDetail = 907,
            EditDetail = 908,
            SaveDocument = 909,
            ModifyDocument = 910,
            DeleteDocument = 911,
            DeleteDocumentAssociation = 912,
            CloseDocument = 913,
            FileUpload = 914,
            CreateDocument = 915,
            EditDocument = 916,
            ImportDocument = 917,
            ReceiveCompleteDocument = 918,
            ReceivePartialDocument = 919,
            ConfirmDocument = 920,
            SessionImport = 921,
            GenericException = 999,
            APIKeyValidation = 940
        }

        public enum NotificationTypes
        {
            RealTimeNotification = 0,
            Email = 1,
            SMS = 2
        }

        public enum ImportErrorLevels
        {
            IsSuccess = 1,
            IsWarning = 2,
            IsError = 3
        }

        public enum BadgeType
        {
            DefaultActions = 1,
            NoView = 2
        }

        public class SPARSCacheKey
        {
            public static string UserList = "UserList";
            public static string ItemsList = "ItemsList";
            public static string BLItemsList = "BLItemsList";
            public static string ItemsSetList = "ItemsSetList";
            public static string SetItemsList = "SetItemsList";
            public static string UsersData = "UsersData";
            public static string DisplayFields = "DisplayFields_";
            public static string DisplayFieldNames = "DisplayFieldNames_";
            public static string SearchFields = "SearchFields_";
            public static string SearchData = "SearchData_";
            public static string ResultData = "ResultData_";
            public static string SearchFieldNames = "SearchFieldNames_";
            public static string SearchStatusValues = "SearchStatusValues_";
            public static string TaxRatesSerialized = "TaxRatesSerialized";
            public static string ShipViaSerialized = "ShipViaSerialized";
            public static string ShipVia = "ShipVia";
            public static string PriceCategoriesSerialized = "PriceCategoriesSerialized";
            public static string PriceCategories = "PriceCategories";
            public static string TaxRates = "TaxRates";
            public static string TaxIDs = "TaxIDs";
            public static string PaymentTerm = "PaymentTerm";
            public static string ShippingCompany = "ShippingCompany";
            public static string Fibers = "Fibers";
            public static string CollectionTypes = "CollectionTypes";
            public static string DefaultCompanyAccounts = "DefaultCompanyAccounts";
            public static string Designs = "Designs";
            public static string GenericSizes = "GenericSizes";
            public static string BLGenericSizes = "BLGenericSizes";
            public static string Locations = "Locations";
            public static string Colors = "Colors";
            public static string ItemStatus = "ItemStatus";
            public static string Classes = "Classes";
            public static string CountryStates = "CountryStates";
            public static string BasicColor = "BasicColor";
            public static string Vendors = "Vendors";
            public static string GeneralColors = "GeneralColors";
            public static string AddressType = "AddressType";
            public static string ShippingBillingOption = "ShippingBillingOption";
            public static string ShippingBillingOptions = "ShippingBillingOptions";
            public static string Collections = "Collections";
            public static string UserPriceCategories = "UserPriceCategories";
            public static string Country = "Country";
            public static string State = "State";
            public static string ShipViaIDs = "ShipViaIDs";
            public static string EventsList = "EventsList";
            public static string SearchFilters = "SearchFilters_";
            public static string SearchFilterFields = "SearchFilterFields_";
            public static string UserWarehouses = "UserWarehouses_";
            public static string TableSchema = "TableSchema_";
            public static string OrderMethod = "OrderMethod";
            public static string POStatus = "POStatus";
            public static string ShipmentMethod = "ShipmentMethod";
            public static string ShipmentMethodList = "ShipmentMethodList";
            public static string VendorPackingSlipStatus = "VendorPackingSlipStatus";
            public static string BLVendorPackingSlipStatus = "BLVendorPackingSlipStatus";
            public static string VendorPackingSlipCostFormat = "VendorPackingSlipCostFormat";
            public static string ItemsReceiptStatus = "ItemsReceiptStatus";
            public static string ItemsReceivingLocations = "ItemsReceivingLocations";
            public static string Company = "Company";
            public static string ActiveUserRoles = "ActiveUserRoles";
            public static string MenuRights = "MenuRights";
            public static string HiddenPanelInterfaces = "HiddenPanelInterfaces";
            public static string UserWarehousesList = "UserWarehousesList_";
            public static string UserWarehousesString = "UserWarehousesString_";
            public static string DocumentImportInformation = "DocumentImportInformation_";
            public static string ContactType = "ContactType";
            public static string ClassList = "ClassList";
            public static string RegionList = "RegionList";
            public static string RegionSerialized = "RegionSerialized";
            public static string Region = "Region";
            public static string CategoryList = "CategoryList";
            public static string Category_List = "Category_List";
            public static string PriceFormats = "PriceFormats";
            public static string PaymentTermsList = "PaymentTermsList";
            public static string TaxRatesList = "TaxRatesList";
            public static string ARCAccountList = "ARCAccountList";
            public static string STLAccountList = "STLAccountList";
            public static string FCRAccountList = "FCRAccountList";
            public static string DRVAccountList = "DRVAccountList";
            public static string CustomerAddresses = "CustomerAddresses";
            public static string GLStructure = "GLStructure";
            public static string GlAccountList = "GlAccountList";
            public static string ApplicationSettings = "ApplicationSettings";
            public static string EDICancellationCodes = "EDICancellationCodes";
            public static string EDICancellationLineCodes = "EDICancellationLineCodes";
            public static string UserApplicationSettings = "UserApplicationSettings";
            public static string ConnectionStrings = "ConnectionStrings";
            public static string WebIconsList = "WebIconsList";
            public static string DataSourcesList = "DataSourcesList";
            public static string WebModulesList = "WebModulesList";
            public static string WebSecurityRoles = "WebSecurityRoles";
            public static string OrderMethodList = "OrderMethodList";
            public static string CollectionList = "CollectionList";
            public static string DesignList = "DesignList";
            public static string DesignListByID = "DesignListByID";
            public static string ColorList = "ColorList";
            public static string ColorListByID = "ColorListByID";
            public static string SizeList = "SizeList";
            public static string QualityList = "QualityList";
            public static string UserWarehouseAddress = "UserWarehouseAddress_";
            public static string PriceCategoryGrid = "PriceCategoryGrid";
            public static string HarmonizedCodesList = "HarmonizedCodesList";
            public static string FreightClassList = "FreightClassList";
            public static string NMFCClassList = "NMFCClassList";
            public static string ShapesList = "ShapesList";
            public static string GeneralSizeList = "GeneralSizeList";
            public static string FibersList = "FibersList";
            public static string CustomerGroups = "CustomerGroups";
            public static string ItemStatusList = "ItemStatusList";
            public static string RugCategories = "RugCategories";
            public static string BLRugCategories = "BLRugCategories";
            public static string BLCutTypes = "BLCutTypes";
            public static string VoucherTypes = "VoucherTypes";
            public static string UserLocationList = "UserLocationList_";
            public static string UsersLocations = "UsersLocations";
            public static string CustomerReturnsBulkStatus = "CustomerReturnsBulkStatus";
            public static string DocumentActions = "DocumentActions";
            public static string DocumentStatusActions = "DocumentStatusActions";
            public static string UserMenus = "UserMenus_";
            public static string UserModules = "UserModules_";
            public static string UserMenusSerialized = "UserMenusSerialized_";
            public static string UserModulesSerialized = "UserModulesSerialized_";
            public static string WebMenuList = "WebMenuList";
            public static string UserTheme = "UserTheme";
            public static string ItemExtendedField06 = "ItemExtendedField06";
            public static string CustomersList = "CustomersList";
            public static string ShipCompanyIDs = "ShipCompanyIDs";
            public static string UPSShipViaIDs = "UPSShipViaIDs";
            public static string FedExShipViaIDs = "FedExShipViaIDs";
            public static string BDID = "BDID";
            public static string ItemExtendedField01 = "ItemExtendedField01";
            public static string ItemExtendedField02 = "ItemExtendedField02";
            public static string ItemExtendedField03 = "ItemExtendedField03";
            public static string ItemExtendedField04 = "ItemExtendedField04";
            public static string ItemExtendedField05 = "ItemExtendedField05";
            public static string ItemExtendedField07 = "ItemExtendedField07";
            public static string ItemExtendedField08 = "ItemExtendedField08";
            public static string ItemExtendedField09 = "ItemExtendedField09";
            public static string ItemExtendedField10 = "ItemExtendedField10";
            public static string ItemExtendedFields = "ItemExtendedFields";
            public static string CustomerExtendedFields = "CustomerExtendedFields";
            public static string VendorExtendedFields = "VendorExtendedFields";
            public static string SalesRepExtendedFields = "SalesRepExtendedFields";
            public static string SuppliersExtendedFields = "SuppliersExtendedFields";
            public static string VendorPackingSlipsExtendedFields = "VendorPackingSlipsExtendedFields";
            public static string SKUExtendedFields = "SKUExtendedFields";
            public static string PurchaseOrdersExtendedFields = "PurchaseOrdersExtendedFields";
            public static string GLAccounts = "GLAccounts";
            public static string GLDetailType = "GLDetailType";
            public static string SubAccount = "SubAccount";
            public static string AccountType = "AccountType";
            public static string ReturnReason = "ReturnReason";
            public static string SMStatus = "SMStatus";
            public static string Drayages = "Drayages";
            public static string ExtendedPropertiesControlTypes = "ExtendedPropertiesControlTypes";
            public static string ExtendedPropertiesFieldDataTypes = "ExtendedPropertiesFieldDataTypes";
            public static string BLPriceCategories = "BLPriceCategories";
            public static string UserBLPriceCategories = "UserBLPriceCategories";
            public static string BLItemsStatus = "BLItemsStatus";
            public static string BLPriceCategoryGrid = "BLPriceCategoryGrid";
            public static string BLPatterns = "BLPatterns";
            public static string BLItemsReceiptStatus = "BLItemsReceiptStatus";
            public static string DocumentsViewRights = "DocumentsViewRights_";
            public static string GenericSizesAll = "GenericSizesAll";
            public static string ShipmentOutBatchType = "ShipmentOutBatchType";
            public static string ScanBatchTypesList = "ScanBatchTypesList";
            public static string Denominator = "Denominator";
            public static string ShipType = "ShipType";
            public static string BatchSubTypes = "BatchSubTypes";
            public static string SergingTypes = "SergingTypes";
            public static string PaymentAccountList = "PaymentAccountList";
            public static string BatchType = "BatchType";
            public static string PROGItemsList = "PROGItemsList";
            public static string ItemSearchFilterStatus = "ItemSearchFilterStatus";
            public static string ItemStatusFilter = "ItemStatusFilter";
            public static string NotShippableReasonsData = "NotShippableReasonsData";
            public static string NotShippableReasons = "NotShippableReasons";
            public static string BLVendorPackingSlipsExtendedFields = "BLVendorPackingSlipsExtendedFields";
            public static string ColorsExtendedFields = "ColorsExtendedFields";
            public static string DesignsExtendedFields = "DesignsExtendedFields";
            public static string RugCategoriesExtendedFields = "RugCategoriesExtendedFields";
            public static string AllowUserPrice = "AllowUserPrice";
            public static string RestrictOperationLocationWise = "RestrictOperationLocationWise";
            public static string WoodFlooringPriceCategory = "WoodFlooringPriceCategory";
            public static string OpenPriceCategory = "OpenPriceCategory";
            public static string GetUserWarehouseWithFullRights = "GetUserWarehouseWithFullRights";
            public static string GetUserWarehouses = "GetUserWarehouses";
            public static string GetItemSetsWithContentKey = "GetItemSetsWithContent";
            public static string LocationTypes = "LocationTypes";
            public static string RefundToCustomerAccount = "RefundToCustomerAccount";
            public static string PaymentModes = "PaymentModes";
            public static string SalesOrdersExtendedFields = "SalesOrdersExtendedFields";
            public static string BLItemsReceiptsStatus = "BLItemsReceiptsStatus";
            public static string PackageType = "PackageType";
            public static string CaliforniaRecycleFee = "CaliforniaRecycleFee";
            public static string InternalSalesRepExtendedFields = "InternalSalesRepExtendedFields";
            public static string MarketingCompanyExtendedFields = "MarketingCompanyExtendedFields";
            public static string WorkOrderStatusData = "WorkOrderStatusData";
            public static string RestrictOperationLocationWiseData = "RestrictOperationLocationWiseData_";
            public static string KeyNames = "KeyNames";
            public static string PrinterConfiguration = "PrinterConfiguration";
        }

        public enum ReportExtensions
        {
            None = 0,
            ActiveReport = 1,
            RDLC = 2,
            CrystalReport = 3,
            RawDataExport = 4
        }

        public enum ControlType
        {
            TextControl = 1,
            ComboControl = 2,
            MultiSelect = 3
        }

        public enum FieldDataType
        {
            Text = 1,
            Integer = 2,
            Currency = 3,
            Date = 4,
            Boolean = 5,
            Decimal = 6
        }

        public static Dictionary<string, DataTable> ScreenSetupData;
        public static int TotalReportsSites = 2;
        public static int CurrentReportsSite = 1;
        public static int CurrentReportsCount = 0;
        public static int g_MenuHearbeatTimeout = 1;
        public static string WebSiteUrl;
        public static string WebSiteUrlWithoutSlash;
        public static TextInfo CultureTextInfo = new CultureInfo("en-US", false).TextInfo;
        public static string m_ClientEndCacheVersion;
        public static string RedisServer;
        public static int RedisPort;
        public static string RedisPassword;
        public static long RedisDB_UserTokenList = 1L;
        public static long RedisDB_AccessTokens = 2L;
        public static long RedisDB_AccessTokenData = 3L;
        public static long RedisDB_AppSettingsData = 4L;
        public static long RedisDB_UserAppSettings = 5L;
        public static long RedisDB_UserMenus = 6L;
        public static long RedisDB_UserSession = 7L;
        public static long RedisDB_UserDataSources = 8L;
        public static long RedisDB_UserBadges = 9L;
        public static long RedisDB_UserPanels = 10L;
        public static long RedisDB_ChatUserList = 11L;
        public static string CurrencySymbol = "$";
        public static DateTime BadSalesInvoiceProcessDate;
        public static string g_RemoteSQLServerUID;
        public static string g_RemoteUserDSN;
        public static string g_RemoteSQLServerPsswd;
        public static string g_RemoteSQLServerDS;
        public static string g_RemoteSQLServerDB;
        public static JsonSerializerSettings g_JSONSetting = new JsonSerializerSettings();
        public static bool g_IsApplicationSetupDone = false;
        public static string GenericReportKey = "GenericReport";
        public static string g_DateFormat = "MM/dd/yyyy";
        public static string ConnectionString = string.Empty;
        public static DataTable EDIExportConfiguration = new DataTable();

        public enum MessageTypes
        {
            Exception,
            Error,
            Message
        }

        public enum CRUDOperations
        {
            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Create,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Update,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Read,

            /// <summary>
            /// TODO: Update summary.
            /// </summary>
            Delete
        }

        public enum TCpChannel
        {
            ConnectionString = 2,
            Reschedule = 3,
            RunSchedule = 4,
            RunDocument = 5,
            ExportEDI810 = 6,
            ExportEDI856 = 7
        }

        public static string g_LocationID = string.Empty;
    }

    public static class JsonRequestBehavior
    {
        public static int AllowGet { get; set; } = 0;
    }
}