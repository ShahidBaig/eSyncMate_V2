using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public class Orders : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public int CustomerId { get; set; }
        public int InboundEDIId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderNumber { get; set; }
        public string VendorNumber { get; set; }
        public string OrderType { get; set; }
        public string ReferenceNo { get; set; }
        public string CustomerOrderNo { get; set; }
        public string ExternalId { get; set; }
        public string ShippingMethod { get; set; }
        public string ShipToId { get; set; }
        public string ShipToName { get; set; }
        public string ShipToAddress1 { get; set; }
        public string ShipToAddress2 { get; set; }
        public string ShipToCity { get; set; }
        public string ShipToState { get; set; }
        public string ShipToZip { get; set; }
        public string ShipToCountry { get; set; }
        public string ShipToEmail { get; set; }
        public string ShipToPhone { get; set; }
        public string BillToId { get; set; }
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
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public string CustomerName { get; set; }

        public List<OrderData> Files { get; set; }
        public InboundEDI Inbound { get; set; }
        public List<OutboundEDI> Outbound { get; set; }

        public List<OrderDetail> Details { get; set; }

        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Orders() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Orders(DBConnector p_Connection) : base(p_Connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Orders(string p_ConnectionString) : base(p_ConnectionString)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(Orders.TableName))
            {
                Orders.TableName = "Orders";
            }

            if (string.IsNullOrEmpty(Orders.ViewName))
            {
                Orders.ViewName = "VW_Orders";
            }

            if (string.IsNullOrEmpty(Orders.PrimaryKeyName))
            {
                Orders.PrimaryKeyName = "Id";
            }

            if (string.IsNullOrEmpty(Orders.EndingPropertyName))
            {
                Orders.EndingPropertyName = "CreatedBy";
            }

            if (Orders.DBProperties == null)
            {
                Orders.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            }

            if (string.IsNullOrEmpty(Orders.InsertQueryStart))
            {
                Orders.InsertQueryStart = PrepareQueries(this, Orders.TableName, Orders.EndingPropertyName, ref l_Query, Orders.DBProperties);
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
                l_Query = "SELECT * FROM [" + Orders.TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + Orders.TableName + "]";
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
                l_Query = "SELECT * FROM [" + Orders.ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + Orders.ViewName + "]";
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

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObject(int p_PrimaryKey)
        {
            SetProperty(Orders.PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Orders.ViewName, Orders.PrimaryKeyName));
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();

            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + Orders.PrimaryKeyName + ", '0'))) FROM " + Orders.TableName, ref l_Data))
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
            if (isOnlyObject)
            {
                return Result.GetSuccessResult();
            }

            l_Data = new DataTable();

            this.Files = new List<OrderData>();

            l_Files.UseConnection(string.Empty, this.Connection);
            l_Files.GetViewList("OrderId = " + PublicFunctions.FieldToParam(this.Id, Declarations.FieldTypes.Number), "*", ref l_Data);

            foreach (DataRow l_Row in l_Data.Rows)
            {
                OrderData l_File = new OrderData();

                l_File.PopulateObjectFromRow(l_File, l_Data, OrderData.DBProperties, string.Empty, l_Row);

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
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Orders.ViewName, Orders.PrimaryKeyName));
        }

        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Orders.ViewName, Orders.PrimaryKeyName), true);
        }

        public Result GetObject(string propertyName, object propertyValue)
        {
            var l_Property = this.GetType().GetProperties().Where(p => p.Name == propertyName);

            foreach (PropertyInfo l_Prop in l_Property)
            {
                l_Prop.SetValue(this, propertyValue);
            }

            return GetObjectFromQuery(PrepareGetObjectQuery(this, Orders.ViewName, propertyName));
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

                l_Query = this.PrepareInsertQuery(this, Orders.InsertQueryStart, Orders.EndingPropertyName, Orders.DBProperties);

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

        public Result Modify()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = string.Empty;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Query = this.PrepareUpdateQuery(this, Orders.TableName, Orders.PrimaryKeyName, Orders.EndingPropertyName, Orders.DBProperties);

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

                l_Query = this.PrepareDeleteQuery(this, Orders.TableName, Orders.PrimaryKeyName);

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

        public Result SetOrderStatus(int orderId, string status)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = string.Empty;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Query = $"UPDATE Orders SET Status = '{status}' WHERE Id = {orderId}";

                l_Process = this.Connection.Execute(l_Query);

                if (l_Process && status == "CANCELLED")
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

        public Result SetOrderStatusSync(int orderId, string status)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = string.Empty;

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                l_Query = $"UPDATE Orders SET Status = '{status}' WHERE Id = {orderId}";

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

        public bool GetDetailData(string p_Criteria, ref DataTable p_Data)
        {
            string l_Query = string.Empty;

            l_Query = "SELECT * FROM VW_OrderDetail WHERE" + p_Criteria;

            return Connection.GetData(l_Query, ref p_Data);
        }

        public Result UpdateShippingInfo(int orderId, string address1, string address2, string city, string state, string postalCode, string country,string ShipToName)
        {
            Result l_Result = new Result();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = string.Empty;
            DataTable dataTable = new DataTable();

            try
            {
                // Begin the transaction
                l_Trans = this.Connection.BeginTransaction();

                // Define the query to select the JSON data using string interpolation for parameter values
                l_Query = $"SELECT Data FROM OrderData WHERE OrderId = {orderId} AND Type = 'API-JSON'";

                // Fetch the existing JSON data using GetData method
                bool isDataRetrieved = this.Connection.GetData(l_Query, ref dataTable, p_Timeout: 0, p_IsSetRole: true, p_IsConnectionCheck: true);

                // Check if data is retrieved and not empty
                if (isDataRetrieved && dataTable.Rows.Count > 0)
                {
                    string jsonData = dataTable.Rows[0]["Data"].ToString();

                    if (!string.IsNullOrWhiteSpace(jsonData))
                    {
                        JObject orderJson = null;

                        // Attempt to parse the JSON data
                        try
                        {
                            orderJson = JObject.Parse(jsonData);
                        }
                        catch (JsonReaderException ex)
                        {
                            l_Result.IsSuccess = false;
                            l_Result.Description = $"Error parsing JSON data: {ex.Message}";
                            return l_Result;
                        }

                        // Update the JSON based on the address object present
                        if (orderJson != null)
                        {
                            if (orderJson["addresses"]?["shipping_address"] != null)
                            {
                                // For JSON with 'shipping_address'
                                
                                orderJson["addresses"]["shipping_address"]["first_name"] = ShipToName;
                                orderJson["addresses"]["shipping_address"]["address1"] = address1;
                                orderJson["addresses"]["shipping_address"]["address2"] = address2;
                                orderJson["addresses"]["shipping_address"]["city"] = city;
                                orderJson["addresses"]["shipping_address"]["state"] = state;
                                orderJson["addresses"]["shipping_address"]["postal_code"] = postalCode;
                                orderJson["addresses"]["shipping_address"]["country_code"] = country;
                            }
                            else if (orderJson["shippingInfo"]?["postalAddress"] != null)
                            {
                                // For JSON with 'postalAddress'
                                orderJson["shippingInfo"]["postalAddress"]["name"] = ShipToName;
                                orderJson["shippingInfo"]["postalAddress"]["address1"] = address1;
                                orderJson["shippingInfo"]["postalAddress"]["address2"] = address2;
                                orderJson["shippingInfo"]["postalAddress"]["city"] = city;
                                orderJson["shippingInfo"]["postalAddress"]["state"] = state;
                                orderJson["shippingInfo"]["postalAddress"]["postalCode"] = postalCode;
                                orderJson["shippingInfo"]["postalAddress"]["country"] = country;
                            }
                            else if (orderJson["customer"]?["shipping_address"] != null)
                            {
                                // For JSON with 'postalAddress'
                                orderJson["customer"]["shipping_address"]["firstname"] = ShipToName;
                                orderJson["customer"]["shipping_address"]["street_1"] = address1;
                                orderJson["customer"]["shipping_address"]["street_2"] = address2;
                                orderJson["customer"]["shipping_address"]["city"] = city;
                                orderJson["customer"]["shipping_address"]["state"] = state;
                                orderJson["customer"]["shipping_address"]["zip_code"] = postalCode;
                                orderJson["customer"]["shipping_address"]["country"] = country;
                            }
                            else if (orderJson["OrderAddress"]?["payload"]?["ShippingAddress"] != null)
                            {
                                // For JSON with 'postalAddress'
                                orderJson["OrderAddress"]["payload"]["ShippingAddress"]["Name"] = ShipToName;
                                orderJson["OrderAddress"]["payload"]["ShippingAddress"]["AddressLine1"] = address1;
                                orderJson["OrderAddress"]["payload"]["ShippingAddress"]["AddressLine2"] = address2;
                                orderJson["OrderAddress"]["payload"]["ShippingAddress"]["City"] = city;
                                orderJson["OrderAddress"]["payload"]["ShippingAddress"]["StateOrRegion"] = state;
                                orderJson["OrderAddress"]["payload"]["ShippingAddress"]["PostalCode"] = postalCode;
                                orderJson["OrderAddress"]["payload"]["ShippingAddress"]["CountryCode"] = country; 
                            }
                            else
                            {
                                l_Result.IsSuccess = false;
                                l_Result.Description = "Address fields not found in JSON.";
                                return l_Result;
                            }

                            // Define the query to update the JSON data with parameterized query
                            l_Query = "UPDATE OrderData SET Data = @UpdatedData WHERE OrderId = @OrderId AND Type = 'API-JSON'";

                            // Prepare SQL parameters for the update
                            SqlParameter[] updateParams = new SqlParameter[]
                            {
                                new SqlParameter("@UpdatedData", orderJson.ToString()),
                                new SqlParameter("@OrderId", orderId)
                            };

                            // Execute the update using Execute method
                            l_Process = this.Connection.Execute(l_Query, p_Timeout: 0, p_SQLParams: updateParams);

                            // Handle transaction commit or rollback based on the result
                            if (l_Trans)
                            {
                                if (l_Process)
                                {
                                    // Commit the transaction if successful
                                    this.Connection.CommitTransaction();
                                    l_Result.IsSuccess = true;
                                    l_Result.Description = "Shipping info updated successfully.";
                                }
                                else
                                {
                                    // Rollback the transaction if failed
                                    this.Connection.RollbackTransaction();
                                    l_Result.IsSuccess = false;
                                    l_Result.Description = "Failed to update shipping info.";
                                }
                            }
                        }
                    }
                    else
                    {
                        l_Result.IsSuccess = false;
                        l_Result.Description = "Order data not found or is empty.";
                    }
                }
                else
                {
                    l_Result.IsSuccess = false;
                    l_Result.Description = "Failed to retrieve order data.";
                }
            }
            catch (Exception ex)
            {
                if (l_Trans)
                {
                    this.Connection.RollbackTransaction();
                }

                l_Result.IsSuccess = false;
                l_Result.Description = $"Error updating shipping info: {ex.Message}";
            }

            return l_Result;
        }

        public Result UpdateShippingAddress(int orderId, string address1, string address2, string city, string state, string postalCode, string country,string ShipToName,string ShipViaCode)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;
            string l_Query = "UPDATE Orders SET ";
            List<string> updates = new List<string>();

            try
            {
                l_Trans = this.Connection.BeginTransaction();

                if (!string.IsNullOrWhiteSpace(address1))
                    updates.Add($"ShipToAddress1 = '{address1}'");

                if (!string.IsNullOrWhiteSpace(address2))
                    updates.Add($"ShipToAddress2 = '{address2}'");

                if (!string.IsNullOrWhiteSpace(city))
                    updates.Add($"ShipToCity = '{city}'");

                if (!string.IsNullOrWhiteSpace(state))
                    updates.Add($"ShipToState = '{state}'");

                if (!string.IsNullOrWhiteSpace(postalCode))
                    updates.Add($"ShipToZip = '{postalCode}'");

                if (!string.IsNullOrWhiteSpace(country))
                    updates.Add($"ShipToCountry = '{country}'");

                if (!string.IsNullOrWhiteSpace(ShipToName))
                    updates.Add($"ShipToName = '{ShipToName}'");

                if (!string.IsNullOrWhiteSpace(ShipViaCode))
                    updates.Add($"ShipViaCode = '{ShipViaCode}'");

                if (updates.Count > 0)
                {
                    l_Query += string.Join(", ", updates) + $" WHERE Id = {orderId}";

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
                else
                {
                    l_Result = Result.GetSuccessResult();
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

        public bool UpdateStatusAndExternalID(int id, string orderNumber, string externalId, string status)
        {
            bool isUpdated = false;
            Orders l_Orders = new Orders();
            l_Orders.UseConnection(string.Empty, this.Connection); // Reuse existing DB connection

            try
            {
                using (SqlConnection connection = new SqlConnection(l_Orders.Connection.ConnectionString))
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    string query = @"
                                    UPDATE [Orders]
                                    SET [ExternalId] = @ExternalId,
                                        [Status] = @Status
                                    WHERE [Id] = @Id AND [OrderNumber] = @OrderNumber";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ExternalId", externalId);
                        command.Parameters.AddWithValue("@Status", status);
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@OrderNumber", orderNumber);

                        int rowsAffected = command.ExecuteNonQuery();
                        isUpdated = rowsAffected > 0;
                    }

                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occurred while updating order: " + ex.Message);
            }

            return isUpdated;
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
            return ((Orders)x).Id == ((Orders)y).Id;
        }

        public new int GetHashCode(object obj)
        {
            return this.Id;
        }
        #endregion


        public bool GetDistributionID(string value, ref DataTable p_Data)
        {
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            string l_criteria = string.Empty;
            var l_dt = new DataTable();

            l_SQL = "SELECT ID,WHSID,ShipNode,CustomerID FROM TargetPlusShipNodes";
            PublicFunctions.FieldToParam(value, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += " WHERE ShipNode = " + l_Param;

            return Connection.GetData(l_SQL, ref p_Data);
        }
    }
}
