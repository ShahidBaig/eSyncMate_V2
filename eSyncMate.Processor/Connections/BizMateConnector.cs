using System.Net;
using System.Text;
using System.Text.Json;
using eSyncMate.Processor.Models;

namespace eSyncMate.Processor.Connections
{
    /// <summary>
    /// Base for every BizMate call failure. The subclasses are the error taxonomy (W1-17, E22):
    /// what a caller does about a failure is decided by its type, never by re-reading a status code.
    /// </summary>
    /// <summary>
    /// What was being sent when a call failed - everything the store-and-forward queue needs to send
    /// it again, and nothing else.
    ///
    /// It rides on the exception so a caller can queue a failed call without knowing any paths or
    /// scopes. That knowledge belongs to the connector, and duplicating it at each call site is how
    /// a retry ends up going somewhere slightly different from the original.
    /// </summary>
    public sealed record BizMateCall(
        HttpMethod Method,
        string PublicPath,
        string? PayloadJson,
        string Scope,
        string CorrelationId,
        string? UrlPathWithQuery);

    public abstract class BizMateException : Exception
    {
        protected BizMateException(string message, Exception? inner = null) : base(message, inner) { }

        /// <summary>May the same request be sent again on a schedule?</summary>
        public abstract bool IsRetryable { get; }

        /// <summary>Does this failure mean the DOCUMENT failed, or only that the attempt did?</summary>
        public abstract bool IsDocumentFailure { get; }

        /// <summary>The call that failed, stamped by the connector. Null if it never got that far.</summary>
        public BizMateCall? Call { get; internal set; }
    }

    /// <summary>Transport fault or 5xx. Retry with backoff; the document is fine.</summary>
    public sealed class BizMateTransportException : BizMateException
    {
        public HttpStatusCode? StatusCode { get; }

        public BizMateTransportException(string message, HttpStatusCode? status = null, Exception? inner = null)
            : base(message, inner) => StatusCode = status;

        public override bool IsRetryable => true;
        public override bool IsDocumentFailure => false;
    }

    /// <summary>
    /// 429. Back off and try again later. Explicitly NOT a document failure: the document was never
    /// read, so it is not a translation outcome (M3 and W6-09).
    /// </summary>
    public sealed class BizMateRateLimitedException : BizMateException
    {
        public TimeSpan? RetryAfter { get; }

        public BizMateRateLimitedException(string message, TimeSpan? retryAfter)
            : base(message) => RetryAfter = retryAfter;

        public override bool IsRetryable => true;
        public override bool IsDocumentFailure => false;
    }

    /// <summary>
    /// 403. A governance decision, not a transient fault - a missing scope, a suspended party, an
    /// ungranted operation. Stop, record it, tell a person. Never retry on a schedule (EQ-05).
    /// </summary>
    public sealed class BizMateRefusedException : BizMateException
    {
        public string? RefusalCode { get; }

        public BizMateRefusedException(string message, string? refusalCode)
            : base(message) => RefusalCode = refusalCode;

        public override bool IsRetryable => false;
        public override bool IsDocumentFailure => true;
    }

    /// <summary>4xx other than 403 and 429. The request is wrong; sending it again will not help.</summary>
    public sealed class BizMateRequestException : BizMateException
    {
        public HttpStatusCode StatusCode { get; }
        public string? ErrorCode { get; }

        public BizMateRequestException(string message, HttpStatusCode status, string? errorCode)
            : base(message)
        {
            StatusCode = status;
            ErrorCode = errorCode;
        }

        public override bool IsRetryable => false;
        public override bool IsDocumentFailure => true;
    }

    /// <summary>
    /// The single delivery pipeline into BizMate (W1-01, requirements E9 and E1).
    ///
    /// Every route hands documents to this and nothing calls BizMate directly, so the route into
    /// BizMate never varies by carrier. One place acquires the token, one place signs, one place
    /// classifies failures.
    ///
    /// Contract: M1-Contracts/openapi.yaml and auth-and-signing.md, contract version 1.0.
    /// </summary>
    public sealed class BizMateConnector
    {
        private const string HttpClientKey = "bizmate";
        public const string CorrelationHeader = "X-Correlation-Id";

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        private readonly string _gatewayBaseUrl;
        private readonly string _signingSecret;
        private readonly BizMateTokenClient _tokens;

        public BizMateConnector(string gatewayBaseUrl, string clientId, string clientSecret, string signingSecret)
        {
            if (string.IsNullOrWhiteSpace(signingSecret))
            {
                throw new ArgumentException("The BizMate signing secret is required.", nameof(signingSecret));
            }

            _gatewayBaseUrl = (gatewayBaseUrl ?? string.Empty).TrimEnd('/');
            _signingSecret = signingSecret;
            _tokens = new BizMateTokenClient(gatewayBaseUrl!, clientId, clientSecret);
        }

