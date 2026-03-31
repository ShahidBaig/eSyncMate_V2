using Newtonsoft.Json;

namespace eSyncMate.Processor.Models
{
    public class LowesPriceImportResponseModel
    {
        [JsonProperty("import_id")]
        public string ImportId { get; set; }
    }
}
