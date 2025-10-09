using System;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace eSyncMate.DB
{
    public class DataConverter
    {
        public static bool ConvertToBoolean(object Val)
        {
            bool rest = false;
            try
            {
                if (!string.IsNullOrEmpty(Conversions.ToString(Val)))
                {
                    rest = Convert.ToBoolean(Val);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return rest;
        }

        public static int ConvertToInteger(object Val)
        {
            int rest = 0;
            try
            {
                if (!string.IsNullOrEmpty(Conversions.ToString(Val)))
                {
                    rest = Convert.ToInt32(Val);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return rest;
        }

        public static decimal ConvertToDecimal(object Val)
        {
            decimal rest = 0m;
            try
            {
                rest = DecimalRoundTwoPoint(Val);
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return rest;
        }

        public static float ConvertToFloat(object Val)
        {
            float rest = 0.0f;

            try
            {
                if (!string.IsNullOrEmpty(Conversions.ToString(Val)))
                {
                    rest = Convert.ToSingle(Val);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return rest;
        }

        public static double ConvertCharToDouble(object Val)
        {
            double rest = 0d;
            try
            {
                rest = (double)DecimalRoundTwoPoint(Val);
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return rest;
        }

        public static double ConvertToDouble(object Val)
        {
            double rest = 0d;
            try
            {
                rest = (double)DecimalRoundTwoPoint(Val);
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return rest;
        }

        public static float ConvertToSingle(object Val)
        {
            float rest = 0f;
            try
            {
                rest = Convert.ToSingle(Val);
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return rest;
        }

        public static decimal ConvertToCurrency(object Val)
        {
            decimal rest = 0m;
            try
            {
                rest = DecimalRoundTwoPoint(Val);
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return rest;
        }

        public static string ConvertToString(object Val)
        {
            string rest = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(Conversions.ToString(Val)))
                {
                    rest = Convert.ToString(Val);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return rest;
        }

        public static DateTime ConvertToDateTime(object Val)
        {
            var rest = DateTime.MinValue;
            try
            {
                if (!string.IsNullOrEmpty(Conversions.ToString(Val)))
                {
                    rest = Convert.ToDateTime(Val);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
                return rest;
            }

            return rest;
        }

        public static string ConvertDateToString(object Val)
        {
            string rest = string.Empty;
            try
            {
                rest = Convert.ToDateTime(Val).ToShortDateString();
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return rest;
        }

        public static string ConvertNull(object val, string defaultval)
        {
            try
            {
                if (Information.IsNothing(val) == true)
                {
                    return defaultval;
                }
                else
                {
                    return Conversions.ToString(val);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
                return defaultval;
            }
        }

        public static int ConvertToInt64(object Val)
        {
            int rest = 0;
            try
            {
                rest = (int)Convert.ToInt64(Val);
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return rest;
        }

        public static string ConvertToUpperCase(object Val)
        {
            string rest = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(Conversions.ToString(Val)))
                {
                    return Val.ToString().ToUpper().Trim();
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
            }

            return string.Empty;
        }

        public static string ConvertToSQLDateTime(object Val)
        {
            string rest = string.Empty;
            try
            {
                if (Convert.ToString(Val).Contains("'"))
                {
                    rest = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("cast(", Val), " as datetime)"));
                }
                else
                {
                    rest = Conversions.ToString(Operators.ConcatenateObject(Operators.ConcatenateObject("cast('", Val), "' as datetime)"));
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
                return "";
            }

            return rest;
        }

        public static decimal DecimalRoundTwoPoint(object argument)
        {
            try
            {
                if (!string.IsNullOrEmpty(Conversions.ToString(argument)))
                {
                    return decimal.Round(Conversions.ToDecimal(argument), 2);
                }
                else
                {
                    return 0m;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.WriteToErrorLog(ex.Message, ex.StackTrace);
                return 0m;
            }
        }
    }
}