        // -----------------------------------------------------------------------------------
        // Endpoints
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// Registers the original artifact (W1-06, E10). This is always the FIRST call for a
        /// document - the ledger-first ordering rule (W2-02) is what makes a document that dies in
        /// translation still die visibly.
        /// </summary>
        public Task<RegisterRawFileResponse> RegisterRawAsync(
            RegisterRawFileRequest request, string correlationId, CancellationToken cancellationToken = default)
        {
            // Always send the hash. BizMate refuses a mismatch, which turns silent corruption in
            // transit into a loud failure at the boundary.
            request.ContentHash ??= BizMateRequestSigner.HashBody(request.Content);

            return SendAsync<RegisterRawFileRequest, RegisterRawFileResponse>(
                HttpMethod.Post, "/v1/integration/raw", request, BizMateScopes.Inbound, correlationId, cancellationToken);
        }

        /// <summary>Delivers a translated canonical document (W1-07, E9).</summary>
        public Task<InboundDocumentResponse> PostInboundAsync(
            string docType, InboundDocumentRequest request, string correlationId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(docType))
            {
                throw new ArgumentException("A document type is required.", nameof(docType));
            }

            if (request.RawFileRef <= 0)
            {
                // The contract makes rawFileRef mandatory; failing here rather than at BizMate keeps
                // the ledger-first violation local and legible.
                throw new InvalidOperationException(
                    "rawFileRef is mandatory: register the artifact with POST /raw before delivering the document (W2-02).");
            }

            return SendAsync<InboundDocumentRequest, InboundDocumentResponse>(
                HttpMethod.Post, $"/v1/integration/inbound/{docType}", request,
                BizMateScopes.Inbound, correlationId, cancellationToken);
        }

        /// <summary>Correlates a 997/CONTRL to BizMate's outbound message (W1-12, E11).</summary>
        public Task<AcknowledgementResponse> PostAckAsync(
            AcknowledgementRequest request, string correlationId, CancellationToken cancellationToken = default)
        {
            return SendAsync<AcknowledgementRequest, AcknowledgementResponse>(
                HttpMethod.Post, "/v1/integration/ack", request, BizMateScopes.Ack, correlationId, cancellationToken);
        }

        /// <summary>
        /// Collects staged documents from the outbox (W1-08, E9).
        ///
        /// channel defaults to EDI on BizMate's side. A customer configured on BOTH channels must
        /// also be polled with channel=API or through the doorway, or its documents sit unseen.
        /// </summary>
        public Task<OutboundPage> GetOutboundPendingAsync(
            string correlationId,
            string? partnerId = null,
            string? docType = null,
            string? family = null,
            string? channel = null,
            int? wait = null,
            bool redeliver = false,
            CancellationToken cancellationToken = default)
        {
            var l_Query = new List<string>();

            if (!string.IsNullOrWhiteSpace(partnerId)) l_Query.Add("partnerId=" + Uri.EscapeDataString(partnerId));
            if (!string.IsNullOrWhiteSpace(docType)) l_Query.Add("docType=" + Uri.EscapeDataString(docType));
            if (!string.IsNullOrWhiteSpace(family)) l_Query.Add("family=" + Uri.EscapeDataString(family));
            if (!string.IsNullOrWhiteSpace(channel)) l_Query.Add("channel=" + Uri.EscapeDataString(channel));
            if (wait.HasValue) l_Query.Add("wait=" + Math.Clamp(wait.Value, 1, 30));
            if (redeliver) l_Query.Add("redeliver=true");

            string l_Path = "/v1/integration/outbound/pending";
            string l_Url = l_Query.Count == 0 ? l_Path : l_Path + "?" + string.Join('&', l_Query);

            // The query string is not signed (auth-and-signing.md section 3), so the signed path
            // stays the bare one while the request goes to the full URL.
            //
            // The response is an object - { items, total } - not a bare array. Deserialising it as
            // a List threw "The JSON value could not be converted" on the first live call, which is
            // the sort of thing only a real endpoint tells you.
            return SendAsync<object, OutboundPage>(
                HttpMethod.Get, l_Path, null, BizMateScopes.OutboundRead, correlationId, cancellationToken, l_Url);
        }

        /// <summary>Marks a staged document collected but not yet confirmed (W1-10).</summary>
        public Task<StatusResponse> MarkFetchedAsync(
            long messageId, string correlationId, CancellationToken cancellationToken = default)
        {
            return SendAsync<object, StatusResponse>(
                HttpMethod.Post, $"/v1/integration/outbound/{messageId}/fetched", null,
                BizMateScopes.OutboundRead, correlationId, cancellationToken);
        }

