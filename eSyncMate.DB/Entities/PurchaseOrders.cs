using Microsoft.VisualBasic;
using MySqlConnector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace eSyncMate.DB.Entities
{
    public class PurchaseOrders : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public int CustomerId { get; set; }
        public int WarehouseID { get; set; }
        public string ShipServiceCode { get; set; }
        public int InboundEDIId { get; set; }
        public DateTime OrderDate { get; set; }
        public string PONumber { get; set; }
        public string SupplierID { get; set; }
        public string LocationID { get; set; }
        public string POStatus { get; set; }
        public DateTime VExpectedDate { get; set; }
        public string ReferenceNo { get; set; }
        public string ShipToName { get; set; }
        public string ShipToAddress1 { get; set; }
        public string ShipToAddress2 { get; set; }
        public string ShipToCity { get; set; }
        public string ShipToState { get; set; }
        public string ShipToZip { get; set; }
        public string ShipToCountry { get; set; }
        public string ShipToEmail { get; set; }
        public string ShipToPhone { get; set; }
        public string BillToName { get; set; }
        public string BillToAddress1 { get; set; }
        public string BillToAddress2 { get; set; }
        public string BillToCity { get; set; }
        public string BillToState { get; set; }
        public string BillToZip { get; set; }
        public string BillToCountry { get; set; }
        public string BillToEmail { get; set; }
        public string BillToPhone { get; set; }
        public string BuyerId { get; set; }
        public string BuyerName { get; set; }
        public string BuyerAddress1 { get; set; }
        public string BuyerAddress2 { get; set; }
        public string BuyerCity { get; set; }
        public string BuyerState { get; set; }
        public string BuyerZip { get; set; }
        public string BuyerCountry { get; set; }
        public string BuyerEmail { get; set; }
        public string BuyerPhone { get; set; }
        public bool IsStoreOrder { get; set; }
        public int TotalQty { get; set; }
        public decimal TotalExtendedPrice { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public string SupplierName { get; set; }
        public string ItemID { get; set; }
        public string Destination_Warehouse_Name { get; set; }
        public string Destination_Warehouse_Phone { get; set; }
        public string Destination_Warehouse_Zip { get; set; }
        public string Destination_Warehouse_State { get; set; }
        public string Destination_Warehouse_City { get; set; }
        public string Destination_Warehouse_Country { get; set; }
        public string Destination_Warehouse_Address1 { get; set; }
        public string Destination_Warehouse_TaxId { get; set; }
        public string ShippedDate { get; set; }
        public List<PurchaseOrderData> Files { get; set; }
        public List<PurchaseOrderDetail> Detail { get; set; }
        public InboundEDI Inbound { get; set; }
        public List<OutboundEDI> Outbound { get; set; }

        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public PurchaseOrders() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public PurchaseOrders(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public PurchaseOrders(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(PurchaseOrders.TableName))
            {
                PurchaseOrders.TableName = "PurchaseOrders";
            }

            if (string.IsNullOrEmpty(PurchaseOrders.ViewName))
            {
                PurchaseOrders.ViewName = "VW_PurchaseOrders";
            }

            if (string.IsNullOrEmpty(PurchaseOrders.PrimaryKeyName))
            {
                PurchaseOrders.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(PurchaseOrders.EndingPropertyName))
            {
                PurchaseOrders.EndingPropertyName = "CreatedBy";
            }

            if (PurchaseOrders.DBProperties == null)
            {
                PurchaseOrders.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if(string.IsNullOrEmpty(PurchaseOrders.InsertQueryStart))
            {
                PurchaseOrders.InsertQueryStart = PrepareQueries(this, PurchaseOrders.TableName, PurchaseOrders.EndingPropertyName, ref l_Query, PurchaseOrders.DBProperties);
            }
        }

        public void UseConnection(string p_ConnectionString, DBConnector p_Connection = null)
        {
            if (string.IsNullOrEmpty(p_ConnectionString))
            {
                Connection = p_Connection;
            }
            else
            {
                Connection = new DBConnector(p_ConnectionString);
            }
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public bool GetList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + PurchaseOrders.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + PurchaseOrders.TableName + "]";
            }

            if (!string.IsNullOrEmpty(p_Criteria))
            {
                l_Query += " WHERE " + p_Criteria;
            }

            if (!string.IsNullOrEmpty(p_OrderBy))
            {
                l_Query += " ORDER BY " + p_OrderBy;
            }

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetViewList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + PurchaseOrders.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + PurchaseOrders.ViewName + "]";
            }

            if (!string.IsNullOrEmpty(p_Criteria))
            {
                l_Query += " WHERE " + p_Criteria;
            }

            if (!string.IsNullOrEmpty(p_OrderBy))
            {
                l_Query += " ORDER BY " + p_OrderBy;
            }

            if (p_Fields == "VW_Temp_PurchaseOrders")
            {
                p_Fields = String.Empty;

                l_Query = "SELECT * FROM VW_Temp_PurchaseOrders";

                if (!string.IsNullOrEmpty(p_Criteria))
                {
                    l_Query += " WHERE " + p_Criteria;
                }

                if (!string.IsNullOrEmpty(p_OrderBy))
                {
                    l_Query += " ORDER BY " + p_OrderBy;
                }
            }

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObject(int p_PrimaryKey)
        {
            SetProperty(PurchaseOrders.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, PurchaseOrders.ViewName, PurchaseOrders.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + PurchaseOrders.PrimaryKeyName + ", '0'))) FROM " + PurchaseOrders.TableName, ref l_Data))
            {
                return l_MaxNo;
            }

            l_MaxNo = PublicFunctions.ConvertNullAsInteger(l_Data.Rows[0][0], 0) + 1;

            l_Data.Dispose();

            return l_MaxNo;
        }

        public Result GetObjectFromQuery(string p_Query, bool isOnlyObject = false)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            string l_Criteria = string.Empty;
            DataTable l_Data = new DataTable();
            OrderData l_Files = new OrderData();
            OutboundEDI l_Outbound = new OutboundEDI();

            if (!Connection.GetData(p_Query, ref l_Data))
            {
                return Result.GetNoRecordResult();
            }

            PopulateObject(this, l_Data, DBProperties, string.Empty);

            l_Data.Dispose();
            if(isOnlyObject)
            {
                return Result.GetSuccessResult();
            }

            l_Data = new DataTable();

            this.Files = new List<PurchaseOrderData>();

            l_Files.UseConnection(string.Empty, this.Connection);
            l_Files.GetViewList("OrderId = " + PublicFunctions.FieldToParam(this.Id, Declarations.FieldTypes.Number), "*", ref l_Data);

            foreach (DataRow l_Row in l_Data.Rows)
            {
                PurchaseOrderData l_File = new PurchaseOrderData();

                l_File.PopulateObjectFromRow(l_File, l_Data, PurchaseOrderData.DBProperties, string.Empty, l_Row);

                this.Files.Add(l_File);
            }

            l_Data.Dispose();

            this.Inbound = new InboundEDI();
            this.Inbound.UseConnection(string.Empty, this.Connection);
            this.Inbound.GetObject(this.InboundEDIId);

            l_Data = new DataTable();

            this.Outbound = new List<OutboundEDI>();

            l_Outbound.UseConnection(string.Empty, this.Connection);
            l_Outbound.GetViewList("OrderId = " + PublicFunctions.FieldToParam(this.Id, Declarations.FieldTypes.Number), "*", ref l_Data);

            foreach (DataRow l_Row in l_Data.Rows)
            {
                OutboundEDI l_OutboundEDI = new OutboundEDI();

                l_OutboundEDI.PopulateObjectFromRow(l_OutboundEDI, l_Data, OutboundEDI.DBProperties, string.Empty, l_Row);

                this.Outbound.Add(l_OutboundEDI);
            }

            l_Data.Dispose();

            return Result.GetSuccessResult();
        }

        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, PurchaseOrders.ViewName, PurchaseOrders.PrimaryKeyName));
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, PurchaseOrders.ViewName, PurchaseOrders.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            foreach (PropertyInfo l_Prop in l_Property)
            {
                l_Prop.SetValue(this, propertyValue);
            }

            return GetObjectFromQuery(PrepareGetObjectQuery(this, PurchaseOrders.ViewName, propertyName));
        }

        public Result SaveNew()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = string.Empty;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                this.Id = this.GetMax();

                l_Query = this.PrepareInsertQuery(this, PurchaseOrders.InsertQueryStart, PurchaseOrders.EndingPropertyName, PurchaseOrders.DBProperties);

                l_Process = this.Connection.Execute(l_Query);

                if (l_Trans)
                {
                    if(l_Process)
                    {
                        this.Connection.CommitTransaction();
                        l_Result = Result.GetSuccessResult();
                    }
                    else 
                    {
                        this.Connection.RollbackTransaction();
                    }
                }
            }
            catch (Exception)
            {
                if (l_Trans)
                {
                    this.Connection.RollbackTransaction();
                }

                throw;
            }
            finally
            {
            }

            return l_Result;
        }

        public Result Modify()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = string.Empty;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareUpdateQuery(this, PurchaseOrders.TableName, PurchaseOrders.PrimaryKeyName, PurchaseOrders.EndingPropertyName, PurchaseOrders.DBProperties);

                l_Process = this.Connection.Execute(l_Query);

                if (l_Trans)
                {
                    if (l_Process)
                    {
                        this.Connection.CommitTransaction();
                        l_Result = Result.GetSuccessResult();
                    }
                    else
                    {
                        this.Connection.RollbackTransaction();
                    }
                }
            }
            catch (Exception)
            {
                if (l_Trans)
                {
                    this.Connection.RollbackTransaction();
                }

                throw;
            }
            finally
            {
            }

            return l_Result;
        }

        public Result Delete()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = string.Empty;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareDeleteQuery(this, PurchaseOrders.TableName, PurchaseOrders.PrimaryKeyName);

                l_Process = this.Connection.Execute(l_Query);

                if (l_Trans)
                {
                    if (l_Process)
                    {
                        this.Connection.CommitTransaction();
                        l_Result = Result.GetSuccessResult();
                    }
                    else
                    {
                        this.Connection.RollbackTransaction();
                    }
                }
            }
            catch (Exception)
            {
                if (l_Trans)
                {
                    this.Connection.RollbackTransaction();
                }

                throw;
            }
            finally
            {
            }

            return l_Result;
        }

        public Result SetPurchaseOrderstatus(int orderId, string status)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = string.Empty; 

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Query = $"UPDATE PurchaseOrders SET Status = '{status}' WHERE Id = {orderId}";

                l_Process = this.Connection.Execute(l_Query);

                if(l_Process && status == "CANCELLED")
                {
                    l_Query = $"UPDATE OrderDetail SET Status = 'CANRVD', CancelQty = 1 WHERE OrderId = {orderId} AND Status = 'NEW'";

                    l_Process = this.Connection.Execute(l_Query);
                }

                if (l_Trans)
                {
                    if (l_Process)
                    {
                        this.Connection.CommitTransaction();
                        l_Result = Result.GetSuccessResult();
                    }
                    else
                    {
                        this.Connection.RollbackTransaction();
                    }
                }
            }
            catch (Exception)
            {
                if (l_Trans)
                {
                    this.Connection.RollbackTransaction();
                }

                throw;
            }
            finally
            {
            }

            return l_Result;
        }

        public Result MarkForRelease(int OrderId)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();
                string sqlQuery = $"UPDATE PurchaseOrders SET Status = 'NEW' WHERE Id = {OrderId}";

                l_Process = this.Connection.Execute(sqlQuery);

                if (l_Trans)
                {
                    if (l_Process)
                    {
                        this.Connection.CommitTransaction();
                        l_Result = Result.GetSuccessResult();
                    }
                    else
                    {
                        this.Connection.RollbackTransaction();
                    }
                }
            }
            catch (Exception)
            {
                if (l_Trans)
                {
                    this.Connection.RollbackTransaction();
                }

                throw;
            }

            return l_Result;
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        // IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                // TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                // TODO: set large fields to null.
            }

            disposedValue = true;
        }

        // TODO: override Finalize() only if Dispose(ByVal disposing As Boolean) above has code to free unmanaged resources.
        // Protected Overrides Sub Finalize()
        // ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
        // Dispose(False)
        // MyBase.Finalize()
        // End Sub

        // This code added by Visual Basic to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region IEqualityComparer Support
        public new bool Equals(object x, object y)
        {
            return ((PurchaseOrders)x).Id == ((PurchaseOrders)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return this.Id;
        }
        #endregion

        public List<PurchaseOrders> GetNewPurchaseOrders()
        {
            List<PurchaseOrders> purchaseOrdersList = new List<PurchaseOrders>();
            DataTable purchaseOrderTable = new DataTable();
            DataTable purchaseOrderDetailTable = new DataTable();
            PurchaseOrderDetail l_Detail = new PurchaseOrderDetail();
            try
            {
                string purchaseOrderQuery = "Status = 'NEW'";

                if (!this.GetViewList(purchaseOrderQuery, "*", ref purchaseOrderTable, ""))
                    return purchaseOrdersList;

                foreach (DataRow row in purchaseOrderTable.Rows)
                {
                    PurchaseOrders purchaseOrder = new PurchaseOrders();
                    PopulateObjectFromRow(purchaseOrder, purchaseOrderTable, row);

                    purchaseOrder.Detail = new List<PurchaseOrderDetail>();
                    l_Detail.UseConnection(string.Empty, this.Connection);
                    string purchaseOrderDetailQuery = $"OrderID = {purchaseOrder.Id} AND Status = 'New'";

                    if (l_Detail.GetViewList(purchaseOrderDetailQuery, "*", ref purchaseOrderDetailTable))
                    {
                        foreach (DataRow detailRow in purchaseOrderDetailTable.Rows)
                        {
                            PurchaseOrderDetail detail = new PurchaseOrderDetail();
                            PopulateObjectFromRow(detail, purchaseOrderDetailTable, detailRow);
                            purchaseOrder.Detail.Add(detail);
                        }
                    }

                    purchaseOrderDetailTable.Dispose();
                    purchaseOrdersList.Add(purchaseOrder);
                }

                purchaseOrderTable.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching new purchase orders: " + ex.Message);
            }

            return purchaseOrdersList;
        }
        public List<PurchaseOrders> GetNewPurchaseOrders(string supplierID, string PONumber)
        {
            List<PurchaseOrders> l_CLTs = new List<PurchaseOrders>();
            DataTable l_Data = new DataTable();
            string l_Criteria = $"SupplierID = '{supplierID}' AND (Status = 'NEW' ) AND PONumber = '{PONumber}' ";

            if (!this.GetViewList(l_Criteria, "*", ref l_Data, "CreatedDate"))
                return l_CLTs;

            foreach (DataRow l_Row in l_Data.Rows)
            {
                PurchaseOrders l_CLT = new PurchaseOrders();

                PopulateObjectFromRow(l_CLT, l_Data, l_Row);

                l_CLT.Files = new List<PurchaseOrderData>();
                l_CLT.Detail = new List<PurchaseOrderDetail>();


                DataTable l_FilesData = new DataTable();
                PurchaseOrderData l_Files = new PurchaseOrderData();
                PurchaseOrderDetail l_Detail = new PurchaseOrderDetail();
                DataTable l_DetailData = new DataTable();


                l_Files.UseConnection(string.Empty, this.Connection);
                l_Files.GetViewList("OrderID = " + PublicFunctions.FieldToParam(l_CLT.Id, Declarations.FieldTypes.Number), "*", ref l_FilesData);

                foreach (DataRow l_FRow in l_FilesData.Rows)
                {
                    PurchaseOrderData l_File = new PurchaseOrderData();

                    l_File.PopulateObjectFromRow(l_File, l_FilesData, PurchaseOrderData.DBProperties, string.Empty, l_FRow);

                    l_CLT.Files.Add(l_File);
                }

                l_Detail.UseConnection(string.Empty, this.Connection);
                l_Detail.GetViewList("OrderID = " + PublicFunctions.FieldToParam(l_CLT.Id, Declarations.FieldTypes.Number), "*", ref l_DetailData);

                foreach (DataRow l_FRow in l_DetailData.Rows)
                {
                    PurchaseOrderDetail l_Details = new PurchaseOrderDetail();

                    l_Details.PopulateObjectFromRow(l_Details, l_DetailData, PurchaseOrderDetail.DBProperties, string.Empty, l_FRow);

                    l_CLT.Detail.Add(l_Details);
                }

                l_FilesData.Dispose();
                l_DetailData.Dispose();

                //l_CLT.Inbound = new InboundEDI();
                //l_CLT.Inbound.UseConnection(string.Empty, this.Connection);
                //l_CLT.Inbound.GetObject(l_CLT.InboundEDIId);

                l_CLTs.Add(l_CLT);
            }

            l_Data.Dispose();

            return l_CLTs;
        }

        public bool UpdatePoStatus(string PoNumber, string status,string ConnectionString)
        {
            bool isUpdated = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    string query = "UPDATE [PurchaseOrders] " +
                                   "SET [Status] = @Status " +
                                   "WHERE [PONumber] = @PONumber";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Status", status);
                        command.Parameters.AddWithValue("@PONumber", PoNumber);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            isUpdated = true;
                        }
                    }
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred: " + ex.Message);
            }

            return isUpdated;
        }

        public DataTable GetSuppliers(string connectionString,ref DataTable data)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT SupplierID,Name FROM Suppliers WHERE Status = 'Active'";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            data = new DataTable();
                            adapter.Fill(data);

                            // You can now use the 'data' DataTable as needed
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return data;
        }

        public DataTable GetItems(string connectionString, ref DataTable data)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT ItemID, SKU, Description, Qty, ETAQty, ETADate,ManufacturerName, NDCItemID, PrimaryCategoryName, SecondaryCategoryName  FROM InvFeedFromNDC ";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            data = new DataTable();
                            adapter.Fill(data);

                            // You can now use the 'data' DataTable as needed
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return data;
        }

        public DataTable GetSuppliersItems(string supplierName, string connectionString, ref DataTable data)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT DISTINCT ItemID, SKU, Description, Qty, ETAQty, ETADate, ManufacturerName, NDCItemID, PrimaryCategoryName, SecondaryCategoryName,ProductName FROM InvFeedFromNDC WHERE SupplierName = @SupplierName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SupplierName", supplierName);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                        {
                            data = new DataTable();
                            adapter.Fill(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return data;
        }

    }
}
