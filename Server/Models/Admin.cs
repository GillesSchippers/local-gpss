namespace GPSS_Server.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="Metrics" />.
    /// </summary>
    public struct Metrics
    {
        /// <summary>
        /// Gets or sets the PokemonCount.
        /// </summary>
        [JsonPropertyName("pokemons")]
        public int PokemonCount { get; set; }

        /// <summary>
        /// Gets or sets the BundleCount.
        /// </summary>
        [JsonPropertyName("bundles")]
        public int BundleCount { get; set; }
    }
}
