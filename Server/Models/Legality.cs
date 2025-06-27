namespace GPSS_Server.Models
{
    using PKHeX.Core;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="LegalityCheckReport" />.
    /// </summary>
    public struct LegalityCheckReport
    {
        /// <summary>
        /// Gets or sets a value indicating whether Legal.
        /// </summary>
        [JsonPropertyName("legal")]
        public bool Legal { get; set; }

        /// <summary>
        /// Gets or sets the Report.
        /// </summary>
        [JsonPropertyName("report")]
        public string[] Report { get; set; }

        /// <summary>
        /// The FromAnalysis.
        /// </summary>
        /// <param name="la">The la<see cref="LegalityAnalysis"/>.</param>
        /// <returns>The <see cref="LegalityCheckReport"/>.</returns>
        public static LegalityCheckReport FromAnalysis(LegalityAnalysis la)
        {
            return new LegalityCheckReport
            {
                Legal = la.Valid,
                Report = la.Report().Split('\n')
            };
        }
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
        public bool Success { get; set; }

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
        /// The FromAnalysis.
        /// </summary>
        /// <param name="la">The la<see cref="LegalityAnalysis"/>.</param>
        /// <param name="pokemon">The pokemon<see cref="PKM?"/>.</param>
        /// <param name="ran">The ran<see cref="bool"/>.</param>
        /// <returns>The <see cref="AutoLegalizationResult"/>.</returns>
        public static AutoLegalizationResult FromAnalysis(LegalityAnalysis la, PKM? pokemon, bool ran)
        {
            return new AutoLegalizationResult
            {
                Legal = la.Valid,
                Success = la.Valid,
                Ran = ran,
                Report = la.Report().Split('\n'),
                PokemonBase64 = pokemon == null
                    ? null
                    : Convert.ToBase64String(
                        pokemon.SIZE_PARTY > pokemon.SIZE_STORED
                            ? pokemon.DecryptedPartyData
                            : pokemon.DecryptedBoxData)
            };
        }
    }
}
