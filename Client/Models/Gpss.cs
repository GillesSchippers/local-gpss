using System.Text.Json.Serialization;

namespace GPSS_Client.Models
{
    public class Upload
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }
    public partial class UploadResult : Upload
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class Search
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }
        [JsonPropertyName("pages")]
        public int Pages { get; set; }
        [JsonPropertyName("total")]
        public int Total { get; set; }
        [JsonPropertyName("pokemon")]
        public List<PokemonResult>? Pokemon { get; set; }
        [JsonPropertyName("bundles")]
        public List<BundleResult>? Bundles { get; set; }
    }

    public partial class SearchResult : Search
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class PokemonDownload
    {
        [JsonPropertyName("legal")]
        public bool Legal { get; set; }
        [JsonPropertyName("base_64")]
        public string Base64 { get; set; }
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("generation")]
        public string Generation { get; set; }
    }

    public partial class PokemonDownloadResult : PokemonDownload
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class BundleDownload
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

    public partial class BundleDownloadResult : BundleDownload
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
