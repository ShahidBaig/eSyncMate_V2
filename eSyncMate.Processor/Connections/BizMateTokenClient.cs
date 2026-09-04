using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using eSyncMate.Processor.Models;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// The scopes eSyncMate may request. Each route asks for what it needs and nothing more (W1-03).
    ///
    /// integration.admin is deliberately absent: it is BizMate UI only and is never granted to
    /// eSyncMate, so there is no constant here to reach for by accident.
    /// </summary>
    public static class BizMateScopes
    {
        public const string Inbound = "integration.inbound";        // POST /raw, POST /inbound/{docType}
        public const string Ack = "integration.ack";                // POST /ack
        public const string OutboundRead = "integration.outbound.read"; // GET /outbound/pending, fetched, delivered
        public const string ConfigRead = "config.read";             // GET /config/{customerNo}

        public static string Join(params string[] scopes) => string.Join(' ', scopes);
    }

    /// <summary>A token and the moment it stops being usable.</summary>
    internal sealed class CachedToken
    {
        public string AccessToken { get; init; } = string.Empty;
        public string Scope { get; init; } = string.Empty;
        public DateTimeOffset RenewAt { get; init; }

        public bool IsUsable(DateTimeOffset now) => now < RenewAt && !string.IsNullOrEmpty(AccessToken);
    }

    internal sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("token_type")] public string? TokenType { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("scope")] public string? Scope { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
    }

    public sealed class BizMateAuthException : Exception
    {
        public string? OAuthError { get; }

        public BizMateAuthException(string message, string? oauthError = null) : base(message)
        {
            OAuthError = oauthError;
        }
    }

    /// <summary>
    /// OAuth 2.0 client-credentials token client for BizMate (W1-02, W1-03, requirement E14).
    /// Contract: M1-Contracts/auth-and-signing.md section 2.
    ///
    /// Tokens live 30 minutes and there are no refresh tokens, so renewal means asking again with
    /// the secret. The cache renews about a minute early rather than on expiry, because a token
    /// that expires in flight is indistinguishable at the far end from a bad credential.
    ///
    /// Cached per (gateway, clientId, scope). Scope is part of the key on purpose: BizMate
    /// intersects the requested scopes with the credential's ceiling and returns what it actually
    /// granted, so a token fetched for one scope set must not be handed to a route that asked for
    /// a different one. When W6 arrives, party id joins this key (EQ-04 confirms one cached token
    /// per party).
    /// </summary>
    public sealed class BizMateTokenClient
    {
        /// <summary>Renew this far ahead of expiry (contract: "~1 minute before expires_in").</summary>
        public static readonly TimeSpan RenewalMargin = TimeSpan.FromSeconds(60);

        private const string TokenPath = "/v1/auth/token";

        private static readonly ConcurrentDictionary<string, CachedToken> _tokens = new();
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        private readonly string _gatewayBaseUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;

        public BizMateTokenClient(string gatewayBaseUrl, string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(gatewayBaseUrl))
            {
                throw new ArgumentException("The BizMate gateway URL is required.", nameof(gatewayBaseUrl));
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentException("The BizMate clientId is required.", nameof(clientId));
            }

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new ArgumentException("The BizMate clientSecret is required.", nameof(clientSecret));
            }

            _gatewayBaseUrl = gatewayBaseUrl.TrimEnd('/');
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        internal static string CacheKey(string gateway, string clientId, string scope)
        {
            // Scope order must not create a second cache entry for the same grant.
            string l_Normalised = string.Join(' ',
                (scope ?? string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .OrderBy(s => s, StringComparer.Ordinal));

            return string.Concat(gateway, "|", clientId, "|", l_Normalised);
        }

        /// <summary>
        /// Returns a usable bearer token for the given scopes, from cache when one is still good.
        /// Pass the scopes the calling route actually needs (W1-03) - never every scope "just in case".
        /// </summary>
        public async Task<string> GetTokenAsync(string scope, CancellationToken cancellationToken = default)
        {
            string l_Key = CacheKey(_gatewayBaseUrl, _clientId, scope);
            DateTimeOffset l_Now = DateTimeOffset.UtcNow;

            if (_tokens.TryGetValue(l_Key, out CachedToken? l_Cached) && l_Cached.IsUsable(l_Now))
            {
                return l_Cached.AccessToken;
            }

            // One fetch per key at a time. Without this, a burst of routes starting together each
            // asks BizMate for its own token and four of the five are thrown away.
            SemaphoreSlim l_Gate = _locks.GetOrAdd(l_Key, static _ => new SemaphoreSlim(1, 1));

            await l_Gate.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Someone may have refreshed it while we waited.
                if (_tokens.TryGetValue(l_Key, out l_Cached) && l_Cached.IsUsable(DateTimeOffset.UtcNow))
                {
                    return l_Cached.AccessToken;
                }

                CachedToken l_Fresh = await RequestTokenAsync(scope, cancellationToken).ConfigureAwait(false);

                _tokens[l_Key] = l_Fresh;

                return l_Fresh.AccessToken;
            }
            finally
            {
                l_Gate.Release();
            }
        }

        private async Task<CachedToken> RequestTokenAsync(string scope, CancellationToken cancellationToken)
        {
            // Shared, pooled client - never a new HttpClient per request.
            HttpClient l_Client = SharedHttpClientFactory.GetOrCreateClient("bizmate-auth");

            var l_Fields = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials"),
                new("client_id", _clientId),
                new("client_secret", _clientSecret)
            };

            // Omitting scope grants every scope on the credential. We always ask, so the token
            // carries the least authority the caller needs.
            if (!string.IsNullOrWhiteSpace(scope))
            {
                l_Fields.Add(new KeyValuePair<string, string>("scope", scope));
            }

            using var l_Request = new HttpRequestMessage(HttpMethod.Post, _gatewayBaseUrl + TokenPath)
            {
                Content = new FormUrlEncodedContent(l_Fields)
            };

            l_Request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using HttpResponseMessage l_Response =
                await l_Client.SendAsync(l_Request, cancellationToken).ConfigureAwait(false);

            string l_Body = await l_Response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            TokenResponse? l_Token;

            try
            {
                l_Token = JsonSerializer.Deserialize<TokenResponse>(l_Body);
            }
            catch (JsonException)
            {
                // A gateway error page rather than an OAuth error body. Say what came back, but
                // never echo the whole thing - the request that produced it carried a secret.
                throw new BizMateAuthException(
                    $"BizMate token endpoint returned {(int)l_Response.StatusCode} with a body that is not JSON: " +
                    Truncate(l_Body));
            }

            if (!l_Response.IsSuccessStatusCode || l_Token is null || string.IsNullOrEmpty(l_Token.AccessToken))
            {
                // RFC 6749 section 5.2 shapes: invalid_client, invalid_scope, unsupported_grant_type,
                // invalid_request. An empty scope intersection is invalid_scope, which is a
                // configuration fault on our side rather than a transient one - do not retry it.
                throw new BizMateAuthException(
                    $"BizMate token request failed: {(int)l_Response.StatusCode} " +
                    $"{l_Token?.Error ?? "unknown_error"} {l_Token?.ErrorDescription}".TrimEnd(),
                    l_Token?.Error);
            }

            // Renew a minute early, and never schedule renewal in the past for a very short token.
            TimeSpan l_Lifetime = TimeSpan.FromSeconds(Math.Max(l_Token.ExpiresIn, 0));
            TimeSpan l_UseFor = l_Lifetime > RenewalMargin ? l_Lifetime - RenewalMargin : l_Lifetime;

            return new CachedToken
            {
                AccessToken = l_Token.AccessToken,
                // BizMate returns what it actually granted after intersecting with the ceiling,
                // which is not necessarily what we asked for.
                Scope = l_Token.Scope ?? string.Empty,
                RenewAt = DateTimeOffset.UtcNow.Add(l_UseFor)
            };
        }

        /// <summary>Drops cached tokens for this credential. Used after a secret rotation.</summary>
        public void InvalidateCache()
        {
            string l_Prefix = string.Concat(_gatewayBaseUrl, "|", _clientId, "|");

            foreach (string l_Key in _tokens.Keys.Where(k => k.StartsWith(l_Prefix, StringComparison.Ordinal)))
            {
                _tokens.TryRemove(l_Key, out _);
            }
        }

        private static string Truncate(string value)
        {
            return value.Length <= 200 ? value : value.Substring(0, 200) + "...";
        }
    }
}
