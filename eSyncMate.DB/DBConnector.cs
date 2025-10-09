// -----------------------------------------------------------------------
// <copyright file="DBConnector.vb" company="eSoftage">
// © 2014 eSoftage Pvt. Ltd. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace eSyncMate.DB
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class DBConnector
    {
        private int m_ConnectionTries = 1;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public string ConnectionString
        {
            get
            {
                return m_ConnectionString;
            }

            set
            {
                m_ConnectionString = value;
            }
        }

        private string m_ConnectionString;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public static string NullValue
        {
            get
            {
                return m_NullValue;
            }

            set
            {
                m_NullValue = value;
            }
        }

        private static string m_NullValue;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private SqlTransaction Transaction
        {
            get
            {
                return m_Transaction;
            }

            set
            {
                m_Transaction = value;
            }
        }

        private SqlTransaction m_Transaction;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private SqlConnection Connection
        {
            get
            {
                return m_Connection;
            }

            set
            {
                m_Connection = value;
            }
        }

        private SqlConnection m_Connection;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private SqlDataAdapter DataAdapter
        {
            get
            {
                return m_DataAdapter;
            }

            set
            {
                m_DataAdapter = value;
            }
        }

        private SqlDataAdapter m_DataAdapter;

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private SqlCommand Command
        {
            get
            {
                return m_Command;
            }

            set
            {
                m_Command = value;
            }
        }

        private SqlCommand m_Command;

        public int CommandTimeout { get; set; }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private bool IsConnected
        {
            get
            {
                return Connection.State != ConnectionState.Closed && Connection.State != ConnectionState.Broken ? true : false;
            }
        }

        private byte[] m_RoleCookie { get; set; }
        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public DBConnector()
        {
            ConnectionString = string.Empty;
            Connection = new SqlConnection(ConnectionString);
            Command = new SqlCommand();
            DataAdapter = new SqlDataAdapter();
            CommandTimeout = 120;
            Command.CommandTimeout = CommandTimeout;
            if (string.IsNullOrEmpty(NullValue))
            {
                NullValue = "NULL";
            }
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public DBConnector(string p_ConnectionString)
        {
            // If Not Me.Connection Is Nothing Then
            // Exit Sub
            // End If

            ConnectionString = p_ConnectionString;
            Connection = new SqlConnection(ConnectionString);
            Command = new SqlCommand();
            DataAdapter = new SqlDataAdapter();
            CommandTimeout = 120;
            Command.CommandTimeout = CommandTimeout;
            if (string.IsNullOrEmpty(NullValue))
            {
                NullValue = "NULL";
            }
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private bool OpenConnection(bool p_IsSetRole = true, bool p_IsConnectionCheck = true)
        {
            try
            {
                if (string.IsNullOrEmpty(ConnectionString))
                {
                    throw new Exception("Invalid Connection String.");
                }

                if (IsConnected)
                {
                    Connection.Close();
                }

                Connection.ConnectionString = this.ConnectionString;

                Connection.Open();
                m_ConnectionTries = 0;
                return true;
            }
            catch (SqlException ex)
            {
                if (p_IsConnectionCheck == false)
                {
                    throw;
                }

                // If m_ConnectionTries > 5 Then
                // Throw
                // End If

                // m_ConnectionTries += 1
                return OpenConnection(p_IsSetRole);
            }
            catch
            {
                // TODO: Implement Log
            }

            return false;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        private bool CloseConnection()
        {
            try
            {
                if (Information.IsNothing(m_RoleCookie))
                {
                    m_ConnectionTries = 1;
                    Connection.Close();
                    return true;
                    return default;
                }

                m_ConnectionTries = 1;
                Connection.Close();
                return true;
            }
            // TODO: Implement Log
            catch (Exception ex)
            {
            }

            return false;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public bool GetData(string l_Query, ref DataTable data, int p_Timeout = 0, bool p_IsSetRole = true, bool p_IsConnectionCheck = true)
        {
            try
            {
                if (Transaction is null)
                {
                    OpenConnection(p_IsSetRole, p_IsConnectionCheck);
                }

                Command.CommandText = l_Query;
                Command.Connection = Connection;
                Command.Transaction = Transaction;
                DataAdapter.SelectCommand = Command;
                if (p_Timeout > 0)
                {
                    Command.CommandTimeout = p_Timeout;
                }

                if (Information.IsNothing(data))
                {
                    data = new DataTable();
                }

                data.Rows.Clear();
                data.Columns.Clear();
                DataAdapter.Fill(data);
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog(l_Query);
                }

                if (data.Rows.Count <= 0)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("Error: " + l_Query);
                }

                throw;
            }
            finally
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }

                Command.CommandTimeout = CommandTimeout;
            }

            return false;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public bool GetDataSP(string l_Query, ref DataTable data, int p_Timeout = 0, bool p_IsSetRole = true)
        {
            try
            {
                if (Transaction is null)
                {
                    OpenConnection(p_IsSetRole);
                }

                Command.CommandText = l_Query;
                Command.Connection = Connection;
                Command.Transaction = Transaction;
                if (p_Timeout > 0)
                {
                    Command.CommandTimeout = p_Timeout;
                }

                DataAdapter.SelectCommand = Command;
                if (Information.IsNothing(data))
                {
                    data = new DataTable();
                }

                data.Rows.Clear();
                data.Columns.Clear();
                DataAdapter.Fill(data);
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog(l_Query);
                }

                if (data.Rows.Count <= 0)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("Error: " + l_Query);
                }

                throw;
            }
            finally
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }

                Command.CommandTimeout = CommandTimeout;
            }

            return false;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public bool GetDataReader(string l_Query, ref DataTable data)
        {
            try
            {
                SqlDataReader reader;
                if (Transaction is null)
                {
                    OpenConnection();
                }

                Command.CommandText = l_Query;
                Command.Connection = Connection;
                Command.Transaction = Transaction;
                DataAdapter.SelectCommand = Command;
                data.Rows.Clear();
                data.Columns.Clear();
                DataAdapter.Fill(data);
                if (data is RecordSet)
                {
                    reader = Command.ExecuteReader(CommandBehavior.KeyInfo);
                    ((RecordSet)data).Schema = reader.GetSchemaTable();
                    reader.Close();
                }

                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog(l_Query);
                }

                if (data.Rows.Count <= 0)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("Error: " + l_Query);
                }

                throw;
            }
            finally
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }
            }

            return false;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public bool Execute(string l_Query, int p_Timeout = -1, SqlParameter[] p_SQLParams = null, bool p_IsSetRole = true)
        {
            bool l_Process = false;
            try
            {
                if (Transaction is null)
                {
                    OpenConnection(p_IsSetRole);
                }

                Command.CommandText = l_Query;
                Command.CommandType = CommandType.Text;
                Command.Connection = Connection;
                Command.CommandTimeout = CommandTimeout;
                Command.Parameters.Clear();
                if (!Information.IsNothing(p_SQLParams))
                {
                    foreach (var index in p_SQLParams)
                        Command.Parameters.Add(index);
                }

                if (p_Timeout > 0)
                {
                    Command.CommandTimeout = p_Timeout;
                }

                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog(l_Query);
                }

                if (Command.ExecuteNonQuery() > 0)
                {
                    l_Process = true;
                }
            }
            catch
            {
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("Error: " + l_Query);
                }

                throw;
            }
            finally
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }

                Command.CommandTimeout = CommandTimeout;
            }

            return l_Process;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public object ExecuteScalar(string l_Query, int p_Timeout = -1)
        {
            object ExecuteScalarRet = default;
            try
            {
                if (Transaction is null)
                {
                    OpenConnection();
                }

                Command.CommandText = l_Query;
                Command.CommandType = CommandType.Text;
                Command.Connection = Connection;
                Command.CommandTimeout = CommandTimeout;
                if (p_Timeout > 0)
                {
                    Command.CommandTimeout = p_Timeout;
                }

                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog(l_Query);
                }

                ExecuteScalarRet = Command.ExecuteScalar();
            }
            catch
            {
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("Error: " + l_Query);
                }

                throw;
            }
            finally
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }

                Command.CommandTimeout = CommandTimeout;
            }

            return ExecuteScalarRet;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public bool ExecuteSP(string Query)
        {
            bool l_Process = false;
            try
            {
                if (Transaction is null)
                {
                    OpenConnection();
                }

                Command.CommandText = Query;
                Command.CommandType = CommandType.StoredProcedure;
                Command.Connection = Connection;
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog(Query);
                }

                if (Command.ExecuteNonQuery() > 0)
                {
                    l_Process = true;
                }
            }
            catch
            {
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("Error: " + Query);
                }

                throw;
            }
            finally
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }
            }

            return l_Process;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public bool BeginTransaction()
        {
            bool l_Process = false;
            try
            {
                l_Process = false;
                if (Transaction is object)
                {
                    return l_Process;
                }

                OpenConnection();
                Transaction = Connection.BeginTransaction();
                Command.Transaction = Transaction;
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("/****************************************Begin Transaction********************************/");
                }

                l_Process = true;
            }
            // TODO: Implement Log
            catch (Exception ex)
            {
            }

            return l_Process;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public bool CommitTransaction()
        {
            try
            {
                if (Transaction is null)
                {
                    throw new Exception("No Transaction is alive.");
                }

                Transaction.Commit();
                CloseConnection();
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("/****************************************Commit Transaction********************************/");
                }

                Transaction = null;
                Command.Transaction = Transaction;
            }
            // TODO: Implement Log
            catch (Exception ex)
            {
                CloseConnection();
            }

            return true;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public bool RollbackTransaction()
        {
            try
            {
                if (Transaction is null)
                {
                    throw new Exception("No Transaction is alive.");
                }

                Transaction.Rollback();
                CloseConnection();
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("/****************************************Rollback Transaction********************************/");
                }

                Transaction = null;
                Command.Transaction = Transaction;
            }
            // TODO: Implement Log
            catch (Exception ex)
            {
                CloseConnection();
            }

            return true;
        }

        public bool CopyDataTable(string p_TableName, DataTable p_Table)
        {
            bool l_Process = false;
            try
            {
                if (Transaction is null)
                {
                    OpenConnection();
                }

                using (var bulkCopy = new SqlBulkCopy(Connection))
                {
                    bulkCopy.DestinationTableName = p_TableName;
                    bulkCopy.WriteToServer(p_Table);
                }

                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("/********************************SQL Bulk Copy [" + p_TableName + "]********************************/");
                }

                l_Process = true;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }
            }

            return l_Process;
        }

        public bool BulkInsert(string p_CommendName, string p_ParamName, DataTable p_DataTable)
        {
            bool l_Process = false;
            try
            {
                if (Transaction is null)
                {
                    OpenConnection();
                }

                Command.CommandText = p_CommendName;
                Command.CommandType = CommandType.StoredProcedure;
                Command.Connection = Connection;
                Command.Parameters.AddWithValue(p_ParamName, p_DataTable);
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog(p_CommendName);
                }

                if (Conversions.ToBoolean(Operators.ConditionalCompareObjectGreater(Command.ExecuteScalar(), 0, false)))
                {
                    l_Process = true;
                }
            }
            catch
            {
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("Error: " + p_CommendName);
                }

                throw;
            }
            finally
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }
            }

            return l_Process;
        }

        public bool BulkUpdateSP(SqlCommand p_SqlCommand, ref DataTable p_DT, string p_SPName = "")
        {
            bool l_Process = false;
            try
            {
                if (Transaction is null)
                {
                    OpenConnection();
                }

                Command = p_SqlCommand;
                Command.CommandType = CommandType.StoredProcedure;
                Command.Connection = Connection;
                DataAdapter.SelectCommand = Command;
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog(p_SPName);
                }

                p_DT.Rows.Clear();
                p_DT.Columns.Clear();
                DataAdapter.Fill(p_DT);
                if (p_DT.Rows.Count <= 0)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("Error: " + p_SPName);
                }

                throw;
            }
            finally
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }
            }

            return l_Process;
        }

        public bool GetDataSP(string l_Query, ref DataSet data, int p_Timeout = 0, bool p_IsSetRole = true)
        {
            try
            {
                if (Transaction is null)
                {
                    OpenConnection(p_IsSetRole);
                }

                Command.CommandText = l_Query;
                Command.Connection = Connection;
                Command.Transaction = Transaction;
                if (p_Timeout > 0)
                {
                    Command.CommandTimeout = p_Timeout;
                }

                DataAdapter.SelectCommand = Command;
                if (data is null)
                {
                    data = new DataSet();
                }
                else
                {
                    data.Tables.Clear();
                }

                DataAdapter.Fill(data);
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog(l_Query);
                }

                if (data.Tables.Count > 0)
                {
                    return data.Tables.Cast<DataTable>().Any(table => table.Rows.Count != 0);
                }

                return false;
            }
            catch
            {
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("Error: " + l_Query);
                }

                throw;
            }
            finally
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }

                Command.CommandTimeout = CommandTimeout;
            }

            return false;
        }

        public bool BulkInsert(string p_TableName, DataTable p_DataTable)
        {
            bool l_Process = false;
            try
            {
                OpenConnection();

                using (var bulkCopy = new SqlBulkCopy(Connection, SqlBulkCopyOptions.KeepNulls, Transaction))
                {
                    bulkCopy.DestinationTableName = p_TableName;

                    // Write from the source to the destination.
                    foreach (DataColumn l_Col in p_DataTable.Columns)
                        bulkCopy.ColumnMappings.Add(l_Col.ColumnName, l_Col.ColumnName);
                    bulkCopy.WriteToServer(p_DataTable);
                    l_Process = true;
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                CloseConnection();
            }

            return l_Process;
        }

        public bool ExecuteSP(string p_Query, ref Hashtable p_Parameters)
        {
            bool process = false;
            try
            {
                SqlParameter l_Param = null;
                var l_Command = new SqlCommand();
                if (Transaction is null)
                {
                    OpenConnection();
                }

                l_Command.Transaction = Transaction;
                l_Command.CommandText = p_Query;
                l_Command.CommandType = CommandType.StoredProcedure;
                l_Command.Connection = Connection;
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog(p_Query);
                }

                l_Command.Parameters.Clear();
                if (p_Parameters is object)
                {
                    foreach (string l_Key in p_Parameters.Keys)
                    {
                        l_Param = (SqlParameter)p_Parameters[l_Key];
                        l_Command.Parameters.Add(l_Param);
                    }
                }

                if (l_Command.ExecuteNonQuery() > 0)
                {
                    process = true;
                }
            }
            catch
            {
                if (Declarations.g_WriteQueries)
                {
                    QueryLogger.WriteToQueryLog("Error: " + p_Query);
                }

                throw;
            }
            finally
            {
                if (Transaction is null)
                {
                    CloseConnection();
                }
            }

            return process;
        }

        public DataTable GetDataBulkInsert(string p_SPName, List<SqlParameter> p_SQLParameters)
        {
            var l_dt = new DataTable();
            try
            {
                using (var con = new SqlConnection(Declarations.g_ConnectionString))
                {
                    using (var cmd = new SqlCommand(p_SPName))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Connection = con;
                        foreach (SqlParameter l_SQLParameter in p_SQLParameters)
                            cmd.Parameters.Add(l_SQLParameter);
                        con.Open();
                        var l_Adapter = new SqlDataAdapter(cmd);
                        l_Adapter.Fill(l_dt);
                        con.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return l_dt;
        }
    }
}