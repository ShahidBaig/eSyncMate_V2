using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Globalization;
using eSyncMate.Processor.Models;
using Nancy;
using eSyncMate.DB.Entities;
using System.Data;

namespace eSyncMate.Maps
{
    public class Transformations
    {
        public static Dictionary<string, string> purposeMap = new Dictionary<string, string>();
        public static Dictionary<string, string> orderTypeMap = new Dictionary<string, string>();
        public static Dictionary<string, string> paymentMethodMap = new Dictionary<string, string>();
        public static Dictionary<string, string> priceIdentifierMap = new Dictionary<string, string>();
        public static Dictionary<string, string> allowanceChargeMap = new Dictionary<string, string>();

        public static string findTagValue(object tags, string matchIndex, string matchValue, string returnIndex)
        {
            int returnIndexInt = Convert.ToInt32(returnIndex.Trim());
            string[] matchIndexArray = matchIndex.Trim().Split('|');
            string[] matchValueArray = matchValue.Trim().Split('|');
            JArray ediTags = null;

            if (tags == null)
                return string.Empty;

            try
            {
                ediTags = JArray.Parse(JsonConvert.SerializeObject(tags));
            }
            catch (Exception)
            {
                ediTags = new JArray();
                ediTags.Add(JToken.Parse(JsonConvert.SerializeObject(tags)));
            }

            foreach (JObject tag in ediTags)
            {
                try
                {
                    bool isMatch = true;

                    for (int i = 0; i < matchIndexArray.Length && isMatch; i++)
                    {
                        int matchIndexInt = Convert.ToInt32(matchIndexArray[i]);
                        string matchValueString = matchValueArray[i];

                        if (tag["Content"][matchIndexInt]["E"].ToString() != matchValueString)
                        {
                            isMatch = false;
                        }
                    }

                    if (isMatch)
                    {
                        try
                        {
                            return tag["Content"][returnIndexInt]["E"].ToString();
                        }
                        catch (Exception ex)
                        {
                            return string.Empty;
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }

            return string.Empty;
        }

        public static string findMapTagValue(string key, string mapMethod)
        {
            string value = string.Empty;

            if (!string.IsNullOrEmpty(key))
            {
                if (mapMethod == "purposeMap")
                {
                    if (purposeMap.TryGetValue(key, out value))
                    {
                        return value;
                    }

                    return string.Empty;

                }
                else if (mapMethod == "orderTypeMap")
                {
                    if (orderTypeMap.TryGetValue(key, out value))
                    {
                        return value;
                    }

                    return string.Empty;
                }
                else if (mapMethod == "priceIdentifierMap")
                {
                    if (priceIdentifierMap.TryGetValue(key, out value))
                    {
                        return value;
                    }

                    return string.Empty;
                }
                else if (mapMethod == "paymentMethodMap")
                {
                    if (paymentMethodMap.TryGetValue(key, out value))
                    {
                        return value;
                    }

                    return string.Empty;
                }
                else if (mapMethod == "allowanceChargeMap")
                {
                    if (allowanceChargeMap.TryGetValue(key, out value))
                    {
                        return value;
                    }

                    return string.Empty;
                }
            }

            return value;
        }

        public static string findLoopTagValue(object tags, string parentTagName, string findTagName, string matchParentIndex, string matchParentValue, string returnIndex)
        {
            int returnIndexInt = Convert.ToInt32(returnIndex.Trim());
            int matchIndexInt = Convert.ToInt32(matchParentIndex.Trim());
            JArray ediTags = null;
            bool isMatch = false;
            JArray parentTagObject = null;

            if (tags == null)
            {
                return string.Empty;
            }

            try
            {
                ediTags = JArray.Parse(JsonConvert.SerializeObject(tags));
            }
            catch (Exception)
            {
                ediTags = new JArray();
                ediTags.Add(JToken.Parse(JsonConvert.SerializeObject(tags)));
            }

            foreach (JObject loopTag in ediTags)
            {
                JArray loopTagContents = (JArray)loopTag["Content"];

                foreach (JObject tag in loopTagContents)
                {
                    if (tag["Name"].ToString() == parentTagName)
                    {
                        if (tag["Content"][matchIndexInt]["E"].ToString() == matchParentValue)
                        {
                            parentTagObject = loopTagContents;
                            break;
                        }
                    }
                }

                if (parentTagObject != null)
                {
                    break;
                }
            }

            if (parentTagObject == null)
            {
                return string.Empty;
            }

            try
            {
                foreach (JObject tag in parentTagObject)
                {
                    if (tag["Name"].ToString() == findTagName)
                    {
                        return tag["Content"][returnIndexInt]["E"].ToString();
                    }
                }
            }
            catch (Exception)
            {
            }

            return string.Empty;
        }

        public static string findLoopTagValueWithoutMatch(object tags, string parentTagName, string findTagName, string returnIndex)
        {
            int returnIndexInt = Convert.ToInt32(returnIndex.Trim());
            JArray ediTags = null;
            bool isMatch = false;
            JArray parentTagObject = null;

            if (tags == null)
            {
                return string.Empty;
            }

            try
            {
                ediTags = JArray.Parse(JsonConvert.SerializeObject(tags));
            }
            catch (Exception)
            {
                ediTags = new JArray();
                ediTags.Add(JToken.Parse(JsonConvert.SerializeObject(tags)));
            }

            foreach (JObject loopTag in ediTags)
            {
                JArray loopTagContents = (JArray)loopTag["Content"];

                foreach (JObject tag in loopTagContents)
                {
                    if (tag["Name"].ToString() == parentTagName)
                    {
                        parentTagObject = loopTagContents;
                    }
                }

                if (parentTagObject != null)
                {
                    break;
                }
            }

            if (parentTagObject == null)
            {
                return string.Empty;
            }

            try
            {
                foreach (JObject tag in parentTagObject)
                {
                    if (tag["Name"].ToString() == findTagName)
                    {
                        return tag["Content"][returnIndexInt]["E"].ToString();
                    }
                }
            }
            catch (Exception)
            {
            }

            return string.Empty;
        }

        public static string findLoopTagByMatchValue(object tags, string parentTagName, string findTagName, string matchParentIndex, string matchParentValue, string returnMatchIndex, string returnIndex)
        {
            int returnIndexInt = Convert.ToInt32(returnIndex.Trim());
            int returnMatchIndexInt = Convert.ToInt32(returnMatchIndex.Trim());
            int matchIndexInt = Convert.ToInt32(matchParentIndex.Trim());
            JArray ediTags = null;
            bool isMatch = false;
            JArray parentTagObject = null;

            if (tags == null)
            {
                return string.Empty;
            }

            try
            {
                ediTags = JArray.Parse(JsonConvert.SerializeObject(tags));
            }
            catch (Exception)
            {
                ediTags = new JArray();
                ediTags.Add(JToken.Parse(JsonConvert.SerializeObject(tags)));
            }

            foreach (JObject loopTag in ediTags)
            {
                JArray loopTagContents = (JArray)loopTag["Content"];

                foreach (JObject tag in loopTagContents)
                {
                    if (tag["Name"].ToString() == parentTagName)
                    {
                        if (tag["Content"][matchIndexInt]["E"].ToString() == matchParentValue)
                        {
                            parentTagObject = loopTagContents;
                            break;
                        }
                    }
                }

                if (parentTagObject != null)
                {
                    break;
                }
            }

            if (parentTagObject == null)
            {
                return string.Empty;
            }

            try
            {
                foreach (JObject tag in parentTagObject)
                {
                    if (tag["Name"].ToString() == findTagName)
                    {
                        if (tag["Content"][returnMatchIndexInt]["E"].ToString() == matchParentValue)
                        {
                            return tag["Content"][returnIndexInt]["E"].ToString();
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return string.Empty;
        }

        public static string findFirstLoopTagValue(object tags, string findTagName, string matchFindIndex, string matchFindValue, string returnIndex)
        {
            int returnIndexInt = Convert.ToInt32(returnIndex.Trim());
            int matchIndexInt = Convert.ToInt32(matchFindIndex.Trim());
            JArray ediTags = null;
            bool isMatch = false;
            JArray parentTagObject = null;

            if (tags == null)
            {
                return string.Empty;
            }

            try
            {
                ediTags = JArray.Parse(JsonConvert.SerializeObject(tags));
            }
            catch (Exception)
            {
                ediTags = new JArray();
                ediTags.Add(JToken.Parse(JsonConvert.SerializeObject(tags)));
            }

            foreach (JObject loopTag in ediTags)
            {
                JArray loopTagContents = (JArray)loopTag["Content"];

                foreach (JObject tag in loopTagContents)
                {
                    if (tag["Name"].ToString() == findTagName)
                    {
                        if (tag["Content"][matchIndexInt]["E"].ToString() == matchFindValue)
                        {
                            return tag["Content"][returnIndexInt]["E"].ToString();
                        }
                    }
                }

                break;
            }

            return string.Empty;
        }

        public static string formatDate(string value, string format = "")
        {
            var cultureInfo = new CultureInfo("en-US");
            string dateValue = value;
            DateTime date = DateTime.MinValue;

            try
            {
                if (IsValidDateFormat(dateValue, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture))
                {
                    date = DateTime.ParseExact(dateValue, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
                }
                else if (IsValidDateFormat(dateValue, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", cultureInfo))
                {
                    date = DateTime.ParseExact(dateValue, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", cultureInfo);
                }
                else if (IsValidDateFormat(dateValue, "yyyy-MM-dd", cultureInfo))
                {
                    date = DateTime.ParseExact(dateValue, "yyyy-MM-dd", cultureInfo);
                }
                else
                {
                    if(!string.IsNullOrWhiteSpace(dateValue) &&
                                 DateTime.TryParse(dateValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    {
                        date = parsed;

                    }
                    else
                    {
                        date = DateTime.ParseExact(dateValue, "yyyyMMdd", cultureInfo);
                    }
                }
            }
            catch (Exception)
            {
                try
                {
                    date = DateTime.ParseExact(dateValue, "MM/dd/yyyy", cultureInfo);
                }
                catch (Exception)
                {
                    try
                    {
                        date = DateTime.ParseExact(dateValue, "dd/MM/yyyy", cultureInfo);
                    }
                    catch (Exception)
                    {
                        return string.Empty;
                    }
                }
            }

            return date.ToString(string.IsNullOrEmpty(format) ? "yyyyMMdd" : format);
        }
        public static string WalmartformatDate(string value, string format = "")
        {
            var cultureInfo = new CultureInfo("en-US");
            string dateValue = value;
            DateTime date = DateTime.MinValue;

            try
            {
                if (IsValidUnixTimestamp(dateValue))
                {
                    date = UnixTimeStampToDateTime(long.Parse(dateValue));
                }
                else
                {
                    date = DateTime.ParseExact(dateValue, "yyyyMMdd", cultureInfo);
                }
            }
            catch (Exception)
            {
                try
                {
                    date = DateTime.ParseExact(dateValue, "MM/dd/yyyy", cultureInfo);
                }
                catch (Exception)
                {
                    try
                    {
                        date = DateTime.ParseExact(dateValue, "dd/MM/yyyy", cultureInfo);
                    }
                    catch (Exception)
                    {
                        return string.Empty;
                    }
                }
            }

            return date.ToString(string.IsNullOrEmpty(format) ? "yyyyMMdd" : format);
        }
        public static string formatTime(string value, string format = "")
        {
            var cultureInfo = new CultureInfo("en-US");
            string dateValue = value;
            DateTime date = DateTime.MinValue;

            try
            {
                date = DateTime.ParseExact(dateValue, "HH:mm:ss", cultureInfo);
            }
            catch (Exception)
            {
                try
                {
                    date = DateTime.ParseExact(dateValue, "HHmmss", cultureInfo);
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }

            return date.ToString(string.IsNullOrEmpty(format) ? "HH:mm:ss" : format);
        }

        public static string ResolveShipVia(string serviceLevel)
        {
            if (string.Equals(serviceLevel, "Std US D2D Dom", StringComparison.OrdinalIgnoreCase))
                return "P/U";
            return "FEDX";
        }

        public static string formatTotalAmount(string value)
        {
            try
            {
                if (value.Contains("."))
                {
                    string[] nums = value.Split('.');

                    if (nums[1].Length == 1)
                    {
                        value = value.Replace(".", string.Empty) + "0";
                    }
                    else
                    {
                        value = value.Replace(".", string.Empty);
                    }
                }
                else
                {
                    value = value + "00";
                }
            }
            catch (Exception)
            {
            }

            return value;
        }

        public static JArray createSDQArray(object tags)
        {
            JArray ediTags = null;
            JArray locationTags = new JArray();

            if (tags == null)
            {
                return locationTags;
            }

            try
            {
                ediTags = JArray.Parse(JsonConvert.SerializeObject(tags));
            }
            catch (Exception)
            {
                ediTags = new JArray();
                ediTags.Add(JToken.Parse(JsonConvert.SerializeObject(tags)));
            }

            foreach (JObject sdqTag in ediTags)
            {
                JArray sdqTagContents = (JArray)sdqTag["Content"];
                int i = 2;

                while (i < sdqTagContents.Count)
                {
                    JObject locationTag = new JObject();

                    locationTag.Add("locationId", sdqTagContents[i]["E"]);
                    locationTag.Add("quantity", sdqTagContents[i + 1]["E"]);

                    locationTags.Add(locationTag);
                    i += 2;
                }
            }

            return locationTags;
        }

        public static string getCurrentTime()
        {
            return DateTime.Now.ToString("HHmm");
        }

        public static string getCurrentDate()
        {
            return DateTime.Now.ToString("yyyyMMdd");
        }

        private static bool IsValidDateFormat(string dateString, string dateFormat, IFormatProvider provider)
        {
            DateTime parsedDate;
            bool isValid = DateTime.TryParseExact(dateString, dateFormat, provider, DateTimeStyles.None, out parsedDate);
            return isValid;
        }

        private static bool IsValidUnixTimestamp(string dateString)
        {
            return long.TryParse(dateString, out long timestamp) && timestamp > 0;
        }

        private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static string Detailpars856Json(string value)
        {
            JObject rootObject = JsonConvert.DeserializeObject<JObject>(value);
            List<JToken> results = new List<JToken>();

            if (rootObject.Type == JTokenType.Object && rootObject["Type"] != null && rootObject["Type"].ToString() == "L")
            {
                if (rootObject["Content"][0]["Content"][2]["E"].ToString() == "P")
                    results.Add(rootObject);
            }
            FindContentWithEEqualsP(rootObject, results);

            // Convert the results to a JSON array string
            return JsonConvert.SerializeObject(results);
        }

        public static void FindContentWithEEqualsP(JToken node, List<JToken> results)
        {
            // Check if the current node is an object and contains an "E" property with value "P"
            JArray contents = JsonConvert.DeserializeObject<JArray>(node["Content"].ToString());
            // Recursively process child objects and arrays
            foreach (JToken child in contents)
            {
                if (child.Type == JTokenType.Object && child["Name"] != null && child["Name"].ToString() == "L_HL")
                {
                    JArray childContents = JsonConvert.DeserializeObject<JArray>(child["Content"].ToString());
                    foreach (JToken innerChild in childContents)
                    {
                        if (innerChild.Type == JTokenType.Object && innerChild["Name"] != null && innerChild["Name"].ToString() == "L_HL")
                        {
                            if (innerChild["Type"] != null && innerChild["Type"].ToString() == "L")
                            {
                                if (innerChild["Content"][0]["Content"][2]["E"].ToString() == "P")
                                {
                                    results.Add(innerChild);
                                    FindContentWithEEqualsP(innerChild, results);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static string TransDistributionID(string value)
        {
            Orders l_Orders = new Orders();
            DataTable l_dt = new DataTable();
            string l_WHSID = string.Empty;
            
            try
            {
                l_Orders.UseConnection(CommonUtils.ConnectionString);

                l_Orders.GetDistributionID(value, ref l_dt);

                if (l_dt.Rows.Count > 0)
                {
                    l_WHSID = l_dt.Rows[0]["WHSID"].ToString();
                }
                
                return l_WHSID;

            }
            catch (Exception)
            {
                return l_WHSID = "";
            }
        }


        public static string AmazonItemID(string value, string ASIN = "")
        {
            string dateValue = value;
            string ItemID = string.Empty;

            try
            {
                ItemID = value.Replace($"{ASIN}", "").Trim();
            }
            catch (Exception)
            {
               
            }

            return ItemID;
        }
    }
}
