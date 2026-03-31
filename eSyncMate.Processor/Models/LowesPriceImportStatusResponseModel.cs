using Newtonsoft.Json;

namespace eSyncMate.Processor.Models
{
    public class LowesPriceImportStatusResponseModel
    {
        [JsonProperty("data")]
        public List<PriceImportData> Data { get; set; } = new();
    }

    public class PriceImportData
    {
        [JsonProperty("import_id")]
        public string ImportId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("reason_status")]
        public string ReasonStatus { get; set; }

        [JsonProperty("date_created")]
        public string DateCreated { get; set; }

        [JsonProperty("lines_in_success")]
        public int LinesInSuccess { get; set; }

        [JsonProperty("lines_in_error")]
        public int LinesInError { get; set; }

        [JsonProperty("offers_updated")]
        public int OffersUpdated { get; set; }

        [JsonProperty("offers_in_error")]
        public int OffersInError { get; set; }

        [JsonProperty("has_error_report")]
        public bool HasErrorReport { get; set; }
    }
}
