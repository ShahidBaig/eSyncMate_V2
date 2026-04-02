using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace eSyncMate.DB.Entities
{
    public class RoleMenus : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int MenuId { get; set; }
        public bool CanView { get; set; } = true;
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedBy { get; set; }

        private static string TableName { get; set; }
        private static string PrimaryKeyName { get; set; }
        private static string InsertQueryStart { get; set; }
        private static string EndingPropertyName { get; set; }
        public static List<PropertyInfo> DBProperties { get; set; }

        public RoleMenus() : base() { SetupDBEntity(); }
        public RoleMenus(DBConnector p_Connection) : base(p_Connection) { SetupDBEntity(); }
        public RoleMenus(string p_ConnectionString) : base(p_ConnectionString) { SetupDBEntity(); }

        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(RoleMenus.TableName)) RoleMenus.TableName = "RoleMenus";
            if (string.IsNullOrEmpty(RoleMenus.PrimaryKeyName)) RoleMenus.PrimaryKeyName = "Id";
            if (string.IsNullOrEmpty(RoleMenus.EndingPropertyName)) RoleMenus.EndingPropertyName = "CreatedBy";
            if (RoleMenus.DBProperties == null) RoleMenus.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            if (string.IsNullOrEmpty(RoleMenus.InsertQueryStart))
                RoleMenus.InsertQueryStart = PrepareQueries(this, RoleMenus.TableName, RoleMenus.EndingPropertyName, ref l_Query, RoleMenus.DBProperties);
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
                ? "SELECT * FROM [" + RoleMenus.TableName + "]"
                : "SELECT " + p_Fields + " FROM [" + RoleMenus.TableName + "]";

            if (!string.IsNullOrEmpty(p_Criteria)) l_Query += " WHERE " + p_Criteria;
            if (!string.IsNullOrEmpty(p_OrderBy)) l_Query += " ORDER BY " + p_OrderBy;

            return Connection.GetData(l_Query, ref p_Data);
        }

        public bool GetViewList(string p_Criteria, string p_Fields, ref DataTable p_Data, string p_OrderBy = "")
        {
            string l_Query = string.IsNullOrEmpty(p_Fields)
                ? "SELECT * FROM [VW_UserMenus]"
                : "SELECT " + p_Fields + " FROM [VW_UserMenus]";

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
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + RoleMenus.PrimaryKeyName + ", '0'))) FROM " + RoleMenus.TableName, ref l_Data))
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
                string l_Query = this.PrepareInsertQuery(this, RoleMenus.InsertQueryStart, RoleMenus.EndingPropertyName, RoleMenus.DBProperties);
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
                string l_Query = this.PrepareDeleteQuery(this, RoleMenus.TableName, RoleMenus.PrimaryKeyName);
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

        public Result DeleteByRoleId(int roleId)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();
                string l_Query = $"DELETE FROM [{RoleMenus.TableName}] WHERE RoleId = {roleId}";
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
        public new bool Equals(object x, object y) { return ((RoleMenus)x).Id == ((RoleMenus)y).Id; }
        public new int GetHashCode(object obj) { return this.Id; }

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

        public Result Modify()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
