using System.Text.Json.Serialization;

namespace GPSS_Client.Models
{
    public class Bundle
    {
        [JsonPropertyName("pokemons")]
        public List<BundlePokemon> Pokemons { get; set; }
        [JsonPropertyName("download_codes")]
        public List<string> DownloadCodes { get; set; }
        [JsonPropertyName("download_code")]
        public string DownloadCode { get; set; }
        [JsonPropertyName("min_gen")]
        public string MinGen { get; set; }
        [JsonPropertyName("max_gen")]
        public string MaxGen { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("legal")]
        public bool Legality { get; set; }
    }

    public partial class BundleResult : Bundle
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
