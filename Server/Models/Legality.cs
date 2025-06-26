namespace GPSS_Server.Models
{
    using PKHeX.Core;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="LegalityCheckReport" />.
    /// </summary>
    public struct LegalityCheckReport(LegalityAnalysis la)
    {
        /// <summary>
        /// Gets or sets a value indicating whether Legal.
        /// </summary>
        [JsonPropertyName("legal")]
        public bool Legal { get; set; } = la.Valid;

        /// <summary>
        /// Gets or sets the Report.
        /// </summary>
        [JsonPropertyName("report")]
        public string[] Report { get; set; } = la.Report().Split("\n");
    }

    /// <summary>
    /// Defines the <see cref="AutoLegalizationResult" />.
    /// </summary>
    public struct AutoLegalizationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether Legal.
        /// </summary>
        [JsonPropertyName("legal")]
        public bool Legal { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Success.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether Ran.
        /// </summary>
        [JsonPropertyName("ran")]
        public bool Ran { get; set; }

        /// <summary>
        /// Gets or sets the Report.
        /// </summary>
        [JsonPropertyName("report")]
        public string[] Report { get; set; }

        /// <summary>
        /// Gets or sets the PokemonBase64.
        /// </summary>
        [JsonPropertyName("pokemon")]
        public string? PokemonBase64 { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref=""/> class.
        /// </summary>
        /// <param name="la">The la<see cref="LegalityAnalysis"/>.</param>
        /// <param name="pokemon">The pokemon<see cref="PKM?"/>.</param>
        /// <param name="ran">The ran<see cref="bool"/>.</param>
        public AutoLegalizationResult(LegalityAnalysis la, PKM? pokemon, bool ran)
        {
            Legal = la.Valid;
            Report = la.Report().Split("\n");
            Success = la.Valid;
            Ran = ran;

            if (pokemon == null) return;
            PokemonBase64 = Convert.ToBase64String(pokemon.SIZE_PARTY > pokemon.SIZE_STORED
                ? pokemon.DecryptedPartyData
                : pokemon.DecryptedBoxData);
        }
    }
}
