using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Microsoft.VisualBasic.CompilerServices;

namespace eSyncMate.DB
{
    public class Document : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int DocumentNo { get; set; }
        public string DocumentID { get; set; }
        public string DocumentDescription { get; set; }
        private string TableName { get; set; }
        private string ViewName { get; set; }
        private string PrimaryKeyName { get; set; }
        private string InsertQueryStart { get; set; }
        private string EndingPropertyName { get; set; }
        public List<DocumentFields> DBProperties { get; set; }
        public Declarations.FormModes DocumentMode { get; set; }
        public int TempUserNo { get; set; }

        /// <summary>
    /// TODO: Update summary.
    /// </summary>
        public Document() : base()
        {
        }

        /// <summary>
    /// TODO: Update summary.
    /// </summary>
        public Document(string p_DocumentID) : base()
        {
            DocumentID = p_DocumentID;
            SetupDBEntity();
        }

        /// <summary>
    /// TODO: Update summary.
    /// </summary>
        public Document(string p_DocumentID, DBConnector p_Connection) : base(p_Connection)
        {
            DocumentID = p_DocumentID;
            SetupDBEntity();
        }

        /// <summary>
    /// TODO: Update summary.
    /// </summary>
        public Document(string p_DocumentID, string p_ConnectionString) : base(p_ConnectionString)
        {
            DocumentID = p_DocumentID;
            SetupDBEntity();
        }

        /// <summary>
    /// TODO: Update summary.
    /// </summary>
        private void SetupDBEntity()
        {
            string l_Query = string.Empty;
            if (string.IsNullOrEmpty(DocumentID) || string.IsNullOrEmpty(Connection.ConnectionString))
            {
                return;
            }

            if (!string.IsNullOrEmpty(TableName))
            {
                return;
            }

            //var l_Doc = new Documents(Connection);
            //if (!l_Doc.GetObject("DocumentID", DocumentID).IsSuccess)
            //{
            //    return;
            //}

            //DocumentNo = l_Doc.DocumentNo;
            //TableName = l_Doc.TableName;
            //ViewName = l_Doc.ViewName;
            //DocumentDescription = l_Doc.TableName;
            //PrimaryKeyName = l_Doc.PrimaryKey;
            //DBProperties = l_Doc.Fields;
            //EndingPropertyName = string.Empty;
            //if (l_Doc.Fields.Count > 0)
            //{
            //    for (int i = 0, loopTo = l_Doc.Fields.Count - 1; i <= loopTo; i++)
            //    {
            //        if (!l_Doc.Fields[i].IsProperty)
            //        {
            //            break;
            //        }

            //        EndingPropertyName = l_Doc.Fields[i].FieldName;
            //    }
            //}

            PrepareQueries(this, "Temp_" + TableName, EndingPropertyName, ref l_Query, DBProperties);
            InsertQueryStart = l_Query.Replace(PrimaryKeyName, "Temp" + PrimaryKeyName);
        }

