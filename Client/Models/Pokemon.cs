namespace GPSS_Client.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="Pokemon" />.
    /// </summary>
    public class Pokemon
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
    /// Defines the <see cref="PokemonResult" />.
    /// </summary>
    public partial class PokemonResult : Pokemon
    {
        /// <summary>
        /// Gets or sets the Error.
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="PokemonInfo" />.
    /// </summary>
    public class PokemonInfo
    {
        /// <summary>
        /// Gets or sets the Species.
        /// </summary>
        public ushort Species { get; set; }

        /// <summary>
        /// Gets or sets the Nickname.
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// Gets or sets the OT.
        /// </summary>
        public string OT { get; set; }

        /// <summary>
        /// Gets or sets the Gender.
        /// </summary>
        public int Gender { get; set; }

        /// <summary>
        /// Gets or sets the Level.
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the Language.
        /// </summary>
        public int Language { get; set; }

        /// <summary>
        /// Gets or sets the Ability.
        /// </summary>
        public int Ability { get; set; }

        /// <summary>
        /// Gets or sets the TID.
        /// </summary>
        public int TID { get; set; }

        /// <summary>
        /// Gets or sets the SID.
        /// </summary>
        public int SID { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsShiny.
        /// </summary>
        public bool IsShiny { get; set; }

        /// <summary>
        /// Gets or sets the Generation.
        /// </summary>
        public byte Generation { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="PokemonInfoDisplay" />.
    /// </summary>
    public partial class PokemonInfoDisplay : PokemonInfo
    {
        /// <summary>
        /// Gets or sets the Species.
        /// </summary>
        public new string Species { get; set; }

        /// <summary>
        /// Gets or sets the Ability.
        /// </summary>
        public new string Ability { get; set; }

        /// <summary>
        /// Gets or sets the Generation.
        /// </summary>
        public new string Generation { get; set; }

        /// <summary>
        /// Gets or sets the Language.
        /// </summary>
        public new string Language { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Legal.
        /// </summary>
        public bool Legal { get; set; }

        /// <summary>
        /// Gets or sets the Code.
        /// </summary>
        public string Code { get; set; }
    }
}