        /// <summary>
        /// Confirms transmission to the partner and starts the acknowledgement clock (W1-10).
        /// transmissionReference is how BizMate finds our record for this document, so it is required.
        /// </summary>
        public Task<StatusResponse> MarkDeliveredAsync(
            long messageId, string transmissionReference, string correlationId,
            DateTimeOffset? deliveredAt = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(transmissionReference))
            {
                throw new ArgumentException(
                    "transmissionReference is required: it is how BizMate finds the eSyncMate record for this document.",
                    nameof(transmissionReference));
            }

            var l_Body = new DeliveredRequest
            {
                TransmissionReference = transmissionReference,
                DeliveredAt = deliveredAt
            };

            return SendAsync<DeliveredRequest, StatusResponse>(
                HttpMethod.Post, $"/v1/integration/outbound/{messageId}/delivered", l_Body,
                BizMateScopes.OutboundRead, correlationId, cancellationToken);
        }

        /// <summary>Reports a partner's connection state (W2-17, E18). An unknown partner answers 404.</summary>
        public Task<PartnerStateResponse> PostPartnerStateAsync(
            PartnerStateRequest request, string correlationId, CancellationToken cancellationToken = default)
        {
            return SendAsync<PartnerStateRequest, PartnerStateResponse>(
                HttpMethod.Post, "/v1/integration/partner-state", request,
                BizMateScopes.Inbound, correlationId, cancellationToken);
        }

        /// <summary>
        /// Reads the resolved configuration for one customer (W1-13, E21). BizMate is the authority;
        /// callers honour cacheTtlSeconds and keep no local copy of enablement.
        /// </summary>
        public Task<CustomerIntegrationConfig> GetConfigAsync(
            long customerNo, string correlationId, CancellationToken cancellationToken = default)
        {
            return SendAsync<object, CustomerIntegrationConfig>(
                HttpMethod.Get, $"/v1/integration/config/{customerNo}", null,
                BizMateScopes.ConfigRead, correlationId, cancellationToken);
        }

        // -----------------------------------------------------------------------------------
        // The pipeline
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// Acquire a scoped token, sign the public path, send, classify the outcome. Every call
        /// above goes through here so none of those four steps can be forgotten or done differently.
        /// </summary>
        private async Task<TResponse> SendAsync<TRequest, TResponse>(
            HttpMethod method,
            string publicPath,
            TRequest? body,
            string scope,
            string correlationId,
            CancellationToken cancellationToken,
            string? urlPathWithQuery = null)
        {
            string? l_Payload = body is null ? null : JsonSerializer.Serialize(body, _json);

            string l_Content = await SendRawAsync(
                method, publicPath, l_Payload, scope, correlationId, urlPathWithQuery, cancellationToken)
                .ConfigureAwait(false);

            if (typeof(TResponse) == typeof(string))
            {
                return (TResponse)(object)l_Content;
            }

            // 204, or an empty 200 on a confirmation endpoint.
            if (string.IsNullOrWhiteSpace(l_Content))
            {
                return Activator.CreateInstance<TResponse>();
            }

            try
            {
                TResponse? l_Result = JsonSerializer.Deserialize<TResponse>(l_Content, _json);

                return l_Result ?? Activator.CreateInstance<TResponse>();
            }
            catch (JsonException ex)
            {
                throw new BizMateTransportException(
                    $"BizMate returned a body that did not parse: {method.Method} {publicPath}", null, ex);
            }
        }

        /// <summary>
        /// Sends a call whose body is already serialised, and returns the raw response.
        ///
        /// This is what the store-and-forward queue replays with (W1-14): a queued call holds the
        /// body it was built with, and replaying it must reproduce that call byte for byte rather
        /// than round-trip it through a model that might serialise differently on a later version.
        /// </summary>
        public async Task<string> SendRawAsync(
            HttpMethod method,
            string publicPath,
            string? payloadJson,
            string scope,
            string correlationId,
            string? urlPathWithQuery = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                throw new ArgumentException(
                    "An X-Correlation-Id is required on every call: it is the join key in BizMate's Document Trace (X-04).",
                    nameof(correlationId));
            }

            // Every BizMateException leaving this method carries what was being sent, so a caller
            // that wants to queue the call for retry does not have to reconstruct it (W1-14).
            try
            {
                return await SendOnceAsync(
                    method, publicPath, payloadJson, scope, correlationId, urlPathWithQuery, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (BizMateException ex)
            {
                ex.Call ??= new BizMateCall(method, publicPath, payloadJson, scope, correlationId, urlPathWithQuery);

                throw;
            }
        }

        private async Task<string> SendOnceAsync(
            HttpMethod method,
            string publicPath,
            string? payloadJson,
            string scope,
            string correlationId,
            string? urlPathWithQuery,
            CancellationToken cancellationToken)
        {
            bool l_HasBody = payloadJson is not null;

            byte[] l_Body = l_HasBody
                ? Encoding.UTF8.GetBytes(payloadJson!)
                : Array.Empty<byte>();

            // A token fetch that cannot reach the gateway throws HttpRequestException from inside
            // the token client, which is outside this connector's taxonomy entirely: it would sail
            // past every `catch (BizMateException)` in the pipelines, so the document neither failed
            // visibly nor got queued - it just threw. Brought inside the taxonomy here, where the
            // rest of the transport story already lives.
            string l_Token;

            try
            {
                l_Token = await _tokens.GetTokenAsync(scope, cancellationToken).ConfigureAwait(false);
            }
            catch (BizMateException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BizMateTransportException(
                    $"Could not obtain a BizMate token for scope [{scope}]: {ex.Message}", null, ex);
            }

            // Sign the PUBLIC path, never the URL with its query string. BizMateRequestSigner
            // refuses a Kong-stripped path outright, so this cannot silently go wrong.
            (long l_Timestamp, string l_Signature) =
                BizMateRequestSigner.Sign(method.Method, publicPath, l_Body, _signingSecret);

            // Blank counts as absent, not as a request for the empty path. A queued call reads its
            // urlPathWithQuery back out of the database, and the entity layer turns a NULL column
            // into an empty string - so `?? publicPath` alone sent every replayed call to the
            // gateway root, where Kong answered "no Route matched with those values" and the queue
            // recorded a 404 that had nothing to do with the call it was replaying.
            string l_RequestPath = string.IsNullOrWhiteSpace(urlPathWithQuery) ? publicPath : urlPathWithQuery!;

            using var l_Request = new HttpRequestMessage(method, _gatewayBaseUrl + l_RequestPath);

            l_Request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + l_Token);
            l_Request.Headers.TryAddWithoutValidation(BizMateRequestSigner.TimestampHeader, l_Timestamp.ToString());
            l_Request.Headers.TryAddWithoutValidation(BizMateRequestSigner.SignatureHeader, l_Signature);
            l_Request.Headers.TryAddWithoutValidation(CorrelationHeader, correlationId);
            l_Request.Headers.TryAddWithoutValidation("Accept", "application/json");

            if (l_HasBody)
            {
                l_Request.Content = new ByteArrayContent(l_Body);
                l_Request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            }

            HttpClient l_Client = SharedHttpClientFactory.GetOrCreateClient(HttpClientKey);
            HttpResponseMessage l_Response;

            try
            {
                l_Response = await l_Client.SendAsync(l_Request, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new BizMateTransportException($"BizMate call timed out: {method.Method} {publicPath}");
            }
            catch (HttpRequestException ex)
            {
                throw new BizMateTransportException(
                    $"BizMate call could not be sent: {method.Method} {publicPath}", null, ex);
            }

            using (l_Response)
            {
                string l_Content = await l_Response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (l_Response.IsSuccessStatusCode)
                {
                    return l_Content;
                }

                throw Classify(l_Response, l_Content, method, publicPath);
            }
        }

        /// <summary>
        /// Turns a non-success response into the right kind of exception (W1-17). The distinction
        /// that matters most: 429 backs off without counting as a document failure, while 403 stops
        /// and is never retried on a schedule.
        /// </summary>
        private static BizMateException Classify(
            HttpResponseMessage response, string content, HttpMethod method, string publicPath)
        {
            string? l_Error = TryReadErrorCode(content);
            string l_Where = $"{method.Method} {publicPath}";
            int l_Status = (int)response.StatusCode;

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                TimeSpan? l_RetryAfter = response.Headers.RetryAfter?.Delta
                    ?? (response.Headers.RetryAfter?.Date is DateTimeOffset d
                        ? d - DateTimeOffset.UtcNow
                        : (TimeSpan?)null);

                return new BizMateRateLimitedException(
                    $"BizMate rate-limited {l_Where}. The document was not read, so this is not a translation outcome.",
                    l_RetryAfter);
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return new BizMateRefusedException(
                    $"BizMate refused {l_Where}: {l_Error ?? "forbidden"}. This is a governance decision - " +
                    "stop, record it, and tell whoever operates this traffic. Do not retry on a schedule.",
                    l_Error);
            }

            if (l_Status >= 500)
            {
                return new BizMateTransportException(
                    $"BizMate returned {l_Status} on {l_Where}: {Truncate(content)}", response.StatusCode);
            }

            return new BizMateRequestException(
                $"BizMate rejected {l_Where} with {l_Status} {l_Error}: {Truncate(content)}",
                response.StatusCode, l_Error);
        }

        private static string? TryReadErrorCode(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            try
            {
                using JsonDocument l_Doc = JsonDocument.Parse(content);

                return l_Doc.RootElement.TryGetProperty("error", out JsonElement l_Error)
                    ? l_Error.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string Truncate(string value)
        {
            return value.Length <= 500 ? value : value.Substring(0, 500) + "...";
        }
    }
}
