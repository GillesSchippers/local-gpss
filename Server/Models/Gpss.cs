namespace GPSS_Server.Models
{
    using MySqlConnector;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="GpssPokemon" />.
    /// </summary>
    public struct GpssPokemon(MySqlDataReader? reader)
    {
        /// <summary>
        /// Gets or sets a value indicating whether Legal.
        /// </summary>
        [JsonPropertyName("legal")]
        public bool Legal { get; set; } = reader?.GetBoolean(reader.GetOrdinal("legal")) ?? false;

        /// <summary>
        /// Gets or sets the Base64.
        /// </summary>
        [JsonPropertyName("base_64")]
        public string Base64 { get; set; } = reader?.GetString(reader.GetOrdinal("base_64")) ?? "";

        /// <summary>
        /// Gets or sets the DownloadCode.
        /// </summary>
        [JsonPropertyName("code")]
        public string DownloadCode { get; set; } = reader?.GetString(reader.GetOrdinal("download_code")) ?? "";

        /// <summary>
        /// Gets or sets the Generation.
        /// </summary>
        [JsonPropertyName("generation")]
        public string Generation { get; set; } = reader?.GetString(reader.GetOrdinal("generation")) ?? "";
    }

    /// <summary>
    /// Defines the <see cref="GpssBundlePokemon" />.
    /// </summary>
    public struct GpssBundlePokemon()
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
    /// Defines the <see cref="GpssBundle" />.
    /// </summary>
    public struct GpssBundle()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref=""/> class.
        /// </summary>
        /// <param name="bundlePokemons">The bundlePokemons<see cref="List{GpssBundlePokemon}"/>.</param>
        /// <param name="downloadCodes">The downloadCodes<see cref="List{string}"/>.</param>
        /// <param name="data">The data<see cref="Dictionary{string, dynamic}"/>.</param>
        public GpssBundle(List<GpssBundlePokemon> bundlePokemons, List<string> downloadCodes, Dictionary<string, dynamic> data) : this()
        {
            Pokemons = bundlePokemons;
            DownloadCodes = downloadCodes;
            Legality = data["legal"];
            Count = bundlePokemons.Count;
            MaxGen = data["max_gen"];
            MinGen = data["min_gen"];
            DownloadCode = data["download_code"];
        }

        /// <summary>
        /// Gets or sets the Pokemons.
        /// </summary>
        [JsonPropertyName("pokemons")]
        public List<GpssBundlePokemon> Pokemons { get; set; }

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
    /// Defines the <see cref="Search" />.
    /// </summary>
    public struct Search
    {
        /// <summary>
        /// Initializes a new instance of the <see cref=""/> class.
        /// </summary>
        public Search()
        {
        }

        /// <summary>
        /// Gets or sets the Generations.
        /// </summary>
        public List<string>? Generations { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether LegalOnly.
        /// </summary>
        public bool LegalOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether SortDirection.
        /// </summary>
        public bool SortDirection { get; set; } = false;

        /// <summary>
        /// Gets or sets the SortField.
        /// </summary>
        public string SortField { get; set; } = "upload_datetime";

        /// <summary>
        /// Gets or sets the DownloadCode.
        /// </summary>
        public string? DownloadCode { get; set; } = null;
    }
}
