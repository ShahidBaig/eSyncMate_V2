using Newtonsoft.Json;

namespace eSyncMate.Processor.Models
{
    public class KnotPriceImportResponseModel
    {
        [JsonProperty("import_id")]
        public string ImportId { get; set; }
    }
}
