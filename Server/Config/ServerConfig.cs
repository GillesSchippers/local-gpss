namespace GPSS_Server.Config
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="ServerConfig" />.
    /// </summary>
    public class ServerConfig
    {
        // Server options

        /// <summary>
        /// Gets or sets the GpssHost.
        /// </summary>
        [JsonPropertyName("gpss_host")]
        public string GpssHost { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the GpssPort.
        /// </summary>
        [JsonPropertyName("gpss_port")]
        public int GpssPort { get; set; } = 8080;

        /// <summary>
        /// Gets or sets a value indicating whether GpssHttp.
        /// </summary>
        [JsonPropertyName("gpss_http")]
        public bool GpssHttp { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether GpssHttps.
        /// </summary>
        [JsonPropertyName("gpss_https")]
        public bool GpssHttps { get; set; } = false;

        /// <summary>
        /// Gets or sets the GpssHttpsCert.
        /// </summary>
        [JsonPropertyName("gpss_https_cert")]
        public string? GpssHttpsCert { get; set; }

        /// <summary>
        /// Gets or sets the GpssHttpsKey.
        /// </summary>
        [JsonPropertyName("gpss_https_key")]
        public string? GpssHttpsKey { get; set; }

        // Database options

        /// <summary>
        /// Gets or sets the MySqlHost.
        /// </summary>
        [JsonPropertyName("mysql_host")]
        public string MySqlHost { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the MySqlPort.
        /// </summary>
        [JsonPropertyName("mysql_port")]
        public int MySqlPort { get; set; } = 3306;

        /// <summary>
        /// Gets or sets the MySqlUser.
        /// </summary>
        [JsonPropertyName("mysql_user")]
        public string MySqlUser { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MySqlPassword.
        /// </summary>
        [JsonPropertyName("mysql_password")]
        public string MySqlPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MySqlDatabase.
        /// </summary>
        [JsonPropertyName("mysql_database")]
        public string MySqlDatabase { get; set; } = "gpss";
    }
}
