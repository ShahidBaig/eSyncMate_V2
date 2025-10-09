using System;
using System.Data;
using System.Runtime.InteropServices;

namespace eSyncMate.DB
{
    public class Common
    {
        private DBConnector Connection;

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

        public bool Execut(string l_SQL)
        {
            return Connection.Execute(l_SQL);
        }

        public object ExecuteScalar(string l_SQL)
        {
            return Connection.ExecuteScalar(l_SQL);
        }

        public bool GetList(string l_SQL, [Optional, DefaultParameterValue(null)] ref RecordSet l_rsHDR, bool p_IsSetRole = true, int p_Timeout = 0)
        {
            if (l_rsHDR is null)
            {
                l_rsHDR = new RecordSet();
            }

            DataTable argdata = l_rsHDR;
            return Connection.GetData(l_SQL, ref argdata, p_Timeout, p_IsSetRole);
        }

        public bool GetList(string l_SQL, [Optional, DefaultParameterValue(null)] ref DataTable l_rsHDR, bool p_IsSetRole = true, int p_Timeout = 0)
        {
            if (l_rsHDR is null)
            {
                l_rsHDR = new DataTable();
            }

            return Connection.GetData(l_SQL, ref l_rsHDR, p_Timeout, p_IsSetRole);
        }

        public bool GetListSP(string l_SQL, [Optional, DefaultParameterValue(null)] ref DataTable l_rsHDR, int p_Timeout = 0)
        {
            if (l_rsHDR is null)
            {
                l_rsHDR = new DataTable();
            }

            return Connection.GetDataSP(l_SQL, ref l_rsHDR, p_Timeout);
        }

        public bool GetListSP(string l_SQL, ref DataSet l_rsHDR, int p_Timeout = 0)
        {
            if (l_rsHDR is null)
            {
                l_rsHDR = new DataSet();
            }

            return Connection.GetDataSP(l_SQL, ref l_rsHDR, p_Timeout);
        }

        /// <summary>
    /// TODO: Update summary.
    /// </summary>
        public Result PerformAction(string p_DocumentName, string p_Action, string p_DocumentNo, int p_UserNo, string p_IsUseWeb, string p_ExtraCode, string p_LineExtraCode)
        {
            Result PerformActionRet = default;
            bool l_Process = false;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            PerformActionRet = Result.GetSuccessResult();
            try
            {
                l_Trans = Connection.BeginTransaction();
                if (p_DocumentName.StartsWith("Web_"))
                {
                    l_Query = "EXEC " + p_DocumentName + "_" + p_Action + " ";
                }
                else
                {
                    l_Query = "EXEC sp_" + p_DocumentName + "_" + p_Action + " ";
                }

                PublicFunctions.FieldToParam(p_DocumentNo, ref l_Param, Declarations.FieldTypes.String);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                if (p_IsUseWeb == "Y")
                {
                    PublicFunctions.FieldToParam(1, ref l_Param, Declarations.FieldTypes.Number);
                    l_Query += ", " + l_Param;
                }

                if (!string.IsNullOrEmpty(p_ExtraCode))
                {
                    PublicFunctions.FieldToParam(p_ExtraCode, ref l_Param, Declarations.FieldTypes.String);
                    l_Query += ", " + l_Param;
                }

                if (!string.IsNullOrEmpty(p_LineExtraCode))
                {
                    if (string.IsNullOrEmpty(p_ExtraCode))
                    {
                        l_Query += ", NULL";
                    }

                    PublicFunctions.FieldToParam(p_LineExtraCode, ref l_Param, Declarations.FieldTypes.String);
                    l_Query += ", " + l_Param;
                }

                Connection.GetDataSP(l_Query, ref l_Data, 450);
                PerformActionRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                l_Process = false;
                throw;
            }
            finally
            {
                l_Data.Dispose();
                if (l_Trans && PerformActionRet.IsSuccess)
                {
                    Connection.CommitTransaction();
                }
                else if (l_Trans)
                {
                    Connection.RollbackTransaction();
                }
            }

            return PerformActionRet;
        }

