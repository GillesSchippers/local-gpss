namespace GPSS_Client.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="Upload" />.
    /// </summary>
    public class Upload
    {
        /// <summary>
        /// Gets or sets the Code.
        /// </summary>
        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="UploadResult" />.
    /// </summary>
    public partial class UploadResult : Upload
    {
        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="Search" />.
    /// </summary>
    public class Search
    {
        /// <summary>
        /// Gets or sets the Page.
        /// </summary>
        [JsonPropertyName("page")]
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the Pages.
        /// </summary>
        [JsonPropertyName("pages")]
        public int Pages { get; set; }

        /// <summary>
        /// Gets or sets the Total.
        /// </summary>
        [JsonPropertyName("total")]
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets the Pokemon.
        /// </summary>
        [JsonPropertyName("pokemon")]
        public List<PokemonResult>? Pokemon { get; set; }

        /// <summary>
        /// Gets or sets the Bundles.
        /// </summary>
        [JsonPropertyName("bundles")]
        public List<BundleResult>? Bundles { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="SearchResult" />.
    /// </summary>
    public partial class SearchResult : Search
    {
        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="PokemonDownload" />.
    /// </summary>
    public class PokemonDownload
    {
        /// <summary>
        /// Gets or sets a value indicating whether Legal.
        /// </summary>
        [JsonPropertyName("legal")]
        public bool Legal { get; set; }

        /// <summary>
        /// Gets or sets the Base64.
        /// </summary>
        [JsonPropertyName("base_64")]
        public string Base64 { get; set; }

        /// <summary>
        /// Gets or sets the Code.
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the Generation.
        /// </summary>
        [JsonPropertyName("generation")]
        public string Generation { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="PokemonDownloadResult" />.
    /// </summary>
    public partial class PokemonDownloadResult : PokemonDownload
    {
        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="BundleDownload" />.
    /// </summary>
    public class BundleDownload
    {
        /// <summary>
        /// Gets or sets the Pokemons.
        /// </summary>
        [JsonPropertyName("pokemons")]
        public List<BundlePokemon> Pokemons { get; set; }

        /// <summary>
        /// Gets or sets the DownloadCodes.
        /// </summary>
        [JsonPropertyName("download_codes")]
        public List<string> DownloadCodes { get; set; }

        /// <summary>
        /// Gets or sets the DownloadCode.
        /// </summary>
        [JsonPropertyName("download_code")]
        public string DownloadCode { get; set; }

        /// <summary>
        /// Gets or sets the MinGen.
        /// </summary>
        [JsonPropertyName("min_gen")]
        public string MinGen { get; set; }

        /// <summary>
        /// Gets or sets the MaxGen.
        /// </summary>
        [JsonPropertyName("max_gen")]
        public string MaxGen { get; set; }

        /// <summary>
        /// Gets or sets the Count.
        /// </summary>
        [JsonPropertyName("count")]
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Legality.
        /// </summary>
        [JsonPropertyName("legal")]
        public bool Legality { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="BundleDownloadResult" />.
    /// </summary>
    public partial class BundleDownloadResult : BundleDownload
    {
        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
