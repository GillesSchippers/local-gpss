namespace GPSS_Client.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="BundlePokemon" />.
    /// </summary>
    public class BundlePokemon
    {
        /// <summary>
        /// Gets or sets a value indicating whether Legal.
        /// </summary>
        [JsonPropertyName("legality")]
        public bool Legal { get; set; }

        /// <summary>
        /// Gets or sets the Base64.
        /// </summary>
        [JsonPropertyName("base_64")]
        public string Base64 { get; set; }

        /// <summary>
        /// Gets or sets the Generation.
        /// </summary>
        [JsonPropertyName("generation")]
        public string Generation { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="BundlePokemonResult" />.
    /// </summary>
    public partial class BundlePokemonResult : BundlePokemon
    {
        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