        public Result PerformFieldAction(string p_DocumentName, string p_PrimaryKey, string p_Id, string p_FieldName, string p_value, int p_UserNo)
        {
            Result PerformFieldActionRet = default;
            bool l_Process = false;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            PerformFieldActionRet = Result.GetSuccessResult();
            try
            {
                l_Trans = Connection.BeginTransaction();
                if (p_DocumentName.StartsWith("Web_"))
                {
                    l_Query = "EXEC " + p_DocumentName + "_FieldAction";
                }
                else
                {
                    l_Query = "EXEC sp_" + p_DocumentName + "_FieldAction";
                }

                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(p_PrimaryKey, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_Id, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_FieldName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_value, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data, 300);
                PerformFieldActionRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                l_Process = false;
                throw;
            }
            finally
            {
                l_Data.Dispose();
                if (l_Trans && PerformFieldActionRet.IsSuccess)
                {
                    Connection.CommitTransaction();
                }
                else if (l_Trans)
                {
                    Connection.RollbackTransaction();
                }
            }

            return PerformFieldActionRet;
        }

        public Result PerformAction(string p_DocumentName, string p_Action, int p_UserNo)
        {
            Result PerformActionRet = default;
            bool l_Process = false;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            PerformActionRet = Result.GetSuccessResult();
            try
            {
                l_Trans = Connection.BeginTransaction();
                if (p_DocumentName.StartsWith("Web_"))
                {
                    l_Query = "EXEC " + p_DocumentName + "_" + p_Action + " ";
                }
                else
                {
                    l_Query = "EXEC sp_" + p_DocumentName + "_" + p_Action + " ";
                }

                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += l_Param;
                Connection.GetDataSP(l_Query, ref l_Data, 300);
                PerformActionRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                l_Process = false;
                throw;
            }
            finally
            {
                l_Data.Dispose();
                if (l_Trans && PerformActionRet.IsSuccess)
                {
                    Connection.CommitTransaction();
                }
                else if (l_Trans)
                {
                    Connection.RollbackTransaction();
                }
            }

            return PerformActionRet;
        }

        public Result PerformAction(string p_DocumentName, string p_Action, int l_DocumentNo, int p_UserNo)
        {
            Result PerformActionRet = default;
            bool l_Process = false;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            PerformActionRet = Result.GetSuccessResult();
            try
            {
                l_Trans = Connection.BeginTransaction();
                if (p_DocumentName.StartsWith("Web_"))
                {
                    l_Query = "EXEC " + p_DocumentName + "_" + p_Action + " ";
                }
                else
                {
                    l_Query = "EXEC sp_" + p_DocumentName + "_" + p_Action + " ";
                }

                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(l_DocumentNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data, 300);
                if (l_Data.Rows.Count > 0)
                {
                    PerformActionRet.Populate(l_Data);
                }
                else
                {
                    PerformActionRet = Result.GetSuccessResult();
                }
            }
            catch (Exception ex)
            {
                l_Process = false;
                throw;
            }
            finally
            {
                l_Data.Dispose();
                if (l_Trans && PerformActionRet.IsSuccess)
                {
                    Connection.CommitTransaction();
                }
                else if (l_Trans)
                {
                    Connection.RollbackTransaction();
                }
            }

            return PerformActionRet;
        }

