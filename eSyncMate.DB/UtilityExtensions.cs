using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using System;
using System.Data;
using System.IO;

namespace eSyncMate.DB
{
    public static class UtilityExtensions
    {
        public static T GetValue<T>(this DataRow p_Row, string p_Name, T p_DefaultValue = default)
        {
            try
            {
                if (!Information.IsDBNull(p_Row[p_Name]))
                {
                    return (T)Convert.ChangeType(p_Row[p_Name], typeof(T));
                }

                return p_DefaultValue;
            }
            catch (Exception ex)
            {
                return p_DefaultValue;
            }
        }
        
        public static string GetValue(this DataRow p_Row, string p_Name)
        {
            try
            {
                if (!Information.IsDBNull(p_Row[p_Name]))
                {
                    return Conversions.ToString(p_Row[p_Name]);
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }

            return string.Empty;
        }
        
        public static byte[] ToCSVBytes(this DataTable p_DataTable)
        {
            byte[] l_CsvFileBytes;

            if (p_DataTable == null)
            {
                return new byte[0];
            }

            using (var l_MemoryStream = new MemoryStream())
            {
                using (var l_StreamWriter = new StreamWriter(l_MemoryStream))
                {
                    PublicFunctions.WriteDataTable(p_DataTable, l_StreamWriter, true);
                }

                l_CsvFileBytes = l_MemoryStream.ToArray();
            }

            return l_CsvFileBytes;
        }
        
        public static bool IsJSON(this string p_InputStr)
        {
            bool l_ReturnValue = false;

            if (string.IsNullOrWhiteSpace(p_InputStr))
            {
                l_ReturnValue = false;
            }

            p_InputStr = p_InputStr.Trim();

            bool l_IsObject = p_InputStr.StartsWith("{") && p_InputStr.EndsWith("}");
            bool l_IsArray = p_InputStr.StartsWith("[") && p_InputStr.EndsWith("]");

            if (l_IsObject || l_IsArray)
            {
                try
                {
                    var l_Obj = JsonConvert.DeserializeObject(p_InputStr);

                    l_ReturnValue = true;
                }
                catch
                {
                    l_ReturnValue = false;
                }
            }

            return l_ReturnValue;
        }
    }
}