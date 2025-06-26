using System.Text.Json.Serialization;

namespace GPSS_Client.Models
{
    public class BundlePokemon
    {
        [JsonPropertyName("legality")]
        public bool Legal { get; set; }
        [JsonPropertyName("base_64")]
        public string Base64 { get; set; }
        [JsonPropertyName("generation")]
        public string Generation { get; set; }
    }
    public partial class BundlePokemonResult : BundlePokemon
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