        public Result UnlockedDocuments(string p_DocumentName, string p_DocumentNo, int p_UserNo)
        {
            Result UnlockedDocumentsRet = default;
            bool l_Process = false;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            UnlockedDocumentsRet = Result.GetSuccessResult();
            try
            {
                l_Trans = Connection.BeginTransaction();
                l_Query = "EXEC sp_Record_Unlock ";
                PublicFunctions.FieldToParam(p_DocumentName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(p_DocumentNo, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                UnlockedDocumentsRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                l_Process = false;
                throw;
            }
            finally
            {
                l_Data.Dispose();
                if (l_Trans && UnlockedDocumentsRet.IsSuccess)
                {
                    Connection.CommitTransaction();
                }
                else if (l_Trans)
                {
                    Connection.RollbackTransaction();
                }
            }

            return UnlockedDocumentsRet;
        }

        public DataTable GetLockedDocumentsQty(string p_AdminUser, int p_UserNo)
        {
            var l_Common = new Common();
            var l_Data = new DataTable();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            l_SQL = "EXEC sp_GetLockedDocumentsQty ";
            PublicFunctions.FieldToParam(p_AdminUser, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += " " + l_Param;
            PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
            l_SQL += ", " + l_Param;
            Connection.GetDataSP(l_SQL, ref l_Data);
            return l_Data;
        }

        public DataTable GetLockedDocuments(string p_AdminUser, int p_UserNo, string p_TableName)
        {
            var l_Common = new Common();
            var l_Data = new DataTable();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            l_SQL = "EXEC sp_GetLockedDocuments ";
            PublicFunctions.FieldToParam(p_AdminUser, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += " " + l_Param;
            PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
            l_SQL += ", " + l_Param;
            PublicFunctions.FieldToParam(p_TableName, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += ", " + l_Param;
            Connection.GetDataSP(l_SQL, ref l_Data);
            return l_Data;
        }

        public Result ReleaseLockedDocumnets(string p_DocumentNo, int p_UserNo, string p_DocumentName)
        {
            Result ReleaseLockedDocumnetsRet = default;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            ReleaseLockedDocumnetsRet = Result.GetFailureResult();
            try
            {
                l_Query = "EXEC sp_LockedDocuments_Release ";
                PublicFunctions.FieldToParam(p_DocumentNo, ref l_Param, Declarations.FieldTypes.String);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_DocumentName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                ReleaseLockedDocumnetsRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                l_Data.Dispose();
                if (l_Trans && ReleaseLockedDocumnetsRet.IsSuccess)
                {
                    Connection.CommitTransaction();
                }
                else if (l_Trans)
                {
                    Connection.RollbackTransaction();
                }
            }

            return ReleaseLockedDocumnetsRet;
        }

        public DataTable GetUPSAccountInfoData()
        {
            var l_Common = new Common();
            var l_Data = new DataTable();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            l_SQL = "EXEC sp_GetUPSAccountInfo_Data";
            Connection.GetDataSP(l_SQL, ref l_Data);
            return l_Data;
        }

        public Result ChangeCurrentUserPassword(string p_currentPassword, string p_newPassword, int p_UserNo)
        {
            Result ChangeCurrentUserPassword = default;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            DataTable l_Data = new DataTable();
            string l_CurrentEncryptedPass = string.Empty;
            string l_NewEncryptedPass = string.Empty;

            ChangeCurrentUserPassword = Result.GetFailureResult();
            try
            {
                l_CurrentEncryptedPass = PublicFunctions.Encrypt(p_currentPassword);
                l_NewEncryptedPass = PublicFunctions.Encrypt(p_newPassword);

                l_Query = "EXEC WEB_UserPassword_Modify ";
                PublicFunctions.FieldToParam(l_CurrentEncryptedPass, ref l_Param, Declarations.FieldTypes.String);
                l_Query += " " + l_Param;
              
                PublicFunctions.FieldToParam(l_NewEncryptedPass, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;

                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;

                Connection.GetDataSP(l_Query, ref l_Data);

                ChangeCurrentUserPassword.Populate(l_Data);
            }
            catch (Exception ex)
            {
                ChangeCurrentUserPassword.Description = ex.Message;
            }
            finally
            {
                l_Data.Dispose();
                if (l_Trans && ChangeCurrentUserPassword.IsSuccess)
                {
                    Connection.CommitTransaction();
                }
                else if (l_Trans)
                {
                    Connection.RollbackTransaction();
                }
            }

            return ChangeCurrentUserPassword;
        }

        public DataTable GetEDISetupData(string p_Tag)
        {
            var l_Common = new Common();
            var l_Data = new DataTable();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            l_SQL = "EXEC sp_GetEDISetup_Data";
            PublicFunctions.FieldToParam(p_Tag, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += " " + l_Param;
            Connection.GetDataSP(l_SQL, ref l_Data);
            return l_Data;
        }

        public DataTable GetFedExData()
        {
            var l_Common = new Common();
            var l_Data = new DataTable();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            l_SQL = "EXEC sp_GetFedEx_Data";
            Connection.GetDataSP(l_SQL, ref l_Data);
            return l_Data;
        }

        public DataTable GetPreLoadSetup()
        {
            var l_Common = new Common();
            var l_Data = new DataTable();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            l_SQL = "SELECT * FROM VW_InterfacePreloadSetup";
            Connection.GetDataSP(l_SQL, ref l_Data);
            return l_Data;
        }

        public DataTable GetLogOffUserData()
        {
            var l_Common = new Common();
            var l_Data = new DataTable();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            l_SQL = "SELECT * FROM VW_InterfaceLogOffUserSetup";
            Connection.GetDataSP(l_SQL, ref l_Data);
            return l_Data;
        }

        public Result UpdateUserLogOffTime(int p_SettingID, int p_UserNo, string p_Tag_Value, int p_User)
        {
            Result UpdateUserLogOffTimeRet = default;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            UpdateUserLogOffTimeRet = Result.GetFailureResult();
            try
            {
                l_Query = "EXEC sp_UpdateUserLogOffTime ";
                PublicFunctions.FieldToParam(p_SettingID, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_Tag_Value, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_User, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                UpdateUserLogOffTimeRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                l_Data.Dispose();
                if (l_Trans && UpdateUserLogOffTimeRet.IsSuccess)
                {
                    Connection.CommitTransaction();
                }
                else if (l_Trans)
                {
                    Connection.RollbackTransaction();
                }
            }

            return UpdateUserLogOffTimeRet;
        }

        public Result UpdateDenominator(decimal p_Denominator, int p_UserNo)
        {
            Result UpdateDenominatorRet = default;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            UpdateDenominatorRet = Result.GetFailureResult();
            try
            {
                l_Query = "EXEC Web_ApplicationSetting_Denominator";
                PublicFunctions.FieldToParam(p_Denominator, ref l_Param, Declarations.FieldTypes.Decimal);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                UpdateDenominatorRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                l_Data.Dispose();
            }

            return UpdateDenominatorRet;
        }

        public DataTable preLoadSetupSelected(string p_MenuTag, int p_SelectAll, string p_SelectAllConfirm, int p_UserNo)
        {
            var l_Common = new Common();
            var l_Data = new DataTable();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            l_SQL = "EXEC sp_InterfacePreloadSetup ";
            PublicFunctions.FieldToParam(p_MenuTag, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += " " + l_Param;
            PublicFunctions.FieldToParam(p_SelectAll, ref l_Param, Declarations.FieldTypes.Number);
            l_SQL += ", " + l_Param;
            PublicFunctions.FieldToParam(p_SelectAllConfirm, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += ", " + l_Param;
            PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
            l_SQL += ", " + l_Param;
            Connection.GetDataSP(l_SQL, ref l_Data);
            return l_Data;
        }

        public Result UpdateApplicationSettings(int p_SettingID, string p_Tagvalue, string p_Tag, int p_UserNo)
        {
            Result UpdateApplicationSettingsRet = default;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            UpdateApplicationSettingsRet = Result.GetFailureResult();
            try
            {
                l_Query = "EXEC sp_ApplicationSettingsData_Update ";
                PublicFunctions.FieldToParam(p_SettingID, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_Tagvalue, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_Tag, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                UpdateApplicationSettingsRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                l_Data.Dispose();
            }

            return UpdateApplicationSettingsRet;
        }

        public Result UpdateDefaultApplicationSettings(string p_Tagvalue, string p_Tag, int p_UserNo)
        {
            Result UpdateDefaultApplicationSettingsRet = default;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            UpdateDefaultApplicationSettingsRet = Result.GetFailureResult();
            try
            {
                l_Query = "EXEC sp_DefaultApplicationSettingsData_Update ";
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(p_Tagvalue, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_Tag, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                UpdateDefaultApplicationSettingsRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                l_Data.Dispose();
            }

            return UpdateDefaultApplicationSettingsRet;
        }

        public Result SaveExtendedProperty(string p_DocumentName, string p_FieldName, string p_FieldValue, int p_UserNo)
        {
            Result SaveExtendedPropertyRet = default;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            SaveExtendedPropertyRet = Result.GetFailureResult();
            try
            {
                l_Query = "EXEC Web_ExtendedProperty_Create";
                PublicFunctions.FieldToParam(p_DocumentName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(p_FieldName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_FieldValue, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                SaveExtendedPropertyRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                l_Data.Dispose();
            }

            return SaveExtendedPropertyRet;
        }

        public Result ModifyExtendedProperty(string p_DocumentName, string p_FieldName, string p_FieldOldValue, string p_FieldValue, int p_UserNo, int p_IsExist, string p_TableName, string p_PrimaryKey)
        {
            Result ModifyExtendedPropertyRet = default;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            ModifyExtendedPropertyRet = Result.GetFailureResult();
            try
            {
                l_Query = "EXEC Web_ExtendedProperty_Modify";
                PublicFunctions.FieldToParam(p_DocumentName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(p_FieldName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_FieldOldValue, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_FieldValue, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_IsExist, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_TableName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_PrimaryKey, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                ModifyExtendedPropertyRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                l_Data.Dispose();
            }

            return ModifyExtendedPropertyRet;
        }

        public Result SaveExtendedPropertyByProperty(string p_DocumentName, string p_FieldName, string p_FieldValue, string p_PropertyName, string p_PropertyValue, int p_UserNo)
        {
            Result SaveExtendedPropertyByPropertyRet = default;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            SaveExtendedPropertyByPropertyRet = Result.GetFailureResult();
            try
            {
                l_Query = "EXEC Web_ExtendedPropertyByProperty_Create";
                PublicFunctions.FieldToParam(p_DocumentName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(p_FieldName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_FieldValue, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_PropertyName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_PropertyValue, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                SaveExtendedPropertyByPropertyRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                l_Data.Dispose();
            }

            return SaveExtendedPropertyByPropertyRet;
        }

        public Result ModifyExtendedPropertyByProperty(string p_DocumentName, string p_FieldName, string p_FieldOldValue, string p_FieldValue, string p_PropertyName, string p_PropertyValue, int p_UserNo)
        {
            Result ModifyExtendedPropertyByPropertyRet = default;
            bool l_Trans = false;
            string l_Query = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            ModifyExtendedPropertyByPropertyRet = Result.GetFailureResult();
            try
            {
                l_Query = "EXEC Web_ExtendedPropertyByProperty_Modify";
                PublicFunctions.FieldToParam(p_DocumentName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += " " + l_Param;
                PublicFunctions.FieldToParam(p_FieldName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_FieldOldValue, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_FieldValue, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_PropertyName, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_PropertyValue, ref l_Param, Declarations.FieldTypes.String);
                l_Query += ", " + l_Param;
                PublicFunctions.FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_Query += ", " + l_Param;
                Connection.GetDataSP(l_Query, ref l_Data);
                ModifyExtendedPropertyByPropertyRet.Populate(l_Data);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                l_Data.Dispose();
            }

            return ModifyExtendedPropertyByPropertyRet;
        }
    }
}