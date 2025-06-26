namespace GPSS_Client.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="LegalityCheck" />.
    /// </summary>
    public class LegalityCheck
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
        public string[]? Report { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="LegalityCheckResult" />.
    /// </summary>
    public partial class LegalityCheckResult : LegalityCheck
    {
        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="Legalize" />.
    /// </summary>
    public class Legalize
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
        public string[]? Report { get; set; }

        /// <summary>
        /// Gets or sets the PokemonBase64.
        /// </summary>
        [JsonPropertyName("pokemon")]
        public string? PokemonBase64 { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="LegalizeResult" />.
    /// </summary>
    public partial class LegalizeResult : Legalize
    {
        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
