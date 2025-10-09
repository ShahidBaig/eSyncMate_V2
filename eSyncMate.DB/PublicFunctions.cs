using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using static eSyncMate.DB.Declarations;

namespace eSyncMate.DB
{
    public static class PublicFunctions
    {
        public static string DateFormat(DateTime dateValue, string format)
        {
            return dateValue.ToString(format);
        }

        public static string GetFileName(string p_FileName)
        {
            string GetFileNameRet = default;
            string l_FileExt;
            string l_FileName;
            int l_FileNameChar;

            GetFileNameRet = string.Empty;

            l_FileExt = string.Empty;
            l_FileName = string.Empty;
            l_FileNameChar = 0;

            if ((Strings.UCase(Strings.Right(p_FileName, 4)) ?? "") == (Strings.UCase(".txt") ?? "") | Strings.Right(p_FileName, 4) == ".INI" | (Strings.UCase(Strings.Right(p_FileName, 4)) ?? "") == (Strings.UCase(".csv") ?? ""))
            {
                l_FileExt = Strings.Right(p_FileName, 4);
                l_FileNameChar = Strings.Len(p_FileName) - 4;
                l_FileName = Strings.Left(p_FileName, l_FileNameChar);
            }
            else
            {
                l_FileName = p_FileName;
            }

            l_FileName = l_FileName + "" + DateFormat(DateAndTime.Now.Date, "yyMMdd") + DateFormat(DateAndTime.Now, "hhmm");
            GetFileNameRet = l_FileName + l_FileExt;
            return GetFileNameRet;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public static bool FieldToParam(object value, ref string l_Param, Declarations.FieldTypes type)
        {
            bool l_Process = false;
            try
            {
                string sV = null;
                l_Param = string.Empty;
                l_Process = false;
                if (value is null)
                {
                    l_Param = DBConnector.NullValue;
                    return true;
                }

                switch (type)
                {
                    case Declarations.FieldTypes.ByteArray:
                        {
                            l_Param = Conversions.ToString(value);
                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.Boolean:
                        {
                            l_Param = Convert.ToInt32(value).ToString();
                            if (string.Compare(l_Param, "-1") == 0)
                            {
                                l_Param = "1";
                            }

                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.Date:
                    case Declarations.FieldTypes.NullableDate:
                        {
                            var l_DateTime = new DateTime();
                            try
                            {
                                l_DateTime = Convert.ToDateTime(value);
                                sV = l_DateTime.ToString(Declarations.g_GlobalCulture).Trim();
                                if (sV == "01/01/0001 12:00:00 AM" || sV == "1/1/0001 12:00:00 AM")
                                {
                                    sV = sV.Replace("0001", "1900");
                                }

                                if (sV == "12:00:00 AM" || sV == "01/01/1900 12:00:00 AM" || sV == "1/1/1900 12:00:00 AM")
                                {
                                    if (type == Declarations.FieldTypes.NullableDate)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = "'" + sV + "'";
                                    }
                                }
                                else
                                {
                                    l_Param = "'" + l_DateTime.ToString(Declarations.g_GlobalCulture).Trim() + "'";
                                }
                            }
                            catch (Exception ex)
                            {
                                if (type == Declarations.FieldTypes.NullableDate)
                                {
                                    l_Param = DBConnector.NullValue;
                                }
                                else
                                {
                                    l_Param = "'" + sV + "'";
                                }
                            }

                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.ReportDate:
                    case Declarations.FieldTypes.NullableReportDate:
                        {
                            var l_DateTime = new DateTime();
                            try
                            {
                                l_DateTime = Convert.ToDateTime(value);
                                sV = l_DateTime.ToShortDateString().Trim();
                                if (sV == "01/01/0001 12:00:00 AM" || sV == "1/1/0001 12:00:00 AM")
                                {
                                    sV = sV.Replace("0001", "1900");
                                }

                                if (sV == "12:00:00 AM" || sV == "01/01/1900 12:00:00 AM" || sV == "1/1/1900 12:00:00 AM")
                                {
                                    if (type == Declarations.FieldTypes.NullableReportDate)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = "#" + sV + "#";
                                    }
                                }
                                else
                                {
                                    l_Param = "#" + l_DateTime.ToShortDateString().Trim() + "#";
                                }
                            }
                            catch (Exception ex)
                            {
                                if (type == Declarations.FieldTypes.NullableReportDate)
                                {
                                    l_Param = DBConnector.NullValue;
                                }
                                else
                                {
                                    l_Param = "#" + sV + "#";
                                }
                            }

                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.Number:
                    case Declarations.FieldTypes.NullableNumber:
                    case Declarations.FieldTypes.NullableByte:
                    case Declarations.FieldTypes.Byte:
                    case Declarations.FieldTypes.NullableShortNumber:
                    case Declarations.FieldTypes.ShortNumber:
                        {
                            try
                            {
                                if (DataConverter.ConvertToInteger(value) == 0)
                                {
                                    if (type == Declarations.FieldTypes.NullableNumber || type == Declarations.FieldTypes.NullableByte)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = Convert.ToString(value);
                                    }
                                }
                                else
                                {
                                    l_Param = Convert.ToString(value);
                                }
                            }
                            catch (Exception ex)
                            {
                                value = value.ToString().Replace("$", string.Empty);
                                if (DataConverter.ConvertToInteger(value) == 0)
                                {
                                    if (type == Declarations.FieldTypes.NullableNumber || type == Declarations.FieldTypes.NullableByte)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = Convert.ToString(value);
                                    }
                                }
                                else
                                {
                                    l_Param = Convert.ToString(value);
                                }
                            }

                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.String:
                    case Declarations.FieldTypes.NullableString:
                        {
                            sV = Convert.ToString(value);
                            if (string.IsNullOrEmpty(sV))
                            {
                                if (type == Declarations.FieldTypes.NullableString)
                                {
                                    l_Param = DBConnector.NullValue;
                                }
                                else
                                {
                                    // sV = sV.Replace("[", "")
                                    // sV = sV.Replace("]", "")
                                    l_Param = "'" + DoQuotes(sV) + "'";
                                }
                            }
                            else
                            {
                                // sV = sV.Replace("[", "")
                                // sV = sV.Replace("]", "")
                                l_Param = "'" + DoQuotes(sV) + "'";
                            }

                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.NumberFloat:
                    case Declarations.FieldTypes.NullableFloat:
                        {
                            try
                            {
                                if (Convert.ToDecimal(value) == 0m)
                                {
                                    if (type == Declarations.FieldTypes.NullableFloat)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = Convert.ToString(value);
                                    }
                                }
                                else
                                {
                                    l_Param = Convert.ToString(value);
                                }
                            }
                            catch (Exception ex)
                            {
                                value = value.ToString().Replace("$", string.Empty);
                                if (Convert.ToDecimal(value) == 0m)
                                {
                                    if (type == Declarations.FieldTypes.NullableFloat)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = Convert.ToString(value);
                                    }
                                }
                                else
                                {
                                    l_Param = Convert.ToString(value);
                                }
                            }

                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.Decimal:
                    case Declarations.FieldTypes.NullableDecimal:
                        {
                            try
                            {
                                if (DataConverter.ConvertToDecimal(value) == 0m)
                                {
                                    if (type == Declarations.FieldTypes.NullableDecimal)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = Convert.ToDecimal(value).ToString();
                                    }
                                }
                                else
                                {
                                    l_Param = Convert.ToDecimal(value).ToString();
                                }
                            }
                            catch (Exception ex)
                            {
                                value = value.ToString().Replace("$", string.Empty);
                                if (DataConverter.ConvertToDecimal(value) == 0m)
                                {
                                    if (type == Declarations.FieldTypes.NullableDecimal)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = Convert.ToDecimal(value).ToString();
                                    }
                                }
                                else
                                {
                                    l_Param = Convert.ToDecimal(value).ToString();
                                }
                            }

                            l_Process = true;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                // TODO: Implement Log
            }

            return l_Process;
        }

        public static string FieldToParam(object value, Declarations.FieldTypes type)
        {
            string l_Param = string.Empty;

            FieldToParam(value, ref l_Param, type);

            return l_Param;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public static bool FieldToParam(object value, ref object l_Param, Declarations.FieldTypes type)
        {
            bool l_Process = false;

            try
            {
                string sV = null;

                l_Param = string.Empty;
                l_Process = false;
                if (value is null)
                {
                    l_Param = DBConnector.NullValue;
                    return true;
                }

                switch (type)
                {
                    case Declarations.FieldTypes.ByteArray:
                        {
                            l_Param = Conversions.ToString(value);
                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.Boolean:
                        {
                            l_Param = Convert.ToInt32(value).ToString();
                            if (string.Compare(Convert.ToString(l_Param), "-1") == 0)
                            {
                                l_Param = "1";
                            }

                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.Date:
                    case Declarations.FieldTypes.NullableDate:
                        {
                            var l_DateTime = new DateTime();
                            try
                            {
                                l_DateTime = Convert.ToDateTime(value);
                                sV = l_DateTime.ToString(Declarations.g_GlobalCulture).Trim();
                                if (sV == "01/01/0001 12:00:00 AM" || sV == "1/1/0001 12:00:00 AM")
                                {
                                    sV = sV.Replace("0001", "1900");
                                }

                                if (sV == "12:00:00 AM" || sV == "01/01/1900 12:00:00 AM" || sV == "1/1/1900 12:00:00 AM")
                                {
                                    if (type == Declarations.FieldTypes.NullableDate)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = "'" + sV + "'";
                                    }
                                }
                                else
                                {
                                    l_Param = "'" + l_DateTime.ToString(Declarations.g_GlobalCulture).Trim() + "'";
                                }
                            }
                            catch (Exception ex)
                            {
                                if (type == Declarations.FieldTypes.NullableDate)
                                {
                                    l_Param = DBConnector.NullValue;
                                }
                                else
                                {
                                    l_Param = "'" + sV + "'";
                                }
                            }

                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.ReportDate:
                    case Declarations.FieldTypes.NullableReportDate:
                        {
                            var l_DateTime = new DateTime();
                            try
                            {
                                l_DateTime = Convert.ToDateTime(value);
                                sV = l_DateTime.ToShortDateString().Trim();
                                if (sV == "01/01/0001 12:00:00 AM" || sV == "1/1/0001 12:00:00 AM")
                                {
                                    sV = sV.Replace("0001", "1900");
                                }

                                if (sV == "12:00:00 AM" || sV == "01/01/1900 12:00:00 AM" || sV == "1/1/1900 12:00:00 AM")
                                {
                                    if (type == Declarations.FieldTypes.NullableReportDate)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = "#" + sV + "#";
                                    }
                                }
                                else
                                {
                                    l_Param = "#" + l_DateTime.ToShortDateString().Trim() + "#";
                                }
                            }
                            catch (Exception ex)
                            {
                                if (type == Declarations.FieldTypes.NullableReportDate)
                                {
                                    l_Param = DBConnector.NullValue;
                                }
                                else
                                {
                                    l_Param = "#" + sV + "#";
                                }
                            }

                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.Number:
                    case Declarations.FieldTypes.NullableNumber:
                    case Declarations.FieldTypes.NullableByte:
                    case Declarations.FieldTypes.Byte:
                    case Declarations.FieldTypes.NullableShortNumber:
                    case Declarations.FieldTypes.ShortNumber:
                        {
                            try
                            {
                                if (DataConverter.ConvertToInteger(value) == 0)
                                {
                                    if (type == Declarations.FieldTypes.NullableNumber || type == Declarations.FieldTypes.NullableByte)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = Convert.ToString(value);
                                    }
                                }
                                else
                                {
                                    l_Param = Convert.ToString(value);
                                }
                            }
                            catch (Exception ex)
                            {
                                value = value.ToString().Replace("$", string.Empty);
                                if (DataConverter.ConvertToInteger(value) == 0)
                                {
                                    if (type == Declarations.FieldTypes.NullableNumber || type == Declarations.FieldTypes.NullableByte)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = Convert.ToString(value);
                                    }
                                }
                                else
                                {
                                    l_Param = Convert.ToString(value);
                                }
                            }

                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.String:
                    case Declarations.FieldTypes.NullableString:
                        {
                            sV = Convert.ToString(value);
                            if (string.IsNullOrEmpty(sV))
                            {
                                if (type == Declarations.FieldTypes.NullableString)
                                {
                                    l_Param = DBConnector.NullValue;
                                }
                                else
                                {
                                    // sV = sV.Replace("[", "")
                                    // sV = sV.Replace("]", "")
                                    l_Param = "'" + DoQuotes(sV) + "'";
                                }
                            }
                            else
                            {
                                // sV = sV.Replace("[", "")
                                // sV = sV.Replace("]", "")
                                l_Param = "'" + DoQuotes(sV) + "'";
                            }

                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.NumberFloat:
                    case Declarations.FieldTypes.NullableFloat:
                        {
                            try
                            {
                                if (Convert.ToDecimal(value) == 0m)
                                {
                                    if (type == Declarations.FieldTypes.NullableFloat)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = Convert.ToString(value);
                                    }
                                }
                                else
                                {
                                    l_Param = Convert.ToString(value);
                                }
                            }
                            catch (Exception ex)
                            {
                                value = value.ToString().Replace("$", string.Empty);
                                if (Convert.ToDecimal(value) == 0m)
                                {
                                    if (type == Declarations.FieldTypes.NullableFloat)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = Convert.ToString(value);
                                    }
                                }
                                else
                                {
                                    l_Param = Convert.ToString(value);
                                }
                            }

                            l_Process = true;
                            break;
                        }

                    case Declarations.FieldTypes.Decimal:
                    case Declarations.FieldTypes.NullableDecimal:
                        {
                            try
                            {
                                if (DataConverter.ConvertToDecimal(value) == 0m)
                                {
                                    if (type == Declarations.FieldTypes.NullableDecimal)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = Convert.ToDecimal(value).ToString();
                                    }
                                }
                                else
                                {
                                    l_Param = Convert.ToDecimal(value).ToString();
                                }
                            }
                            catch (Exception ex)
                            {
                                value = value.ToString().Replace("$", string.Empty);
                                if (DataConverter.ConvertToDecimal(value) == 0m)
                                {
                                    if (type == Declarations.FieldTypes.NullableDecimal)
                                    {
                                        l_Param = DBConnector.NullValue;
                                    }
                                    else
                                    {
                                        l_Param = Convert.ToDecimal(value).ToString();
                                    }
                                }
                                else
                                {
                                    l_Param = Convert.ToDecimal(value).ToString();
                                }
                            }

                            l_Process = true;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                // TODO: Implement Log
            }

            return l_Process;
        }

        public static string DoQuotes(string value)
        {
            string functionReturnValue = string.Empty;
            int iLast = 0;
            string sPart = string.Empty;
            functionReturnValue = string.Empty;
            if (value.Length == 0)
            {
                return functionReturnValue;
            }

            value = value.ToString().Replace("'", "''");
            functionReturnValue = value.Trim();
            return functionReturnValue;
        }

        /// <summary>
        /// TODO: Update summary.
        /// </summary>
        public static object ConvertNull(object value, object defaultValue)
        {
            if (Information.IsDBNull(value))
            {
                return defaultValue;
            }

            return value;
        }

        public static string ConvertNullAsString(object value, string defaultValue)
        {
            if (Information.IsDBNull(value))
            {
                return defaultValue;
            }

            return (string)value;
        }

        public static int ConvertNullAsInteger(object value, int defaultValue)
        {
            if (Information.IsDBNull(value))
            {
                return defaultValue;
            }

            return Convert.ToInt32(value);
        }

        public static bool ConvertNullAsBoolean(object value, bool defaultValue)
        {
            if (Information.IsDBNull(value))
            {
                return defaultValue;
            }

            return Conversions.ToBoolean(value);
        }

        public static double ConvertNullAsDouble(object value, double defaultValue)
        {
            if (Information.IsDBNull(value))
            {
                return defaultValue;
            }

            return Convert.ToDouble(value);
        }

        public static DateTime ConvertNullAsDateTime(object value, DateTime defaultValue)
        {
            if (Information.IsDBNull(value))
            {
                return defaultValue;
            }

            return (DateTime)value;
        }

        public static DataTable GetCSVData(string p_FileName, string p_POColumnName, ref Dictionary<string, string> p_ExcelFileDictionary)
        {
            //string CSV_ConnString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Path.GetDirectoryName(p_FileName) + ";Extended Properties=Text;";
            //var l_Conn = new OleDbConnection(CSV_ConnString);
            var l_dt = new DataTable();
            //try
            //{
            //    l_Conn.Open();
            //    var l_Command = new OleDbCommand("SELECT * FROM [" + Path.GetFileName(p_FileName) + "] ORDER BY [" + p_POColumnName + "]", l_Conn);
            //    var l_Adapter = new OleDbDataAdapter();
            //    l_Adapter.SelectCommand = l_Command;
            //    CreateSchemaFile(Path.GetDirectoryName(p_FileName), Path.GetFileName(p_FileName), ref p_ExcelFileDictionary);
            //    l_Adapter.Fill(l_dt);
            //    l_Adapter.Dispose();
            return l_dt;
            //}
            //catch
            //{
            //    throw;
            //}
            //finally
            //{
            //    l_Conn.Close();
            //}
        }

        public static string PrepareRowFilter(string p_Value, DataTable p_Data)
        {
            string l_Criteria = string.Empty;
            string l_Param = string.Empty;
            var l_Type = Declarations.FieldTypes.Boolean;
            string l_Filter = string.Empty;
            FieldToParam("*" + p_Value + "*", ref l_Param, Declarations.FieldTypes.String);
            foreach (DataColumn l_Column in p_Data.Columns)
            {
                l_Type = GetColumnFieldType(l_Column);
                l_Filter = string.Empty;
                if (l_Type == Declarations.FieldTypes.String)
                {
                    l_Filter = l_Column.ColumnName + " LIKE " + l_Param;
                }
                // ElseIf l_Type = FieldTypes.Date Then
                // Try
                // Dim l_Date As Date = Date.Parse(p_Value)
                // l_Filter = l_Column.ColumnName & " = #" & l_Date & "#"
                // Catch ex As Exception
                // End Try
                else
                {
                    // Call FieldToParam(p_Value, l_Param, l_Type)
                    l_Filter = "CONVERT(" + l_Column.ColumnName + ", 'System.String') LIKE " + l_Param;
                }

                if (!string.IsNullOrEmpty(l_Filter))
                {
                    if (!string.IsNullOrEmpty(l_Criteria))
                    {
                        l_Criteria += " OR ";
                    }

                    l_Criteria += l_Filter;
                }
            }

            return l_Criteria;
        }

        public static Declarations.FieldTypes GetColumnFieldType(DataColumn p_Column)
        {
            if (p_Column.DataType == Type.GetType("System.String"))
            {
                return Declarations.FieldTypes.String;
            }
            else if (p_Column.DataType == Type.GetType("System.Boolean"))
            {
                return Declarations.FieldTypes.Boolean;
            }
            else if (p_Column.DataType == Type.GetType("System.DateTime"))
            {
                return Declarations.FieldTypes.Date;
            }

            return Declarations.FieldTypes.Number;
        }

        public static int ActiveClientSKU()
        {
            int ActiveClientSKURet = default;
            ActiveClientSKURet = 10;
            return ActiveClientSKURet;
        }

        public static DataTable GetCustomerEmailInfo(string p_ConnectionString, string p_criteria, string p_FieldName = "")
        {
            var l_dt = new DataTable();
            var l_Common = new Common();
            string l_SQL = string.Empty;
            l_Common.UseConnection(p_ConnectionString);
            if ((p_FieldName ?? "") == (string.Empty ?? ""))
            {
                p_FieldName = "*";
            }

            if ((p_criteria ?? "") != (string.Empty ?? ""))
            {
                p_criteria = " WHERE " + p_criteria;
            }

            if (Information.IsNothing(l_dt))
            {
                l_dt = new DataTable();
            }

            l_SQL = "SELECT " + p_FieldName + " FROM App_CustomerEmailInfo " + p_criteria;
            l_Common.GetList(l_SQL, ref l_dt);
            return l_dt;
        }

        public static string Encrypt(string strInput)
        {
            string strKey;
            long iCount;
            var lngPtr = default(long);
            strKey = "v";
            var loopTo = (long)Strings.Len(strInput);
            for (iCount = 1L; iCount <= loopTo; iCount++)
            {
                if ((Strings.Mid(strInput, (int)iCount, 1) ?? "") != (strKey ?? ""))
                {
                    var midTmp = Conversions.ToString((char)(Strings.Asc(Strings.Mid(strInput, (int)iCount, 1)) ^ Strings.Asc(Strings.Mid(strKey, (int)(lngPtr + 1L), 1))));
                    StringType.MidStmtStr(ref strInput, (int)iCount, 1, midTmp);
                    lngPtr = (lngPtr + 1L) % Strings.Len(strKey);
                }
            }

            return strInput;
        }

        public static string GetTitleCase(string title)
        {
            title = Regex.Replace(title, "([A-Z])+", " $0", RegexOptions.Compiled).TrimStart();
            return title;
        }

        public static bool CreateSchemaFile(string filePath, string p_FileName, ref Dictionary<string, string> p_ExcelFileDictionary)
        {
            int index = 1;
            using (var filestr = new FileStream(filePath + Convert.ToString(@"\schema.ini"), FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(filestr))
                {
                    writer.WriteLine("[" + p_FileName + "]");
                    writer.WriteLine("ColNameHeader=True");
                    writer.WriteLine("Format=CSVDelimited");
                    foreach (var column in p_ExcelFileDictionary)
                    {
                        writer.WriteLine("Col" + index + "=" + column.Key + " " + column.Value);
                        index = index + 1;
                    }

                    writer.Close();
                    writer.Dispose();
                }

                filestr.Close();
                filestr.Dispose();
            }

            return true;
        }

        /// <summary>
        /// Description here.
        /// </summary>
        /// <returns> returns value. </returns>
        public static List<object> ConvertChartDataTable(DataTable data, string xaxisColumn, string yaxisColumn)
        {
            var rows = new List<object>();
            var xFunction = Declarations.DataFunctions.None;
            var yFunction = Declarations.DataGroupFunctions.None;
            string xaxisColumnName = xaxisColumn;
            string yaxisColumnName = yaxisColumn;
            if (xaxisColumn.StartsWith("MONTH("))
            {
                xFunction = Declarations.DataFunctions.Month;
                xaxisColumnName = xaxisColumn.Substring(6, xaxisColumn.Length - 7);
            }
            else if (xaxisColumn.StartsWith("MONTHYEAR("))
            {
                xFunction = Declarations.DataFunctions.MonthYear;
                xaxisColumnName = xaxisColumn.Substring(10, xaxisColumn.Length - 11);
            }
            else if (xaxisColumn.StartsWith("YEAR("))
            {
                xFunction = Declarations.DataFunctions.Year;
                xaxisColumnName = xaxisColumn.Substring(5, xaxisColumn.Length - 6);
            }
            else if (xaxisColumn.StartsWith("DAY("))
            {
                xFunction = Declarations.DataFunctions.Day;
                xaxisColumnName = xaxisColumn.Substring(4, xaxisColumn.Length - 5);
            }

            if (yaxisColumn.StartsWith("COUNT("))
            {
                yFunction = Declarations.DataGroupFunctions.COUNT;
                yaxisColumnName = yaxisColumn.Substring(6, yaxisColumn.Length - 7);
            }
            else if (yaxisColumn.StartsWith("MIN("))
            {
                yFunction = Declarations.DataGroupFunctions.MIN;
                yaxisColumnName = yaxisColumn.Substring(4, yaxisColumn.Length - 5);
            }
            else if (yaxisColumn.StartsWith("MAX("))
            {
                yFunction = Declarations.DataGroupFunctions.MAX;
                yaxisColumnName = yaxisColumn.Substring(4, yaxisColumn.Length - 5);
            }
            else if (yaxisColumn.StartsWith("AVG("))
            {
                yFunction = Declarations.DataGroupFunctions.AVG;
                yaxisColumnName = yaxisColumn.Substring(4, yaxisColumn.Length - 5);
            }
            else if (yaxisColumn.StartsWith("SUM("))
            {
                yFunction = Declarations.DataGroupFunctions.MIN;
                yaxisColumnName = yaxisColumn.Substring(4, yaxisColumn.Length - 5);
            }

            if (!data.Columns.Contains(xaxisColumnName) || !data.Columns.Contains(yaxisColumnName))
            {
                return rows;
            }

            if (yFunction == Declarations.DataGroupFunctions.None)
            {
                foreach (DataRow row in data.Rows)
                    rows.Add(new
                    {
                        x = GetFunctionData(row[xaxisColumnName], xFunction),
                        y = row[yaxisColumnName]
                    });
            }
            else
            {
                //Faisal
                var grouped = from table in data.AsEnumerable()
                              group table by GetFunctionData(table[xaxisColumnName], xFunction) into groupby
                              select new { xAxisColumn1 = groupby.Key, groupby = groupby.ToArray() };

                foreach (var key in grouped)
                    rows.Add(new
                    {
                        x = key.xAxisColumn1,
                        y = GetGroupFunctionData((DataRow[])key.groupby, yFunction, yaxisColumnName)
                    });
            }

            return rows;
        }

        private static object GetGroupFunctionData(DataRow[] columnValues, Declarations.DataGroupFunctions function, string yAxisColumnName)
        {
            object result = null;
            try
            {
                switch (function)
                {
                    case Declarations.DataGroupFunctions.COUNT:
                        {
                            result = columnValues.Count();
                            break;
                        }

                    case Declarations.DataGroupFunctions.MIN:
                        {
                            result = columnValues.Min(r => (DataRow)r[yAxisColumnName]);
                            break;
                        }

                    case Declarations.DataGroupFunctions.MAX:
                        {
                            result = columnValues.Max(r => (DataRow)r[yAxisColumnName]);
                            break;
                        }

                    case Declarations.DataGroupFunctions.AVG:
                        {
                            result = columnValues.Average(r => Convert.ToDouble(r[yAxisColumnName]));
                            break;
                        }

                    case Declarations.DataGroupFunctions.SUM:
                        {
                            result = columnValues.Sum(r => Convert.ToDouble(r[yAxisColumnName]));
                            break;
                        }

                    case Declarations.DataGroupFunctions.None:
                        {
                            result = 0;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                result = 0;
            }

            return result;
        }

        private static object GetFunctionData(object cellData, Declarations.DataFunctions function)
        {
            object result = null;
            try
            {
                var date = Convert.ToDateTime(cellData);
                switch (function)
                {
                    case Declarations.DataFunctions.Month:
                        {
                            result = date.Month;
                            break;
                        }

                    case Declarations.DataFunctions.MonthYear:
                        {
                            result = date.Month.ToString() + "/" + date.Year.ToString();
                            break;
                        }

                    case Declarations.DataFunctions.Year:
                        {
                            result = date.Year;
                            break;
                        }

                    case Declarations.DataFunctions.Day:
                        {
                            result = date.Day;
                            break;
                        }

                    case Declarations.DataFunctions.None:
                        {
                            result = cellData;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                result = cellData;
            }

            return result;
        }

        public static string GetMessageDescription(int Code)
        {
            switch (Code)
            {
                case (int)Declarations.ErrorCodes.NoSession:
                    {
                        return "Session not found or Session timeout. Please login again";
                    }

                case (int)Declarations.ErrorCodes.NotAuthorize:
                    {
                        return "You are not authorized to perform this action. Please login again";
                    }

                case (int)Declarations.ErrorCodes.NoSearchData:
                    {
                        return "No search data available for the document.";
                    }

                case (int)Declarations.ErrorCodes.ViewDocument:
                    {
                        return "Unable to view the document.";
                    }

                case (int)Declarations.ErrorCodes.NoReportData:
                    {
                        return "No report info available.";
                    }

                case (int)Declarations.ErrorCodes.VoidDocument:
                    {
                        return "Unable to void the document.";
                    }

                case (int)Declarations.ErrorCodes.NoRecord:
                    {
                        return "No record found.";
                    }

                case (int)Declarations.ErrorCodes.OperationFailed:
                    {
                        return "Operation failed.";
                    }

                case (int)Declarations.ErrorCodes.AddDetail:
                    {
                        return "Unable to add the detail.";
                    }

                case (int)Declarations.ErrorCodes.EditDetail:
                    {
                        return "Unable to edit the detail.";
                    }

                case (int)Declarations.ErrorCodes.SaveDocument:
                    {
                        return "Unable to save the document.";
                    }

                case (int)Declarations.ErrorCodes.ModifyDocument:
                    {
                        return "Unable to modify the document.";
                    }

                case (int)Declarations.ErrorCodes.DuplicateDocument:
                    {
                        return "Document value already exist.";
                    }

                case (int)Declarations.ErrorCodes.CancelDocument:
                    {
                        return "Unable to cancel the document.";
                    }

                case (int)Declarations.ErrorCodes.CloseDocument:
                    {
                        return "Unable to close the document.";
                    }

                case (int)Declarations.ErrorCodes.InvalidSKU:
                    {
                        return "SKU is invalid.";
                    }

                case (int)Declarations.ErrorCodes.CreateDocument:
                    {
                        return "Unable to open new/create document.";
                    }

                case (int)Declarations.ErrorCodes.EditDocument:
                    {
                        return "Unable to open edit document.";
                    }

                case (int)Declarations.ErrorCodes.ImportDocument:
                    {
                        return "Unable to import document(s).";
                    }

                case (int)Declarations.ErrorCodes.GenericError:
                    {
                        return "Operation failed.";
                    }

                case var @case when @case == (int)Declarations.ErrorCodes.GenericError:
                    {
                        return "Operation failed.";
                    }

                case (int)Declarations.ErrorCodes.DeleteDocument:
                    {
                        return "Delete document failed, Please try again.";
                    }

                case (int)Declarations.SuccessCodes.CancelDocumentLine:
                    {
                        return "Document line cancellation failed.";
                    }

                case (int)Declarations.SuccessCodes.OperationCompleted:
                    {
                        return "Operation completed successfully.";
                    }

                case (int)Declarations.SuccessCodes.LoadDocument:
                    {
                        return "Loaded document list successfully.";
                    }

                case (int)Declarations.SuccessCodes.SearchDocument:
                    {
                        return "Searched document list successfully.";
                    }

                case (int)Declarations.SuccessCodes.ViewDocument:
                    {
                        return "View document successful.";
                    }

                case (int)Declarations.SuccessCodes.VoidDocument:
                    {
                        return "Document voided successfully.";
                    }

                case (int)Declarations.SuccessCodes.ReceiveCompleteDocument:
                    {
                        return "Document received successfully.";
                    }

                case (int)Declarations.SuccessCodes.AddDetail:
                    {
                        return "The detail line added successfully.";
                    }

                case (int)Declarations.SuccessCodes.EditDetail:
                    {
                        return "The detail line edited successfully.";
                    }

                case (int)Declarations.SuccessCodes.SaveDocument:
                    {
                        return "The document is saved successfully.";
                    }

                case (int)Declarations.SuccessCodes.DeleteDocument:
                    {
                        return "The document is Deleted successfully.";
                    }

                case (int)Declarations.SuccessCodes.ModifyDocument:
                    {
                        return "The document is modified successfully.";
                    }

                case (int)Declarations.SuccessCodes.CloseDocument:
                    {
                        return "The document has been closed successfully.";
                    }

                case (int)Declarations.SuccessCodes.CreateDocument:
                    {
                        return "Create document is loaded successfully.";
                    }

                case (int)Declarations.SuccessCodes.EditDocument:
                    {
                        return "Edit document is loaded successfully.";
                    }

                case (int)Declarations.SuccessCodes.ImportDocument:
                    {
                        return "Process completed successfully";
                    }

                case (int)Declarations.SuccessCodes.ValidDocument:
                    {
                        return "Valid documents";
                    }

                case (int)Declarations.SuccessCodes.GenericSuccess:
                    {
                        return "Operation completed successfully.";
                    }

                case var case1 when case1 == (int)Declarations.SuccessCodes.CancelDocumentLine:
                    {
                        return "Document line canceled successfully.";
                    }

                case (int)Declarations.SuccessCodes.FileUpload:
                    {
                        return "File has been uploaded successfully.";
                    }

                case (int)Declarations.SuccessCodes.ValidAPIKey:
                    {
                        return "API key authorized successfully.";
                    }

                case (int)Declarations.ExceptionCodes.ExceptionOccured:
                    {
                        return "Operation failed, please try again.";
                    }

                case (int)Declarations.ExceptionCodes.LoadDocument:
                    {
                        return "Loading documents failed, please try again.";
                    }

                case (int)Declarations.ExceptionCodes.SearchDocument:
                    {
                        return "Searching documents failed, please try again.";
                    }

                case (int)Declarations.ExceptionCodes.ViewDocument:
                    {
                        return "Viewing document failed, please try again.";
                    }

                case (int)Declarations.ExceptionCodes.ReportData:
                    {
                        return "Report Viewing failed, please try again.";
                    }

                case (int)Declarations.ExceptionCodes.VoidDocument:
                    {
                        return "Voiding document failed, please try again.";
                    }

                case (int)Declarations.ExceptionCodes.AddDetail:
                    {
                        return "Adding detail failed, please try again.";
                    }

                case (int)Declarations.ExceptionCodes.EditDetail:
                    {
                        return "Editing detail failed, please try again.";
                    }

                case (int)Declarations.ExceptionCodes.SaveDocument:
                    {
                        return "Saving document failed, please try again.";
                    }

                case (int)Declarations.ExceptionCodes.ModifyDocument:
                    {
                        return "Modifying document failed, please try again.";
                    }

                case (int)Declarations.ExceptionCodes.DeleteDocumentAssociation:
                    {
                        return "This document has some association in Database! It can't be Deleted";
                    }

                case (int)Declarations.ExceptionCodes.CloseDocument:
                    {
                        return "Cancelling document failed, please try again.";
                    }

                case (int)Declarations.ExceptionCodes.GenericException:
                    {
                        return "Operation failed, please try again.";
                    }

                case (int)Declarations.ExceptionCodes.FileUpload:
                    {
                        return "File Upload failed, Please try again.";
                    }

                case (int)Declarations.ExceptionCodes.APIKeyValidation:
                    {
                        return "Unable to authorize API Key.";
                    }

                case (int)Declarations.ErrorCodes.PreSaveError:
                    {
                        return "Operation failed, data is not valid.";
                    }

                case (int)Declarations.ErrorCodes.FileUpload:
                    {
                        return "File can't be imported, Either this file is invalid or data is incorrect.";
                    }

                case (int)Declarations.ErrorCodes.SessionImport:
                    {
                        return "Session import failed.";
                    }

                case (int)Declarations.ExceptionCodes.SessionImport:
                    {
                        return "Session import failed.";
                    }

                case (int)Declarations.ExceptionCodes.CreateDocument:
                    {
                        return "Creating document failed, Please try again.";
                    }

                case (int)Declarations.ExceptionCodes.ImportDocument:
                    {
                        return "Failed to import document(s).";
                    }

                case (int)Declarations.ExceptionCodes.EditDocument:
                    {
                        return "Editing document failed, Please try again.";
                    }

                case (int)Declarations.ExceptionCodes.DeleteDocument:
                    {
                        return "Delete document failed, Please try again.";
                    }

                case (int)Declarations.ErrorCodes.CheckPriceCategory:
                    {
                        return "Price Category cannot be blocked because it is assigned to a Customer.";
                    }

                case (int)Declarations.ErrorCodes.DeleteCustomerAddress:
                    {
                        return "This Address have some references to other documents and can not be deleted.";
                    }

                case (int)Declarations.ErrorCodes.CheckDefaultAddress:
                    {
                        return "Default Address cannot be deleted.";
                    }

                case (int)Declarations.ErrorCodes.DuplicateCarrierCode:
                    {
                        return "SCACCodeNo already exists.";
                    }

                case (int)Declarations.ErrorCodes.DesignIDLength:
                    {
                        return "Design ID is required and should have 5 characters.";
                    }

                case (int)Declarations.ErrorCodes.InvalidAPIKey:
                    {
                        return "Invalid API Key.";
                    }

                case (int)Declarations.ErrorCodes.NoImportFound:
                    {
                        return "There is no import setup configured.";
                    }

                case (int)Declarations.ErrorCodes.ImportCompletedWithError:
                    {
                        return "Import process completed with error, please check erroneous lines.";
                    }

                case (int)Declarations.ErrorCodes.ImportLengthExceedError:
                    {
                        return "Import file validation failed, please check erroneous lines.";
                    }

                case (int)Declarations.ErrorCodes.ReportManagerRawDataFileSizeExceeded:
                    {
                        return "The Raw File Size exceeded, please contact administrator for more information.";
                    }

                case (int)Declarations.ErrorCodes.ItemSpecialIDMismatchCustomerID:
                    {
                        return "The provided Item's Special ID does not belong to selected Customer.";
                    }

                default:
                    {
                        return string.Empty;
                    }
            }
        }

        public static bool HasFeauture(string p_criteria)
        {
            bool HasFeautureRet = default;
            var l_dt = new DataTable();
            var l_Common = new Common();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            string l_criteria = string.Empty;
            l_Common.UseConnection(Declarations.g_ConnectionString);
            if ((p_criteria ?? "") != (string.Empty ?? ""))
            {
                FieldToParam(p_criteria, ref l_Param, Declarations.FieldTypes.String);
                l_criteria = " WHERE Tag = " + l_Param;
            }

            if (Information.IsNothing(l_dt))
            {
                l_dt = new DataTable();
            }

            l_SQL = "SELECT Tag_Value from ApplicationSettings " + l_criteria;
            l_Common.GetList(l_SQL, ref l_dt);
            if (l_dt.Rows.Count > 0)
            {
                HasFeautureRet = Conversions.ToBoolean(Interaction.IIf(Conversions.ToBoolean(Operators.ConditionalCompareObjectEqual(ConvertNull(l_dt.Rows[0]["Tag_Value"], string.Empty), "Y", false)), true, false));
            }

            return HasFeautureRet;
        }

        public static Result LockDocument(string p_ConnectionString, string p_TableName, string p_KeyColumn, string p_DocNo, string p_DocType, int p_UserNo)
        {
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            var l_Result = new Result();
            var l_Common = new Common();
            l_Common.UseConnection(p_ConnectionString);
            l_SQL = "EXEC sp_App_LockDocument ";
            FieldToParam(p_TableName, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += l_Param + ",";
            FieldToParam(p_KeyColumn, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += l_Param + ",";
            FieldToParam(p_DocNo, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += l_Param + ",";
            FieldToParam(p_DocType, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += l_Param + ",";
            FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
            l_SQL += l_Param;
            l_Common.GetListSP(l_SQL, ref l_Data);
            l_Result.Populate(l_Data);
            l_Data.Dispose();
            return l_Result;
        }

        public static Result UnLockDocument(string p_ConnectionString, string p_TableName, string p_KeyColumn, string p_DocNo, string p_DocType, int p_UserNo)
        {
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            var l_Result = new Result();
            var l_Common = new Common();
            l_Common.UseConnection(p_ConnectionString);
            l_SQL = "EXEC sp_App_UnLockDocument ";
            FieldToParam(p_TableName, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += l_Param + ",";
            FieldToParam(p_KeyColumn, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += l_Param + ",";
            FieldToParam(p_DocNo, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += l_Param + ",";
            FieldToParam(p_DocType, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += l_Param + ",";
            FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
            l_SQL += l_Param;
            l_Common.GetListSP(l_SQL, ref l_Data);
            l_Result.Populate(l_Data);
            l_Data.Dispose();
            return l_Result;
        }

        public static bool CopyTo(object p_Source, object p_Destination)
        {
            var l_ModelProperties = new List<PropertyInfo>(p_Source.GetType().GetProperties());
            var l_Properties = new List<PropertyInfo>(p_Destination.GetType().GetProperties());
            foreach (PropertyInfo l_Property in l_ModelProperties)
            {
                foreach (PropertyInfo l_ObjProperty in l_Properties)
                {
                    if ((l_Property.Name ?? "") == (l_ObjProperty.Name ?? ""))
                    {
                        try
                        {
                            l_ObjProperty.SetValue(p_Destination, l_Property.GetValue(p_Source));
                        }
                        catch (Exception ex)
                        {
                        }

                        break;
                    }
                }
            }

            l_ModelProperties = null;
            l_Properties = null;
            return true;
        }

        public static DataTable EDI846LogData(string p_ItemID, string p_CustomerID)
        {
            var l_Common = new Common();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            string l_criteria = string.Empty;
            var l_dt = new DataTable();
            l_Common.UseConnection(Declarations.g_ConnectionString);
            l_SQL = "SELECT * FROM VW_EDI_846_FileDataLog";
            FieldToParam(p_ItemID, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += " WHERE ItemID = " + l_Param;
            if (!string.IsNullOrEmpty(p_CustomerID))
            {
                FieldToParam(p_CustomerID, ref l_Param, Declarations.FieldTypes.String);
                l_SQL += " And CustomerID = " + l_Param;
            }

            l_Common.GetList(l_SQL, ref l_dt);
            return l_dt;
        }

        public static DataTable PopulateTrafficAnalyzerPanel(string p_CustomerPO)
        {
            var l_Common = new Common();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            var l_dt = new DataTable();
            l_Common.UseConnection(Declarations.g_ConnectionString);
            l_SQL = "SELECT * FROM VW_EDITraffic ";
            FieldToParam(p_CustomerPO, ref l_Param, Declarations.FieldTypes.String);
            l_SQL += "WHERE CustomerPO = " + l_Param;
            l_Common.GetList(l_SQL, ref l_dt);
            return l_dt;
        }

        public static object PrepareSQLInClauseValues(string p_CommaSeparatedString, bool p_IsFieldInteger)
        {
            var l_ValueList = new List<string>();
            string l_FlatString;
            string l_Param = string.Empty;
            if (string.IsNullOrWhiteSpace(p_CommaSeparatedString))
            {
                return p_CommaSeparatedString;
            }

            if (!p_CommaSeparatedString.Contains(Conversions.ToString(',')))
            {
                return p_CommaSeparatedString;
            }

            p_CommaSeparatedString = p_CommaSeparatedString.Replace("'", "");
            foreach (var l_Str in p_CommaSeparatedString.Split(new char[] { ',' }))
            {
                if (p_IsFieldInteger)
                {
                    FieldToParam(l_Str, ref l_Param, Declarations.FieldTypes.Number);
                }
                else
                {
                    FieldToParam(l_Str, ref l_Param, Declarations.FieldTypes.String);
                }

                l_ValueList.Add(l_Param);
            }

            l_FlatString = string.Join(",", l_ValueList);
            return l_FlatString;
        }

        public static DataTable GetEmailMessageSetupData(string p_EmailDocType)
        {
            var l_Common = new Common();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            var l_Data = new DataTable();
            try
            {
                l_Common.UseConnection(Declarations.g_ConnectionString);
                l_SQL = "SELECT POS,DocType,FromEmail,EmailSource,ServerName,PortNo FROM [VW_EmailMessageSetupData] WHERE DocType = ";
                FieldToParam(p_EmailDocType, ref l_Param, Declarations.FieldTypes.String);
                l_SQL += l_Param;
                l_Common.GetList(l_SQL, ref l_Data);
                return l_Data;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string TrimNonMSSQLQueryChars(string p_NonSQLChars)
        {
            string l_SQL = p_NonSQLChars;
            string l_ISNULLPattern = $@"ISNULL\(.*?\)";
            MatchCollection l_Matches;
            string[] l_Parts;
            string l_ColumnName;
            string l_TranslatedISNULL;
            if (string.IsNullOrWhiteSpace(l_SQL))
            {
                return l_SQL;
            }

            l_SQL = l_SQL.Replace("{", string.Empty);
            l_SQL = l_SQL.Replace("}", string.Empty);
            l_SQL = l_SQL.Replace("[", "(");
            l_SQL = l_SQL.Replace("]", ")");
            l_SQL = l_SQL.Replace("*", "%");
            l_SQL = l_SQL.Replace("#", "'");
            l_Matches = Regex.Matches(l_SQL, l_ISNULLPattern);
            foreach (Match l_Match in l_Matches)
            {
                l_Parts = l_Match.Value.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                if (l_Parts.Length >= 2)
                {
                    l_ColumnName = l_Parts[1].Trim();
                    l_TranslatedISNULL = $"{l_ColumnName} IS NULL";
                    l_SQL = l_SQL.Replace(l_Match.Value, l_TranslatedISNULL);
                }
            }

            return l_SQL;
        }

        public static DataTable GetDefaultEmailEntity(int p_UserNo, string p_Entity, string p_RecordId)
        {
            Common l_Common = new Common();
            string l_SQL = string.Empty;
            string l_Param = string.Empty;
            DataTable l_Data = new DataTable();

            try
            {
                l_Common.UseConnection(Declarations.g_ConnectionString);

                l_SQL = "EXEC Web_GetDefaultEmail ";

                FieldToParam(p_UserNo, ref l_Param, Declarations.FieldTypes.Number);
                l_SQL += l_Param + ",";

                FieldToParam(p_Entity, ref l_Param, Declarations.FieldTypes.String);
                l_SQL += l_Param + ",";

                FieldToParam(p_RecordId, ref l_Param, Declarations.FieldTypes.String);
                l_SQL += l_Param;

                l_Common.GetList(l_SQL, ref l_Data);

                return l_Data;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// SPARS.BizLayer - WriteDataTable
        /// </summary>        
        public static void WriteDataTable(DataTable p_SourceTable, TextWriter p_TextWriter, bool p_IncludeHeaders)
        {
            IEnumerable<string> l_TextLines = null;
            IEnumerable<string> l_HeaderLabels = null;

            if (p_IncludeHeaders)
            {
                l_HeaderLabels = p_SourceTable.Columns.OfType<DataColumn>().Select(column => QuoteValue(column.ColumnName));

                p_TextWriter.WriteLine(string.Join(",", l_HeaderLabels));
            }

            foreach (DataRow l_DataRow in p_SourceTable.Rows)
            {
                l_TextLines = l_DataRow.ItemArray.Select(obj => QuoteValue(obj?.ToString() ?? string.Empty));

                p_TextWriter.WriteLine(string.Join(",", l_TextLines));
            }

            p_TextWriter.Flush();
        }

        /// <summary>
        /// SPARS.BizLayer - QuoteValue
        /// </summary>        
        public static string QuoteValue(string p_TextValue)
        {
            return string.Concat("\"", p_TextValue.Replace("\"", "\"\""), "\"");
        }

        public static string GetTitleCase(string title, bool p_UseHash = true)
        {
            if (title.Contains(" "))
            {
                title = title;
            }
            else
            {
                title = Regex.Replace(title, "([A-Z])+", " $0", RegexOptions.Compiled).TrimStart();
                title = title.Replace("_", string.Empty);
            }

            if (p_UseHash)
            {
                if (title.EndsWith(" No"))
                {
                    title = title.Replace(" No", " #");
                }
                else if (title.EndsWith("No"))
                {
                    title = title.Replace("No", " #");
                }
            }

            return Declarations.CultureTextInfo.ToTitleCase(title);
        }

        public static Declarations.FieldTypes GetFieldType(int p_DBType)
        {
            if (p_DBType == 135)
            {
                return DB.Declarations.FieldTypes.Date;
            }
            else if (p_DBType == 3 || p_DBType == 2)
            {
                return DB.Declarations.FieldTypes.Number;
            }
            else if (p_DBType == 200 || p_DBType == 203 || p_DBType == 129)
            {
                return DB.Declarations.FieldTypes.String;
            }
            else if (p_DBType == 6 || p_DBType == 4)
            {
                return DB.Declarations.FieldTypes.NumberFloat;
            }
            else if (p_DBType == 11)
            {
                return DB.Declarations.FieldTypes.Boolean;
            }
            else if (p_DBType == 201)
            {
                return DB.Declarations.FieldTypes.Combo;
            }
            else if (p_DBType == 202)
            {
                return DB.Declarations.FieldTypes.MultiSelect;
            }

            return DB.Declarations.FieldTypes.Number;
        }

        public static Declarations.FieldTypes GetFieldTypeString(string p_DBType)
        {
            p_DBType = p_DBType.ToLower();

            if (p_DBType == "datetime")
            {
                return DB.Declarations.FieldTypes.Date;
            }
            else if (p_DBType == "int" || p_DBType == "smallint" || p_DBType == "tinyint")
            {
                return DB.Declarations.FieldTypes.Number;
            }
            else if (p_DBType == "varchar" || p_DBType == "char")
            {
                return DB.Declarations.FieldTypes.String;
            }
            else if (p_DBType == "real" || p_DBType == "float" || p_DBType == "money")
            {
                return DB.Declarations.FieldTypes.NumberFloat;
            }
            else if (p_DBType == "bit")
            {
                return DB.Declarations.FieldTypes.Boolean;
            }

            return DB.Declarations.FieldTypes.Number;
        }

        public static int GetFieldTypeInt(string p_DBType)
        {
            int GetFieldTypeIntRet = default;

            p_DBType = p_DBType.ToLower();

            if (p_DBType == "datetime")
            {
                return 135;
            }
            else if (p_DBType == "int" || p_DBType == "smallint" || p_DBType == "tinyint")
            {
                return 3;
            }
            else if (p_DBType == "varchar" || p_DBType == "char")
            {
                return 200;
            }
            else if (p_DBType == "real" || p_DBType == "float" || p_DBType == "money")
            {
                return 6;
            }
            else if (p_DBType == "bit")
            {
                return 11;
            }

            int.TryParse(p_DBType, out GetFieldTypeIntRet);

            return GetFieldTypeIntRet;
        }

        public static string GetColumnTypeString(Type p_DBType)
        {
            string l_DBType = p_DBType.ToString().ToLower().Replace("system.", string.Empty);
            if (l_DBType == "datetime")
            {
                return "date";
            }
            else if (l_DBType == "int32" || l_DBType == "int16")
            {
                return "int";
            }
            else if (l_DBType == "string")
            {
                return "string";
            }
            else if (l_DBType == "single" || l_DBType == "double" || l_DBType == "decimal")
            {
                return "real";
            }
            else if (l_DBType == "boolean")
            {
                return "boolean";
            }

            return "int";
        }

        public static Declarations.FieldTypes GetFieldType(PropertyInfo property)
        {
            if (property.PropertyType == Type.GetType("System.String"))
            {
                return DB.Declarations.FieldTypes.String;
            }
            else if (property.PropertyType.IsGenericType)
            {
                var types = property.PropertyType.GetGenericArguments();

                if (types.Length == 0)
                {
                    return DB.Declarations.FieldTypes.Number;
                }

                if (property.PropertyType.Name == "Nullable`1")
                {
                    if (types[0] == Type.GetType("System.String"))
                    {
                        return DB.Declarations.FieldTypes.NullableString;
                    }
                    else if (types[0] == Type.GetType("System.DateTime"))
                    {
                        return DB.Declarations.FieldTypes.NullableDate;
                    }
                    else if (types[0] == Type.GetType("System.Boolean"))
                    {
                        return DB.Declarations.FieldTypes.Boolean;
                    }
                    else if (types[0] == Type.GetType("System.Byte"))
                    {
                        return DB.Declarations.FieldTypes.NullableByte;
                    }
                    else if (types[0] == Type.GetType("System.Int32") || types[0] == Type.GetType("System.Int64") || types[0] == Type.GetType("System.UInt32") || types[0] == Type.GetType("System.UInt64"))
                    {
                        return DB.Declarations.FieldTypes.NullableNumber;
                    }
                    else if (types[0] == Type.GetType("System.Int16") || types[0] == Type.GetType("System.UInt16"))
                    {
                        return DB.Declarations.FieldTypes.NullableShortNumber;
                    }

                    return DB.Declarations.FieldTypes.NullableFloat;
                }
                else
                {
                    if (types[0] == Type.GetType("System.String"))
                    {
                        return DB.Declarations.FieldTypes.String;
                    }
                    else if (types[0] == Type.GetType("System.DateTime"))
                    {
                        return DB.Declarations.FieldTypes.Date;
                    }
                    else if (types[0] == Type.GetType("System.Boolean"))
                    {
                        return DB.Declarations.FieldTypes.Boolean;
                    }
                    else if (types[0] == Type.GetType("System.Byte"))
                    {
                        return DB.Declarations.FieldTypes.Byte;
                    }
                    else if (types[0] == Type.GetType("System.Int32") || types[0] == Type.GetType("System.Int64") || types[0] == Type.GetType("System.UInt32") || types[0] == Type.GetType("System.UInt64"))
                    {
                        return DB.Declarations.FieldTypes.Number;
                    }
                    else if (types[0] == Type.GetType("System.Int16") || types[0] == Type.GetType("System.UInt16"))
                    {
                        return DB.Declarations.FieldTypes.ShortNumber;
                    }

                    return DB.Declarations.FieldTypes.NumberFloat;
                }
            }
            else if (property.PropertyType == Type.GetType("System.DateTime"))
            {
                return DB.Declarations.FieldTypes.Date;
            }
            else if (property.PropertyType == Type.GetType("System.Boolean"))
            {
                return DB.Declarations.FieldTypes.Boolean;
            }
            else if (property.PropertyType == Type.GetType("System.Byte"))
            {
                return DB.Declarations.FieldTypes.Byte;
            }
            else if (property.PropertyType == Type.GetType("System.Int32") || property.PropertyType == Type.GetType("System.Int64") || property.PropertyType == Type.GetType("System.UInt32") || property.PropertyType == Type.GetType("System.UInt64"))
            {
                return DB.Declarations.FieldTypes.Number;
            }
            else if (property.PropertyType == Type.GetType("System.Int16") || property.PropertyType == Type.GetType("System.UInt16"))
            {
                return DB.Declarations.FieldTypes.ShortNumber;
            }

            return DB.Declarations.FieldTypes.NumberFloat;
        }

        public static DataTable ConvertToDataTable<t>(IList<t> list)
        {
            DataTable l_Data = new DataTable();

            if (!list.Any())
            {
                // don't know schema ....
                return l_Data;
            }

            var fields = list.First().GetType().GetProperties();

            foreach (var field in fields)
                l_Data.Columns.Add(field.Name, field.PropertyType);
            foreach (var item in list)
            {
                DataRow row = l_Data.NewRow();
                foreach (var field in fields)
                {
                    var p = item.GetType().GetProperty(field.Name);
                    row[field.Name] = p.GetValue(item, null);
                }

                l_Data.Rows.Add(row);
            }

            return l_Data;
        }

        public static bool SQLDataType(string l_DataType)
        {
            bool isDataType = false;

            switch (l_DataType)
            {
                case "int":
                case "bigint":
                case "smallint":
                case "tinyint":
                case "decimal":
                case "float":
                case "money":
                    {
                        isDataType = true;
                        break;
                    }
                default:
                    {
                        isDataType = false;
                        break;
                    }
            }

            return isDataType;
        }

        public static bool ConvertDataType(string l_DataType, object l_Value)
        {
            bool isConvert = true;

            if (l_Value != DBNull.Value)
            {
                try
                {
                    switch (l_DataType)
                    {
                        case "int":
                        case "smallint":
                        case "bigint":
                        case "tinyint":
                        case "System.Int16":
                        case "System.Int32":
                        case "System.Int64":
                            {
                                Conversions.ToInteger(l_Value);
                                break;
                            }
                        case "float":
                        case "decimal":
                        case "money":
                        case "System.Decimal":
                            {
                                Conversions.ToDecimal(l_Value);
                                break;
                            }
                    }
                }
                catch (Exception e)
                {
                    isConvert = false;
                }
            }

            return isConvert;
        }

        public static void BulkInsert(string connectionString, string destinationTableName, DataTable dataTable)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = destinationTableName;
                    bulkCopy.BulkCopyTimeout = 600;

                    try
                    {
                        bulkCopy.WriteToServer(dataTable);
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
        }

        public static DataTable GetDataFromApplicationSettings(string p_SettingName, string p_connectionString)
        {
            DataTable dataTable = new DataTable();
            string query = string.Empty;

            query = $"SELECT * FROM ApplicationSettings WHERE TagName = '{p_SettingName}'";

            using (SqlConnection connection = new SqlConnection(p_connectionString))
            {
                SqlDataAdapter dataAdapter = new SqlDataAdapter(query, connection);

                try
                {
                    connection.Open();
                    dataAdapter.Fill(dataTable); // Fills the DataTable with data from the SQL table based on the query
                }
                catch (Exception ex)
                {
                    throw new Exception("Error retrieving data from SQL table", ex);
                }
            }

            return dataTable;
        }

    }
}