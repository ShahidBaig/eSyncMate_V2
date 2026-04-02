using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace eSyncMate.DB.Entities
{
    public class Menus : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TranslationKey { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsExternalLink { get; set; }
        public string ExternalUrl { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public string Company { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }

        private static string TableName { get; set; }
        private static string ViewName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        public Menus() : base() { SetupDBEntity(); }
        public Menus(DBConnector p_Connection) : base(p_Connection) { SetupDBEntity(); }
        public Menus(string p_ConnectionString) : base(p_ConnectionString) { SetupDBEntity(); }

        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(Menus.TableName)) Menus.TableName = "Menus";
            if (string.IsNullOrEmpty(Menus.ViewName)) Menus.ViewName = "Menus";
            if (string.IsNullOrEmpty(Menus.PrimaryKeyName)) Menus.PrimaryKeyName = "Id";
            if (string.IsNullOrEmpty(Menus.EndingPropertyName)) Menus.EndingPropertyName = "CreatedBy";
            if (Menus.DBProperties == null) Menus.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            if (string.IsNullOrEmpty(Menus.InsertQueryStart))
                Menus.InsertQueryStart = PrepareQueries(this, Menus.TableName, Menus.EndingPropertyName, ref l_Query, Menus.DBProperties);
        }

        public void UseConnection(string p_ConnectionString, DBConnector p_Connection = null)
        {
            if (string.IsNullOrEmpty(p_ConnectionString))
                Connection = p_Connection;
            else
                Connection = new DBConnector(p_ConnectionString);
        }

        public bool GetList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.IsNullOrEmpty(p_Fields)
                ? "SELECT * FROM [" + Menus.TableName + "]"
                : "SELECT " + p_Fields + " FROM [" + Menus.TableName + "]";

            if (!string.IsNullOrEmpty(p_Criteria)) l_Query += " WHERE " + p_Criteria;
            if (!string.IsNullOrEmpty(p_OrderBy)) l_Query += " ORDER BY " + p_OrderBy;

            return Connection.GetData(l_Query, ref p_Data);
        }

        public int GetMax()
        {
            DataTable l_Data = new DataTable();
            int l_MaxNo = 1;
            Common l_Common = new Common();
            l_Common.UseConnection(string.Empty, Connection);
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + Menus.PrimaryKeyName + ", '0'))) FROM " + Menus.TableName, ref l_Data))
                return l_MaxNo;
            l_MaxNo = PublicFunctions.ConvertNullAsInteger(l_Data.Rows[0][0], 0) + 1;
            l_Data.Dispose();
            return l_MaxNo;
        }

        public Result SaveNew()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();
                this.Id = this.GetMax();
                string l_Query = this.PrepareInsertQuery(this, Menus.InsertQueryStart, Menus.EndingPropertyName, Menus.DBProperties);
                l_Process = this.Connection.Execute(l_Query);

                if (l_Trans)
                {
                    if (l_Process) { this.Connection.CommitTransaction(); l_Result = Result.GetSuccessResult(); }
                    else { this.Connection.RollbackTransaction(); }
                }
            }
            catch (Exception)
            {
                if (l_Trans) this.Connection.RollbackTransaction();
                throw;
            }

            return l_Result;
        }

        public Result Modify()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();
                string l_Query = this.PrepareUpdateQuery(this, Menus.TableName, Menus.PrimaryKeyName, Menus.EndingPropertyName, Menus.DBProperties);
                l_Process = this.Connection.Execute(l_Query);

                if (l_Trans)
                {
                    if (l_Process) { this.Connection.CommitTransaction(); l_Result = Result.GetSuccessResult(); }
                    else { this.Connection.RollbackTransaction(); }
                }
            }
            catch (Exception)
            {
                if (l_Trans) this.Connection.RollbackTransaction();
                throw;
            }

            return l_Result;
        }

        public Result Delete()
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();
                string l_Query = this.PrepareDeleteQuery(this, Menus.TableName, Menus.PrimaryKeyName);
                l_Process = this.Connection.Execute(l_Query);

                if (l_Trans)
                {
                    if (l_Process) { this.Connection.CommitTransaction(); l_Result = Result.GetSuccessResult(); }
                    else { this.Connection.RollbackTransaction(); }
                }
            }
            catch (Exception)
            {
                if (l_Trans) this.Connection.RollbackTransaction();
                throw;
            }

            return l_Result;
        }

        #region IDisposable Support
        private bool disposedValue;
        protected virtual void Dispose(bool disposing) { if (!disposedValue) { disposedValue = true; } }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        #endregion

        #region IEqualityComparer Support
        public new bool Equals(object x, object y) { return ((Menus)x).Id == ((Menus)y).Id; }
        public new int GetHashCode(object obj) { return this.Id; }

        public bool GetViewList(string Criteria, string Fields, ref DataTable Data, string OrderBy = "")
        {
            throw new NotImplementedException();
        }

        public Result GetObjectFromQuery(string Query, bool isOnlyObject = false)
        {
            throw new NotImplementedException();
        }

        public Result GetObject()
        {
            throw new NotImplementedException();
        }

        public Result GetObjectOnly()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
