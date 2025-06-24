using System.Text.Json.Serialization;

namespace Models
{
    public struct Config
    {
        // Server optopns
        [JsonPropertyName("ip")] public required string Ip { get; set; }
        [JsonPropertyName("port")] public required int Port { get; set; }

        // Database options
        [JsonPropertyName("mysql_host")] public required string MySqlHost { get; set; }
        [JsonPropertyName("mysql_port")] public required int MySqlPort { get; set; }
        [JsonPropertyName("mysql_user")] public required string MySqlUser { get; set; }
        [JsonPropertyName("mysql_password")] public required string MySqlPassword { get; set; }
        [JsonPropertyName("mysql_database")] public required string MySqlDatabase { get; set; }
    }
}