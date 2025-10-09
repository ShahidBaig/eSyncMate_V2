using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using Microsoft.VisualBasic.CompilerServices;

namespace eSyncMate.DB
{


    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DocumentFields : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int FieldNo { get; set; }
        public int? DocumentNo { get; set; }
        [Required(ErrorMessage = "Please provide a FieldName")]
        public string FieldName { get; set; }
        public string ControlName { get; set; }
        public bool IsMandatory { get; set; }
        public int FieldLength { get; set; }
        [Required(ErrorMessage = "Please provide a FieldType")]
        public string FieldType { get; set; }
        public string DependentField { get; set; }
        public string Content { get; set; }
        public bool IsHiddenField { get; set; }
        public bool IsSearchField { get; set; }
        public bool IsResultField { get; set; }
        public bool IsPrimaryField { get; set; }
        public bool IsDefaultField { get; set; }
        public bool IsEditableField { get; set; }
        public bool IsVirtualField { get; set; }

        // Public Property LineMode As System.Int32
        // Public Property ResultFieldsDisplayOrder As System.Int32

        public string DependentValue { get; set; }
        public object FieldValue { get; set; }
        public int TempDocumentNo { get; set; }
        public string DocumentId { get; set; }
        public int TempUserNo { get; set; }
        public Declarations.FormModes DocumentMode { get; set; }
        public int UserNo { get; set; }
        public Declarations.LineModes LineMode { get; set; }
        public bool IsHidden { get; set; }
        public bool IsProperty { get; set; }
        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private static string TableName
        {
            get
            {
                return m_TableName;
            }

            set
            {
                m_TableName = value;
            }
        }

        private static string m_TableName;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public static List<string> PrimaryKeyName
        {
            get
            {
                return m_PrimaryKeyName;
            }

            set
            {
                m_PrimaryKeyName = value;
            }
        }

        private static List<string> m_PrimaryKeyName;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private static string InsertQueryStart
        {
            get
            {
                return m_InsertQueryStart;
            }

            set
            {
                m_InsertQueryStart = value;
            }
        }

        private static string m_InsertQueryStart;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public static string EndingPropertyName
        {
            get
            {
                return m_EndingPropertyName;
            }

            set
            {
                m_EndingPropertyName = value;
            }
        }

        private static string m_EndingPropertyName;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public static List<PropertyInfo> DBProperties
        {
            get
            {
                return m_DBProperties;
            }

            set
            {
                m_DBProperties = value;
            }
        }

