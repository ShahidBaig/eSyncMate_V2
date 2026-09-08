using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using eSyncMate.Processor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eSyncMate.Processor.Controllers
{
    /// <summary>
    /// The ESyncMateTraceRecord, exactly as the contract fixes it. Field names are the JSON names
    /// BizMate reads, so they are lowerCamelCase here and must not be "corrected".
    /// </summary>
    public sealed class ESyncMateTraceRecord
    {
        [JsonPropertyName("transmissionReference")] public string? TransmissionReference { get; set; }
        [JsonPropertyName("partnerId")] public string? PartnerId { get; set; }
        [JsonPropertyName("partnerControlNo")] public string? PartnerControlNo { get; set; }
        [JsonPropertyName("direction")] public string? Direction { get; set; }
        [JsonPropertyName("documentType")] public string? DocumentType { get; set; }
        [JsonPropertyName("mapName")] public string? MapName { get; set; }
        [JsonPropertyName("mapVersion")] public string? MapVersion { get; set; }
        [JsonPropertyName("receivedAt")] public string? ReceivedAt { get; set; }
        [JsonPropertyName("translatedAt")] public string? TranslatedAt { get; set; }
        [JsonPropertyName("outcome")] public string? Outcome { get; set; }
        [JsonPropertyName("errorDetail")] public string? ErrorDetail { get; set; }
        [JsonPropertyName("handedOffAt")] public string? HandedOffAt { get; set; }
        [JsonPropertyName("bizmateMessageId")] public long? BizmateMessageId { get; set; }
        [JsonPropertyName("rawArtifactRef")] public string? RawArtifactRef { get; set; }
    }

    /// <summary>
    /// The trace read API BizMate consumes (W2-13, requirement E17).
    /// Contract: M1-Contracts/esyncmate-trace-api.md, contract version 1.0 (2026-09-03).
    ///
    /// Deliberately small: one GET, one record shape, two lookups. It reads VW_EDITrace rather than
    /// re-projecting the ledger, so this endpoint and the read-only-view fallback the contract also
    /// permits (W2-16) can never disagree about what a trace record contains.
    ///
    /// Authentication is a static bearer key eSyncMate issues to BizMate, NOT the JWT scheme the
    /// rest of this service uses - BizMate is a machine caller holding a key in its own
    /// configuration, not a user. Hence AllowAnonymous plus an explicit check.
    ///
    /// {base} for BizMate's ESyncMateTraceUrl setting is this service's root plus /api; BizMate
    /// appends /trace itself.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class TraceController : ControllerBase
    {
        /// <summary>
        /// ApplicationSettings tag holding the key we issue to BizMate. One per environment.
        /// Read through CommonUtils like every other BizMate setting, not from appsettings.json.
        /// </summary>
        public const string ApiKeySetting = "BizMate_TraceApiKey";

        /// <summary>BizMate waits 5 s and then renders the hop as unknown, so never exceed it.</summary>
        private const int QueryTimeoutSeconds = 4;

        private readonly ILogger<TraceController> _logger;

        public TraceController(ILogger<TraceController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] string? transmissionReference,
            [FromQuery] string? partnerId,
            [FromQuery] string? partnerControlNo,
            CancellationToken cancellationToken)
        {
            if (!IsAuthorised())
            {
                return Unauthorized(new { error = "invalid_token", error_description = "A valid trace API key is required." });
            }

            bool l_ByReference = !string.IsNullOrWhiteSpace(transmissionReference);
            bool l_ByPartner = !string.IsNullOrWhiteSpace(partnerId) && !string.IsNullOrWhiteSpace(partnerControlNo);

            if (!l_ByReference && !l_ByPartner)
            {
                // The contract requires partnerId and partnerControlNo together; one alone is not a
                // narrower search, it is an unanswerable one.
                return BadRequest(new
                {
                    error = "invalid_request",
                    error_description =
                        "Supply either transmissionReference, or both partnerId and partnerControlNo."
                });
            }

            try
            {
                ESyncMateTraceRecord? l_Record = l_ByReference
                    ? await ReadByTransmissionReferenceAsync(transmissionReference!, cancellationToken)
                    : await ReadByPartnerControlAsync(partnerId!, partnerControlNo!, cancellationToken);

                if (l_Record is null)
                {
                    return NotFound(new
                    {
                        error = "not_found",
                        error_description = "No eSyncMate record for this document."
                    });
                }

                return Ok(l_Record);
            }
            catch (Exception ex)
            {
                // Any non-404 leaves BizMate showing "eSyncMate trace unavailable" on the hop and
                // changes nothing else, so failing cleanly here is safe for their timeline.
                _logger.LogError(ex, "[TraceController.Get] - Trace lookup failed.");

                return StatusCode(500, new { error = "server_error", error_description = "Trace lookup failed." });
            }
        }

        /// <summary>
        /// Constant-time comparison of the presented key. A trace record names partners, control
        /// numbers and error text, so the key is worth not leaking through timing.
        /// </summary>
        private bool IsAuthorised()
        {
            string? l_Expected = CommonUtils.BizMate_TraceApiKey;

            if (string.IsNullOrWhiteSpace(l_Expected))
            {
                // No key configured means the endpoint is closed, never open. BizMate simply shows
                // "eSyncMate record not connected", which is the documented and harmless state.
                return false;
            }

            string? l_Header = Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(l_Header) ||
                !l_Header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string l_Presented = l_Header.Substring("Bearer ".Length).Trim();

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(l_Presented),
                Encoding.UTF8.GetBytes(l_Expected));
        }

        /// <summary>Lookup 1. Served by UX_EDILedger_TransmissionReference as a covering seek.</summary>
        private Task<ESyncMateTraceRecord?> ReadByTransmissionReferenceAsync(
            string transmissionReference, CancellationToken cancellationToken)
        {
            const string c_Sql =
                "SELECT TOP 1 * FROM [dbo].[VW_EDITrace] WHERE [transmissionReference] = @ref";

            return ReadAsync(c_Sql, cancellationToken,
                new SqlParameter("@ref", SqlDbType.NVarChar, 100) { Value = transmissionReference });
        }

        /// <summary>Lookup 2. Served by IX_EDILedger_PartnerControl as a covering seek.</summary>
        private Task<ESyncMateTraceRecord?> ReadByPartnerControlAsync(
            string partnerId, string partnerControlNo, CancellationToken cancellationToken)
        {
            // Newest first: a partner can reuse a control number across years, and the current
            // document is the one being traced.
            const string c_Sql =
                "SELECT TOP 1 * FROM [dbo].[VW_EDITrace] " +
                "WHERE [partnerId] = @partnerId AND [partnerControlNo] = @controlNo " +
                "ORDER BY [receivedAt] DESC";

            return ReadAsync(c_Sql, cancellationToken,
                new SqlParameter("@partnerId", SqlDbType.NVarChar, 50) { Value = partnerId },
                new SqlParameter("@controlNo", SqlDbType.NVarChar, 50) { Value = partnerControlNo });
        }

        private async Task<ESyncMateTraceRecord?> ReadAsync(
            string sql, CancellationToken cancellationToken, params SqlParameter[] parameters)
        {
            using var l_Connection = new SqlConnection(CommonUtils.ConnectionString);
            using var l_Command = new SqlCommand(sql, l_Connection) { CommandTimeout = QueryTimeoutSeconds };

            l_Command.Parameters.AddRange(parameters);

            await l_Connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using SqlDataReader l_Reader =
                await l_Command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (!await l_Reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            return new ESyncMateTraceRecord
            {
                TransmissionReference = GetString(l_Reader, "transmissionReference"),
                PartnerId = GetString(l_Reader, "partnerId"),
                PartnerControlNo = GetString(l_Reader, "partnerControlNo"),
                Direction = GetString(l_Reader, "direction"),
                DocumentType = GetString(l_Reader, "documentType"),
                MapName = GetString(l_Reader, "mapName"),
                MapVersion = GetString(l_Reader, "mapVersion"),
                ReceivedAt = GetUtc(l_Reader, "receivedAt"),
                TranslatedAt = GetUtc(l_Reader, "translatedAt"),
                Outcome = GetString(l_Reader, "outcome"),
                ErrorDetail = GetString(l_Reader, "errorDetail"),
                HandedOffAt = GetUtc(l_Reader, "handedOffAt"),
                BizmateMessageId = GetLong(l_Reader, "bizmateMessageId"),
                RawArtifactRef = GetString(l_Reader, "rawArtifactRef")
            };
        }

        private static string? GetString(SqlDataReader reader, string column)
        {
            int l_Ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(l_Ordinal) ? null : reader.GetString(l_Ordinal);
        }

        private static long? GetLong(SqlDataReader reader, string column)
        {
            int l_Ordinal = reader.GetOrdinal(column);

            return reader.IsDBNull(l_Ordinal) ? null : reader.GetInt64(l_Ordinal);
        }

        /// <summary>
        /// The ledger stores UTC, and the contract requires ISO-8601 with a Z. Formatting explicitly
        /// rather than letting the serialiser infer a kind is what stops a server-local rendering
        /// reaching BizMate and shifting a timeline by the offset.
        /// </summary>
        private static string? GetUtc(SqlDataReader reader, string column)
        {
            int l_Ordinal = reader.GetOrdinal(column);

            if (reader.IsDBNull(l_Ordinal))
            {
                return null;
            }

            return DateTime.SpecifyKind(reader.GetDateTime(l_Ordinal), DateTimeKind.Utc)
                .ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
    }
}