        /// <summary>
    /// TODO: Update summary.
    /// </summary>
        public bool GetList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.Empty;
            if (string.IsNullOrEmpty(p_Fields))
            {
                l_Query = "SELECT * FROM [" + TableName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + TableName + "]";
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
                l_Query = "SELECT * FROM [" + ViewName + "]";
            }
            else
            {
                l_Query = "SELECT " + p_Fields + " FROM [" + ViewName + "]";
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
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(Temp" + PrimaryKeyName + ", '0'))) FROM Temp_" + TableName, ref l_Data))
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
        public Result GetObject(int p_PrimaryKey)
        {
            SetProperty(PrimaryKeyName, p_PrimaryKey);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ViewName, PrimaryKeyName));
        }

        /// <summary>
    /// TODO: Update summary.
    /// </summary>
        public Result GetObject(string p_PropertyName, string p_PropertyValue)
        {
            SetProperty(p_PropertyName, p_PropertyValue);
            return GetObjectFromQuery(PrepareGetObjectQuery(this, ViewName, p_PropertyName));
        }

        /// <summary>
    /// TODO: Update summary.
    /// </summary>
        public Result GetObjectFromQuery(string p_Query, bool isOnlyObject = false)
        {
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            string l_Criteria = string.Empty;
            var l_Data = new DataTable();
            if (!Connection.GetData(p_Query, ref l_Data))
            {
                return Result.GetNoRecordResult();
            }

            PopulateObject(this, l_Data, DBProperties, string.Empty);
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
                l_Query = PrepareUpdateQuery(this, "Temp_" + TableName, "Temp" + PrimaryKeyName, EndingPropertyName, DBProperties);
                l_Trans = Connection.BeginTransaction();
                l_Process = Connection.Execute(l_Query);
                if (l_Process)
                {
                    SaveNewRet = PreSaveChecks();
                    if (SaveNewRet.IsSuccess)
                    {
                        l_Query = "EXEC sp_" + TableName + "_Create ";
                        PublicFunctions.FieldToParam(TempPrimaryKeyValue(), ref l_Param, Declarations.FieldTypes.Number);
                        l_Query += " " + l_Param;
                        PublicFunctions.FieldToParam(TempUserNo, ref l_Param, Declarations.FieldTypes.Number);
                        l_Query += ", " + l_Param;
                        Connection.GetDataSP(l_Query, ref l_Data);
                        SaveNewRet.Populate(l_Data);
                        if (SaveNewRet.IsSuccess)
                        {
                            SetProperty(PrimaryKeyName, SaveNewRet.DocumentNo);
                        }
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
            ModifyRet = Result.GetFailureResult();
            try
            {
                l_Query = PrepareUpdateQuery(this, "Temp_" + TableName, "Temp" + PrimaryKeyName, EndingPropertyName, DBProperties);
                l_Trans = Connection.BeginTransaction();
                l_Process = Connection.Execute(l_Query);
                if (l_Process)
                {
                    ModifyRet = PreSaveChecks();
                    if (ModifyRet.IsSuccess)
                    {
                        l_Query = "EXEC sp_" + TableName + "_Modify ";
                        PublicFunctions.FieldToParam(TempPrimaryKeyValue(), ref l_Param, Declarations.FieldTypes.Number);
                        l_Query += " " + l_Param;
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
            return Delete(Conversions.ToInteger(PrimaryKeyName));
        }

        /// <summary>
    /// TODO: Update summary.
    /// </summary>
        public Result Delete(int p_PrimaryKey)
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
                SetProperty(PrimaryKeyName, p_PrimaryKey);
                l_Trans = Connection.BeginTransaction();
                l_Query = "EXEC sp_" + TableName + "_Delete ";
                PublicFunctions.FieldToParam(PrimaryKeyValue(), ref l_Param, Declarations.FieldTypes.Number);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(TempUserNo, ref l_Param, Declarations.FieldTypes.Number);
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

        /// <summary>
    /// TODO: Update summary.
    /// </summary>
        public Result Find(Guid p_Id)
        {
            Result FindRet = default;
            bool l_Process = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            FindRet = Result.GetFailureResult();
            try
            {
                l_Query = "EXEC sp_Documents_Find ";
                PublicFunctions.FieldToParam(p_Id.ToString(), ref l_Param, Declarations.FieldTypes.String);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(TempUserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                if (l_Data.Columns.Contains("Success") && !l_Data.Columns.Contains("DocumentID") || l_Data.Rows.Count == 0)
                {
                    return FindRet;
                }

                DocumentID = Conversions.ToString(PublicFunctions.ConvertNull(l_Data.Rows[0]["DocumentID"], string.Empty));
                SetupDBEntity();
                PopulateObject(this, l_Data, DBProperties, EndingPropertyName);
                FindRet = Result.GetSuccessResult();
            }
            catch (Exception ex)
            {
                l_Process = false;
                throw;
            }
            finally
            {
                l_Data.Dispose();
            }

            return FindRet;
        }

        public void UseConnection(string p_ConnectionString, DBConnector p_Connection = null)
        {
            if (string.IsNullOrEmpty(DocumentID))
            {
                throw new Exception("Document ID is not provided yet!");
            }

            if (string.IsNullOrEmpty(p_ConnectionString))
            {
                Connection = p_Connection;
            }
            else
            {
                Connection = new DBConnector(p_ConnectionString);
            }

            SetupDBEntity();
        }

        public string PrimaryKeyValue()
        {
            foreach (DocumentFields l_Property in DBProperties)
            {
                if (string.Compare(l_Property.FieldName, PrimaryKeyName, true) == 0)
                {
                    return Conversions.ToString(l_Property.FieldValue);
                }
            }

            return default;
        }

        public string TempPrimaryKeyValue()
        {
            foreach (DocumentFields l_Property in DBProperties)
            {
                if (string.Compare(l_Property.FieldName, "Temp" + PrimaryKeyName, true) == 0)
                {
                    return Conversions.ToString(l_Property.FieldValue);
                }
            }

            return default;
        }

        public Result PreSaveChecks()
        {
            Result PreSaveChecksRet = default;
            string l_SQL = string.Empty;
            var l_Data = new DataTable();
            string l_Param = string.Empty;
            try
            {
                PreSaveChecksRet = Result.GetFailureResult();
                l_SQL = "EXEC sp_" + TableName + "_PreSaveCheck";
                PublicFunctions.FieldToParam(TempPrimaryKeyValue(), ref l_Param, Declarations.FieldTypes.Number);
                l_SQL += " " + l_Param;
                PublicFunctions.FieldToParam(TempUserNo, ref l_Param, Declarations.FieldTypes.String);
                l_SQL += ", " + l_Param;
                PublicFunctions.FieldToParam(Convert.ToInt32((int)DocumentMode), ref l_Param, Declarations.FieldTypes.Number);
                l_SQL += ", " + l_Param;
                Connection.GetDataSP(l_SQL, ref l_Data);
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

        public Result Create()
        {
            Result CreateRet = default;
            string l_SQL = string.Empty;
            var l_Data = new DataTable();
            string l_Param = string.Empty;
            try
            {
                CreateRet = Result.GetFailureResult();
                l_SQL = "EXEC sp_" + TableName + "_New";
                PublicFunctions.FieldToParam(TempUserNo, ref l_Param, Declarations.FieldTypes.String);
                l_SQL += " " + l_Param;
                Connection.GetDataSP(l_SQL, ref l_Data);
                CreateRet.Populate(l_Data);
                if (CreateRet.IsSuccess)
                {
                    SetProperty("Temp" + PrimaryKeyName, CreateRet.DocumentNo);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                l_Data.Dispose();
            }

            return CreateRet;
        }

        public Result Edit()
        {
            Result EditRet = default;
            string l_SQL = string.Empty;
            var l_Data = new DataTable();
            string l_Param = string.Empty;
            try
            {
                EditRet = Result.GetFailureResult();
                l_SQL = "EXEC sp_" + TableName + "_Edit";
                PublicFunctions.FieldToParam(PrimaryKeyValue(), ref l_Param, Declarations.FieldTypes.Number);
                l_SQL += " " + l_Param;
                PublicFunctions.FieldToParam(TempUserNo, ref l_Param, Declarations.FieldTypes.String);
                l_SQL += ", " + l_Param;
                Connection.GetDataSP(l_SQL, ref l_Data);
                EditRet.Populate(l_Data);
                if (EditRet.IsSuccess)
                {
                    SetProperty("Temp" + PrimaryKeyName, EditRet.DocumentNo);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                l_Data.Dispose();
            }

            return EditRet;
        }

        public DocumentFields GetProperty(string p_Name)
        {
            DocumentFields GetPropertyRet = default;
            GetPropertyRet = null;
            foreach (DocumentFields l_Property in DBProperties)
            {
                if ((l_Property.FieldName ?? "") == (p_Name ?? ""))
                {
                    GetPropertyRet = l_Property;
                    break;
                }
            }

            return GetPropertyRet;
        }

        public new void SetProperty(string p_PropertyName, object p_PropertyValue)
        {
            foreach (DocumentFields l_Property in DBProperties)
            {
                if (string.Compare(l_Property.FieldName, p_PropertyName, true) == 0)
                {
                    l_Property.SetValue(this, p_PropertyValue);
                    break;
                }
            }
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
            return (((Document)x).PrimaryKeyValue() ?? "") == (((Document)y).PrimaryKeyValue() ?? "");
        }

        public new int GetHashCode(object obj)
        {
            return Conversions.ToInteger(((Document)obj).PrimaryKeyValue());
        }
        #endregion
    }
}