        private static List<PropertyInfo> m_DBProperties;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public DocumentFields() : base()
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public DocumentFields(DBConnector p_connection) : base(p_connection)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public DocumentFields(string p_connectionstring) : base(p_connectionstring)
        {
            SetupDBEntity();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private void SetupDBEntity()
        {
            if (string.IsNullOrEmpty(TableName))
            {
                TableName = "DocumentFields";
            }

            // If String.IsNullOrEmpty(DocumentFields.PrimaryKeyName) Then
            // DocumentFields.PrimaryKeyName = New List(Of String)()
            if (PrimaryKeyName is null)
            {
                PrimaryKeyName = new List<string>();
                PrimaryKeyName.Add("DocumentNo");
                PrimaryKeyName.Add("FieldNo");
            }

            if (string.IsNullOrEmpty(EndingPropertyName))
            {
                EndingPropertyName = "DependentValue";
            }

            if (DBProperties is null)
            {
                DBProperties = new List<PropertyInfo>(GetType().GetProperties());
            }

            ExtraFields = new List<string>();
            ExtraFields.Add("TempDocumentNo");
            // Me.ExtraFields.Add("IsHiddenField")
            // Me.ExtraFields.Add("IsSearchField")
            // Me.ExtraFields.Add("IsResultField")
            // Me.ExtraFields.Add("IsPrimaryField")
            // Me.ExtraFields.Add("IsDefaultField")
            // Me.ExtraFields.Add("Content")

            if (string.IsNullOrEmpty(InsertQueryStart))
            {
                string l_Query = string.Empty;
                PrepareQueries(this, "Temp_" + TableName, EndingPropertyName, ref l_Query, DBProperties);
                InsertQueryStart = l_Query;
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
                l_Query = "SELECT * FROM [" + TableName + "] WITH (NOLOCK)";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + TableName + "] WITH (NOLOCK)";
            }

            if (!string.IsNullOrEmpty(p_Criteria))
            {
                l_Query += Convert.ToString(" WHERE ") + p_Criteria;
            }

            if (!string.IsNullOrEmpty(p_OrderBy))
            {
                l_Query += Convert.ToString(" ORDER BY ") + p_OrderBy;
            }

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public bool GetViewList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.Empty;
            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [VW_" + TableName + "] WITH (NOLOCK)";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [VW_" + TableName + "] WITH (NOLOCK)";
            }

            if (!string.IsNullOrEmpty(p_Criteria))
            {
                l_Query += Convert.ToString(" WHERE ") + p_Criteria;
            }

            if (!string.IsNullOrEmpty(p_OrderBy))
            {
                l_Query += Convert.ToString(" ORDER BY ") + p_OrderBy;
            }

            return Connection.GetData(l_Query, ref p_Data);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public int GetMax()
        {
            var l_Data = new DataTable();
            int l_MaxNo = 1;
            var l_Common = new Common();
            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(ISNULL(FieldNo, 0)) FROM Temp_" + TableName + " WHERE Temp" + PrimaryKeyName[0] + " = " + TempDocumentNo, ref l_Data))
            {
                return l_MaxNo;
            }

            l_MaxNo = Conversions.ToInteger(Operators.AddObject(PublicFunctions.ConvertNull(l_Data.Rows[0][0], 0), 1));
            l_Data.Dispose();
            return l_MaxNo;
        }


        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObject(int p_FieldNo)
        {
            FieldNo = p_FieldNo;
            return GetObjectFromQuery(PrepareGetObjectQuery(this, Convert.ToString("VW_") + TableName, PrimaryKeyName));
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObject(string p_PropertyName, string p_PropertyValue)
        {
            var property = GetType().GetProperty(p_PropertyName);
            property.SetValue(this, p_PropertyValue, null);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, "VW_" + TableName, p_PropertyName));
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObjectFromQuery(string p_Query, bool isOnlyObject = false)
        {
            var l_Data = new DataTable();
            if (!Connection.GetData(p_Query, ref l_Data))
            {
                return Result.GetNoRecordResult();
            }

            PopulateObject(this, l_Data, DBProperties, EndingPropertyName);
            l_Data.Dispose();
            return Result.GetSuccessResult();
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObject()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, "VW_" + TableName, PrimaryKeyName));
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObjectOnly()
        {
            return GetObjectFromQuery(PrepareGetObjectQuery(this, "VW_" + TableName, PrimaryKeyName), true);
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result GetObjectFromRow(DataTable p_Data, DataRow p_Row)
        {
            PopulateObjectFromRow(this, p_Data, DBProperties, EndingPropertyName, p_Row);
            return Result.GetSuccessResult();
        }

        public Result SaveNew()
        {
            Result SaveNewRet = default;
            bool l_Process = false;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            SaveNewRet = Result.GetFailureResult();
            try
            {
                FieldNo = GetMax();
                l_Query = PrepareInsertQuery(this, InsertQueryStart, EndingPropertyName, DBProperties);
                l_Trans = Connection.BeginTransaction();
                l_Process = Connection.Execute(l_Query);
                if (l_Process)
                {
                    SaveNewRet = PreSaveChecks();
                    if (SaveNewRet.IsSuccess)
                    {
                        l_Query = "EXEC sp_DocumentFields_Create ";
                        PublicFunctions.FieldToParam(TempDocumentNo, ref l_Param, Declarations.FieldTypes.Number);
                        l_Query += " " + l_Param;
                        PublicFunctions.FieldToParam(FieldNo, ref l_Param, Declarations.FieldTypes.Number);
                        l_Query += ", " + l_Param;
                        PublicFunctions.FieldToParam(TempUserNo, ref l_Param, Declarations.FieldTypes.Number);
                        l_Query += ", " + l_Param;
                        Connection.GetDataSP(l_Query, ref l_Data);
                        SaveNewRet.Populate(l_Data);
                    }
                }
            }
            catch (Exception ex)
            {
                SaveNewRet = Result.GetFailureResult();
                throw;
            }
            finally
            {
                l_Data.Dispose();
                if (l_Trans && SaveNewRet.IsSuccess)
                {
                    Connection.CommitTransaction();
                }
                else if (l_Trans)
                {
                    Connection.RollbackTransaction();
                }
            }

            return SaveNewRet;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result Modify()
        {
            Result ModifyRet = default;
            bool l_Process = false;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            var l_Keys = new List<string>();
            ModifyRet = Result.GetFailureResult();
            try
            {
                foreach (var l_Key in PrimaryKeyName)
                {
                    if (l_Key == "DocumentNo")
                    {
                        l_Keys.Add("TempDocumentNo");
                    }
                    else
                    {
                        l_Keys.Add(l_Key);
                    }
                }

                l_Query = PrepareUpdateQuery(this, "Temp_" + TableName, l_Keys, EndingPropertyName, DBProperties);
                l_Trans = Connection.BeginTransaction();
                l_Process = Connection.Execute(l_Query);
                if (l_Process)
                {
                    ModifyRet = PreSaveChecks();
                    if (ModifyRet.IsSuccess)
                    {
                        l_Query = "EXEC sp_DocumentFields_Modify ";
                        PublicFunctions.FieldToParam(TempDocumentNo, ref l_Param, Declarations.FieldTypes.Number);
                        l_Query += " " + l_Param;
                        PublicFunctions.FieldToParam(FieldNo, ref l_Param, Declarations.FieldTypes.Number);
                        l_Query += ", " + l_Param;
                        PublicFunctions.FieldToParam(TempUserNo, ref l_Param, Declarations.FieldTypes.Number);
                        l_Query += ", " + l_Param;
                        Connection.GetDataSP(l_Query, ref l_Data);
                        ModifyRet.Populate(l_Data);
                    }
                }
            }
            catch (Exception ex)
            {
                ModifyRet = Result.GetFailureResult();
                throw;
            }
            finally
            {
                l_Data.Dispose();
                if (l_Trans && ModifyRet.IsSuccess)
                {
                    Connection.CommitTransaction();
                }
                else if (l_Trans)
                {
                    Connection.RollbackTransaction();
                }
            }

            return ModifyRet;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result Delete()
        {
            return Delete(FieldNo);
        }

        public void GetGridColumns(DataTable p_Data)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            bool l_Process = false;
            try
            {
                l_Query = "EXEC sp_GetMainGridColumns ";
                PublicFunctions.FieldToParam(DocumentId, ref l_Param, Declarations.FieldTypes.String);
                l_Query += l_Param;
                Connection.GetData(l_Query, ref p_Data);
            }
            catch (Exception ex)
            {
                l_Process = false;
                throw;
            }
            finally
            {
            }
        }

        public void SetSearchFields(FieldAttributes p_Data)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            bool l_Process = false;
            try
            {
                l_Query = "Insert INTO ";
                l_Query += DocumentId;
            }

            // FieldToParam(Me.DocumentId, l_Param, FieldTypes.String)
            // l_Query += l_Param

            // Me.Connection.GetData(l_Query, "")

            catch (Exception ex)
            {
                l_Process = false;
                throw;
            }
            finally
            {
            }
        }



        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public Result Delete(int p_FieldNo)
        {
            Result DeleteRet = default;
            bool l_Process = false;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            DeleteRet = Result.GetFailureResult();
            try
            {
                FieldNo = p_FieldNo;
                l_Trans = Connection.BeginTransaction();
                l_Query = "EXEC sp_DocumentFields_Delete ";
                PublicFunctions.FieldToParam(TempDocumentNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(FieldNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                DeleteRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                DeleteRet = Result.GetFailureResult();
                throw;
            }
            finally
            {
                l_Data.Dispose();
                if (l_Trans && DeleteRet.IsSuccess)
                {
                    Connection.CommitTransaction();
                }
                else if (l_Trans)
                {
                    Connection.RollbackTransaction();
                }
            }

            return DeleteRet;
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

        public Result PreSaveChecks()
        {
            Result PreSaveChecksRet = default;
            string l_Query = string.Empty;
            var l_Data = new DataTable();
            string l_Param = string.Empty;
            try
            {
                PreSaveChecksRet = Result.GetFailureResult();
                l_Query = "EXEC sp_DocumentFields_PreSaveCheck";
                PublicFunctions.FieldToParam(TempDocumentNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(FieldNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(TempUserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(Convert.ToInt32((int)LineMode), ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                PreSaveChecksRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                l_Data.Dispose();
            }

            return PreSaveChecksRet;
        }

        public void SetValue(Document p_Object, object p_Value, object[] p_Index = null)
        {
            FieldValue = p_Value;
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
            return ((DocumentFields)x).FieldNo == ((DocumentFields)y).FieldNo;
        }

        public new int GetHashCode(object obj)
        {
            return ((DocumentFields)obj).FieldNo;
        }
        #endregion
    }
}