using System.Text.Json.Serialization;

namespace GPSS_Server.Config
{
    public class ServerConfig
    {
        // Server options
        [JsonPropertyName("gpss_host")]
        public string GpssHost { get; set; } = "localhost";
        [JsonPropertyName("gpss_port")]
        public int GpssPort { get; set; } = 8080;
        [JsonPropertyName("gpss_http")]
        public bool GpssHttp { get; set; } = true;
        [JsonPropertyName("gpss_https")]
        public bool GpssHttps { get; set; } = false;
        [JsonPropertyName("gpss_https_cert")]
        public string? GpssHttpsCert { get; set; }
        [JsonPropertyName("gpss_https_key")]
        public string? GpssHttpsKey { get; set; }

        // Database options
        [JsonPropertyName("mysql_host")]
        public string MySqlHost { get; set; } = "localhost";
        [JsonPropertyName("mysql_port")]
        public int MySqlPort { get; set; } = 3306;
        [JsonPropertyName("mysql_user")]
        public string MySqlUser { get; set; } = "gpss";
        [JsonPropertyName("mysql_password")]
        public string MySqlPassword { get; set; } = "";
        [JsonPropertyName("mysql_database")]
        public string MySqlDatabase { get; set; } = "gpss";
    }
}
