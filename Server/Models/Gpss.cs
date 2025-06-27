namespace GPSS_Server.Models
{
    using MySqlConnector;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="GpssPokemon" />.
    /// </summary>
    public struct GpssPokemon
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
        /// Gets or sets the DownloadCode.
        /// </summary>
        [JsonPropertyName("code")]
        public string DownloadCode { get; set; }

        /// <summary>
        /// Gets or sets the Generation.
        /// </summary>
        [JsonPropertyName("generation")]
        public string Generation { get; set; }

        /// <summary>
        /// The FromReader.
        /// </summary>
        /// <param name="reader">The reader<see cref="MySqlDataReader"/>.</param>
        /// <returns>The <see cref="GpssPokemon"/>.</returns>
        public static GpssPokemon FromReader(MySqlDataReader reader)
        {
            return new GpssPokemon
            {
                Legal = reader.GetBoolean(reader.GetOrdinal("legal")),
                Base64 = reader.GetString(reader.GetOrdinal("base_64")),
                DownloadCode = reader.GetString(reader.GetOrdinal("download_code")),
                Generation = reader.GetString(reader.GetOrdinal("generation"))
            };
        }
    }

    /// <summary>
    /// Defines the <see cref="GpssBundlePokemon" />.
    /// </summary>
    public struct GpssBundlePokemon
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
    public struct GpssBundle
    {
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

        /// <summary>
        /// The Create.
        /// </summary>
        /// <param name="pokemons">The pokemons<see cref="List{GpssBundlePokemon}"/>.</param>
        /// <param name="downloadCodes">The downloadCodes<see cref="List{string}"/>.</param>
        /// <param name="downloadCode">The downloadCode<see cref="string"/>.</param>
        /// <param name="minGen">The minGen<see cref="string"/>.</param>
        /// <param name="maxGen">The maxGen<see cref="string"/>.</param>
        /// <param name="legality">The legality<see cref="bool"/>.</param>
        /// <returns>The <see cref="GpssBundle"/>.</returns>
        public static GpssBundle Create(
            List<GpssBundlePokemon> pokemons,
            List<string> downloadCodes,
            string downloadCode,
            string minGen,
            string maxGen,
            bool legality)
        {
            return new GpssBundle
            {
                Pokemons = pokemons,
                DownloadCodes = downloadCodes,
                DownloadCode = downloadCode,
                MinGen = minGen,
                MaxGen = maxGen,
                Count = pokemons?.Count ?? 0,
                Legality = legality
            };
        }
    }

    /// <summary>
    /// Defines the <see cref="Search" />.
    /// </summary>
    public struct Search
    {
        /// <summary>
        /// Gets or sets the Generations.
        /// </summary>
        public List<string>? Generations { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether LegalOnly.
        /// </summary>
        public bool LegalOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SortDirection.
        /// </summary>
        public bool SortDirection { get; set; }

        /// <summary>
        /// Gets or sets the SortField.
        /// </summary>
        public string SortField { get; set; }

        /// <summary>
        /// Gets or sets the DownloadCode.
        /// </summary>
        public string? DownloadCode { get; set; }

        /// <summary>
        /// The Create.
        /// </summary>
        /// <param name="generations">The generations<see cref="List{string}?"/>.</param>
        /// <param name="legalOnly">The legalOnly<see cref="bool"/>.</param>
        /// <param name="sortDirection">The sortDirection<see cref="bool"/>.</param>
        /// <param name="sortField">The sortField<see cref="string"/>.</param>
        /// <param name="downloadCode">The downloadCode<see cref="string?"/>.</param>
        /// <returns>The <see cref="Search"/>.</returns>
        public static Search Create(
            List<string>? generations = null,
            bool legalOnly = false,
            bool sortDirection = false,
            string sortField = "upload_date_time",
            string? downloadCode = null)
        {
            return new Search
            {
                Generations = generations,
                LegalOnly = legalOnly,
                SortDirection = sortDirection,
                SortField = sortField,
                DownloadCode = downloadCode
            };
        }
    }
}
