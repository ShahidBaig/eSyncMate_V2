using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eSyncMate.Processor.Models
{
    /// <summary>
    /// Keeps OrderData / RouteData logs valid JSON: a partner answer is not always JSON (an HTTP error page,
    /// a plain-text error, an empty body), and a JSON answer put inside another object must stay an object,
    /// not an escaped string.
    /// </summary>
    public static class LogJson
    {
        // Valid JSON object / array -> as it is; anything else -> {"message": text}
        public static string Ensure(string p_Text)
        {
            JToken l_Token = Parse(p_Text);

            return l_Token != null ? p_Text : JsonConvert.SerializeObject(new { message = p_Text ?? string.Empty });
        }

        // For a value inside another log object: parsed JSON when it is JSON, else the text itself
        public static JToken Embed(string p_Text)
        {
            return Parse(p_Text) ?? new JValue(p_Text ?? string.Empty);
        }

        private static JToken Parse(string p_Text)
        {
            string l_Text = p_Text?.Trim() ?? string.Empty;

            if (!(l_Text.StartsWith("{") || l_Text.StartsWith("[")))
                return null;

            try
            {
                return JToken.Parse(l_Text);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
