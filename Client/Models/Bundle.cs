namespace GPSS_Client.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="Bundle" />.
    /// </summary>
    public class Bundle
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
    /// Defines the <see cref="BundleResult" />.
    /// </summary>
    public partial class BundleResult : Bundle
    {
        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
