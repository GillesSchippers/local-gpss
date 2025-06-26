using System.Text.Json.Serialization;

namespace GPSS_Client.Models
{
    public class LegalityCheck
    {
        [JsonPropertyName("legal")]
        public bool Legal { get; set; }
        [JsonPropertyName("report")]
        public string[]? Report { get; set; }
    }

    public partial class LegalityCheckResult : LegalityCheck
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class Legalize
    {
        [JsonPropertyName("legal")]
        public bool Legal { get; set; }
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("ran")]
        public bool Ran { get; set; }
        [JsonPropertyName("report")]
        public string[]? Report { get; set; }
        [JsonPropertyName("pokemon")]
        public string? PokemonBase64 { get; set; }
    }

    public partial class LegalizeResult : Legalize
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
