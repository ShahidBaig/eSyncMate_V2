using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace eSyncMate.DB.Entities
{
    public class UserMenus : DBEntity, IDBEntity, IDisposable, IEqualityComparer
    {
        public int Id { get; set; }
        public int UserId { get; set; }
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

        public UserMenus() : base() { SetupDBEntity(); }
        public UserMenus(DBConnector p_Connection) : base(p_Connection) { SetupDBEntity(); }
        public UserMenus(string p_ConnectionString) : base(p_ConnectionString) { SetupDBEntity(); }

        private void SetupDBEntity()
        {
            string l_Query = string.Empty;

            if (string.IsNullOrEmpty(UserMenus.TableName)) UserMenus.TableName = "UserMenus";
            if (string.IsNullOrEmpty(UserMenus.PrimaryKeyName)) UserMenus.PrimaryKeyName = "Id";
            if (string.IsNullOrEmpty(UserMenus.EndingPropertyName)) UserMenus.EndingPropertyName = "CreatedBy";
            if (UserMenus.DBProperties == null) UserMenus.DBProperties = new List<PropertyInfo>(this.GetType().GetProperties());
            if (string.IsNullOrEmpty(UserMenus.InsertQueryStart))
                UserMenus.InsertQueryStart = PrepareQueries(this, UserMenus.TableName, UserMenus.EndingPropertyName, ref l_Query, UserMenus.DBProperties);
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
                ? "SELECT * FROM [" + UserMenus.TableName + "]"
                : "SELECT " + p_Fields + " FROM [" + UserMenus.TableName + "]";

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
            if (!l_Common.GetList("SELECT MAX(CONVERT(INT, ISNULL(" + UserMenus.PrimaryKeyName + ", '0'))) FROM " + UserMenus.TableName, ref l_Data))
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
                string l_Query = this.PrepareInsertQuery(this, UserMenus.InsertQueryStart, UserMenus.EndingPropertyName, UserMenus.DBProperties);
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

        public Result DeleteByUserId(int userId)
        {
            Result l_Result = Result.GetFailureResult();
            bool l_Trans = false;
            bool l_Process = false;

            try
            {
                l_Trans = this.Connection.BeginTransaction();
                string l_Query = $"DELETE FROM [{UserMenus.TableName}] WHERE UserId = {userId}";
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
                string l_Query = this.PrepareDeleteQuery(this, UserMenus.TableName, UserMenus.PrimaryKeyName);
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
        public new bool Equals(object x, object y) { return ((UserMenus)x).Id == ((UserMenus)y).Id; }
        public new int GetHashCode(object obj) { return this.Id; }

        public bool GetViewList(string Criteria, string Fields, ref DataTable Data, string OrderBy = "") { throw new NotImplementedException(); }
        public Result GetObjectFromQuery(string Query, bool isOnlyObject = false) { throw new NotImplementedException(); }
        public Result GetObject() { throw new NotImplementedException(); }
        public Result GetObjectOnly() { throw new NotImplementedException(); }
        public Result Modify() { throw new NotImplementedException(); }
        #endregion
    }
}
