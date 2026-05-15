using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eSyncMate.Processor.Helpers
{
    public static class EncryptionHelper
    {
        private const string EncPrefix = "ENC:";

        // Fields to blank out when sending to UI (non-sensitive fields still shown)
        private static readonly string[] SensitiveFields =
        {
            "Password", "ConnectionString", "ConsumerSecret", "ConsumerKey",
            "Token", "TokenSecret", "Username", "BaseUrl", "Host", "Url",
            "AuthType", "Command", "ConnectionString"
        };

        // ── AES-256 Encrypt (entire string) ──────────────────────
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            if (plainText.StartsWith(EncPrefix))  return plainText;

            using var aes   = Aes.Create();
            aes.Key         = DeriveKey(key);
            aes.GenerateIV();
            var plainBytes  = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = aes.CreateEncryptor().TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            var result      = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV,      0, result, 0,            aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
            return EncPrefix + Convert.ToBase64String(result);
        }

        // ── AES-256 Decrypt (entire string) ──────────────────────
        public static string Decrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText))   return cipherText;
            if (!cipherText.StartsWith(EncPrefix))  return cipherText;

            var allBytes = Convert.FromBase64String(cipherText.Substring(EncPrefix.Length));
            using var aes = Aes.Create();
            aes.Key       = DeriveKey(key);
            var iv        = new byte[aes.IV.Length];
            var cipher    = new byte[allBytes.Length - iv.Length];
            Buffer.BlockCopy(allBytes, 0,         iv,     0, iv.Length);
            Buffer.BlockCopy(allBytes, iv.Length, cipher, 0, cipher.Length);
            aes.IV        = iv;
            var plainBytes = aes.CreateDecryptor().TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        // ── Encrypt entire connector JSON blob ────────────────────
        public static string EncryptConnectorData(string jsonData, string key)
        {
            if (string.IsNullOrWhiteSpace(jsonData)) return jsonData;
            return Encrypt(jsonData, key);
        }

        // ── Decrypt entire connector JSON blob ────────────────────
        public static string DecryptConnectorData(string jsonData, string key)
        {
            if (string.IsNullOrWhiteSpace(jsonData)) return jsonData;
            return Decrypt(jsonData, key);
        }

        // ── Decrypt then mask sensitive fields for UI display ─────
        public static string MaskConnectorData(string jsonData)
        {
            if (string.IsNullOrWhiteSpace(jsonData)) return jsonData;
            // If encrypted blob → can't parse without key → return empty JSON
            if (jsonData.StartsWith(EncPrefix))
                return "{}";
            try
            {
                var obj = JObject.Parse(jsonData);
                foreach (var field in SensitiveFields)
                {
                    if (obj[field] != null && obj[field].Type == JTokenType.String)
                        obj[field] = "";
                }
                return obj.ToString(Formatting.None);
            }
            catch { return "{}"; }
        }

        // ── Decrypt then mask (when key is available) ─────────────
        public static string DecryptAndMaskConnectorData(string jsonData, string key)
        {
            if (string.IsNullOrWhiteSpace(jsonData)) return jsonData;
            var decrypted = DecryptConnectorData(jsonData, key);
            return MaskConnectorData(decrypted);
        }

        // ── 256-bit key derivation ────────────────────────────────
        private static byte[] DeriveKey(string key)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        }
    }
}
