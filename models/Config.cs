using System.Text.Json.Serialization;

namespace Models
{
    public struct Config
    {
        // Server optopns
        [JsonPropertyName("ip")] public string Ip { get; set; }
        [JsonPropertyName("port")] public int Port { get; set; }

        // Database options
        [JsonPropertyName("mysql_host")] public string? MySqlHost { get; set; }
        [JsonPropertyName("mysql_port")] public int? MySqlPort { get; set; }
        [JsonPropertyName("mysql_user")] public string? MySqlUser { get; set; }
        [JsonPropertyName("mysql_password")] public string? MySqlPassword { get; set; }
        [JsonPropertyName("mysql_database")] public string? MySqlDatabase { get; set; }
    }
}