using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Outcome of verifying an inbound signed push from BizMate. The distinct values are the
    /// refusal reasons the contract names, so a rejected callback can say why it was rejected.
    /// </summary>
    public enum SignatureVerificationResult
    {
        Valid,
        MissingTimestamp,
        MalformedTimestamp,
        TimestampOutsideWindow,
        MissingSignature,
        MalformedSignature,
        SignatureMismatch
    }

    /// <summary>
    /// HMAC-SHA256 request signing for the BizMate integration endpoints, both directions.
    /// Contract: M1-Contracts/auth-and-signing.md section 3, contract version 1.0 (2026-09-02).
    ///
    /// Signed string is {timestamp}\n{METHOD}\n{public path}\n{sha256hex(body)}, LF only, and the
    /// signature travels as X-Signature: sha256=(lowercase hex) alongside X-Timestamp.
    ///
    /// The path signed is always the PUBLIC one (/v1/integration/...). Kong strips that prefix
    /// before the request reaches BizMate and re-adds it from X-Forwarded-Prefix before verifying,
    /// so signing the stripped suffix produces a signature that fails on the far side for reasons
    /// nothing local can show. That mistake is refused here rather than documented -- see
    /// NormalisePath.
    /// </summary>
    public static class BizMateRequestSigner
    {
        /// <summary>Replay window either side of the receiver's clock, in seconds (contract rule 1).</summary>
        public const int ReplayWindowSeconds = 300;

        public const string TimestampHeader = "X-Timestamp";
        public const string SignatureHeader = "X-Signature";

        private const string SignaturePrefix = "sha256=";
        private const string PublicPathPrefix = "/v1/integration";

        /// <summary>The signed path must be the public one. Anything else is a caller mistake.</summary>
        public static string NormalisePath(string publicPath)
        {
            if (string.IsNullOrWhiteSpace(publicPath))
            {
                throw new ArgumentException("A path is required to sign a request.", nameof(publicPath));
            }

            // Query strings are never signed.
            int l_Query = publicPath.IndexOf('?');
            string l_Path = l_Query >= 0 ? publicPath.Substring(0, l_Query) : publicPath;

            // A full URL is a common slip; take the path component so the caller does not have to.
            if (Uri.TryCreate(l_Path, UriKind.Absolute, out Uri? l_Absolute))
            {
                l_Path = l_Absolute.AbsolutePath;
            }

            if (!l_Path.StartsWith('/'))
            {
                l_Path = "/" + l_Path;
            }

            if (!l_Path.StartsWith(PublicPathPrefix, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Sign the public path, which begins '{PublicPathPrefix}', not the Kong-stripped suffix. " +
                    $"Received '{l_Path}'. See auth-and-signing.md section 3.",
                    nameof(publicPath));
            }

            return l_Path;
        }

        /// <summary>Lowercase hex SHA-256 of the raw body bytes. An empty body hashes the empty string.</summary>
        public static string HashBody(byte[]? body)
        {
            return Convert.ToHexString(SHA256.HashData(body ?? Array.Empty<byte>())).ToLowerInvariant();
        }

        public static string HashBody(string? body)
        {
            return HashBody(body == null ? null : Encoding.UTF8.GetBytes(body));
        }

        /// <summary>{timestamp}\n{METHOD}\n{public path}\n{sha256hex(body)} -- LF only, method upper-case.</summary>
        public static string BuildSignedString(long timestamp, string method, string publicPath, string bodyHash)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                throw new ArgumentException("An HTTP method is required to sign a request.", nameof(method));
            }

            string l_Method = method.Trim().ToUpperInvariant();
            string l_Path = NormalisePath(publicPath);

            return string.Concat(
                timestamp.ToString(CultureInfo.InvariantCulture), "\n",
                l_Method, "\n",
                l_Path, "\n",
                bodyHash);
        }

        /// <summary>
        /// Signs a request and returns the two headers to send. Pass <paramref name="timestamp"/>
        /// only to reproduce a known vector; production callers leave it null so the current time
        /// is used.
        /// </summary>
        public static (long Timestamp, string Signature) Sign(
            string method,
            string publicPath,
            byte[]? body,
            string signingSecret,
            long? timestamp = null)
        {
            if (string.IsNullOrEmpty(signingSecret))
            {
                throw new ArgumentException("The BizMate signing secret is required.", nameof(signingSecret));
            }

            long l_Timestamp = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string l_Signed = BuildSignedString(l_Timestamp, method, publicPath, HashBody(body));

            byte[] l_Mac = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(signingSecret),
                Encoding.UTF8.GetBytes(l_Signed));

            return (l_Timestamp, SignaturePrefix + Convert.ToHexString(l_Mac).ToLowerInvariant());
        }

        public static (long Timestamp, string Signature) Sign(
            string method,
            string publicPath,
            string? body,
            string signingSecret,
            long? timestamp = null)
        {
            return Sign(method, publicPath, body == null ? null : Encoding.UTF8.GetBytes(body), signingSecret, timestamp);
        }

        /// <summary>
        /// Verifies an inbound signed push from BizMate (W1-05). Applies the contract's two rules in
        /// order: the timestamp must be present, numeric and inside the replay window, then the
        /// signature must match under a constant-time comparison.
        /// </summary>
        public static SignatureVerificationResult Verify(
            string? timestampHeader,
            string? signatureHeader,
            string method,
            string publicPath,
            byte[]? body,
            string signingSecret,
            DateTimeOffset? now = null)
        {
            if (string.IsNullOrWhiteSpace(timestampHeader))
            {
                return SignatureVerificationResult.MissingTimestamp;
            }

            if (!long.TryParse(timestampHeader.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long l_Timestamp))
            {
                return SignatureVerificationResult.MalformedTimestamp;
            }

            long l_Now = (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();

            if (Math.Abs(l_Now - l_Timestamp) > ReplayWindowSeconds)
            {
                return SignatureVerificationResult.TimestampOutsideWindow;
            }

            if (string.IsNullOrWhiteSpace(signatureHeader))
            {
                return SignatureVerificationResult.MissingSignature;
            }

            string l_Presented = signatureHeader.Trim();

            if (!l_Presented.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return SignatureVerificationResult.MalformedSignature;
            }

            string l_PresentedHex = l_Presented.Substring(SignaturePrefix.Length);
            byte[] l_PresentedBytes;

            try
            {
                l_PresentedBytes = Convert.FromHexString(l_PresentedHex);
            }
            catch (FormatException)
            {
                return SignatureVerificationResult.MalformedSignature;
            }

            string l_Signed = BuildSignedString(l_Timestamp, method, publicPath, HashBody(body));

            byte[] l_Expected = HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(signingSecret),
                Encoding.UTF8.GetBytes(l_Signed));

            // Constant-time, and length-safe: FixedTimeEquals returns false on a length mismatch
            // without leaking where the difference is.
            return CryptographicOperations.FixedTimeEquals(l_Expected, l_PresentedBytes)
                ? SignatureVerificationResult.Valid
                : SignatureVerificationResult.SignatureMismatch;
        }

        /// <summary>
        /// Reproduces the computed test vector published with the contract
        /// (auth-and-signing.md section 3). Returns true when this implementation agrees with
        /// BizMate's reference implementation. Cheap enough to call at start-up.
        /// </summary>
        public static bool SelfTest()
        {
            const string c_Secret = "0123456789abcdef0123456789abcdef";
            const string c_Body = "{\"hello\":\"world\"}";
            const long c_Timestamp = 1700000000L;
            const string c_ExpectedBodyHash = "93a23971a914e5eacbf0a8d25154cda309c3c1c72fbb9914d47c60f3cb681588";
            const string c_ExpectedSignature = "sha256=fc7f61c4e73a1ff57903fd43faa5049f2f99b0f649d17923751a90ef8f1267b7";

            if (!string.Equals(HashBody(c_Body), c_ExpectedBodyHash, StringComparison.Ordinal))
            {
                return false;
            }

            (long _, string l_Signature) = Sign("POST", "/v1/integration/raw", c_Body, c_Secret, c_Timestamp);

            return string.Equals(l_Signature, c_ExpectedSignature, StringComparison.Ordinal);
        }
    }
}
