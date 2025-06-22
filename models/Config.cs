using System.Text.Json.Serialization;

namespace local_gpss.models;

public struct Config
{
    [JsonPropertyName("ip")] public string Ip { get; set; }
    [JsonPropertyName("port")] public int Port { get; set; }

    // MariaDB options
    [JsonPropertyName("mariadb_host")] public string? MariaDbHost { get; set; }
    [JsonPropertyName("mariadb_port")] public int? MariaDbPort { get; set; }
    [JsonPropertyName("mariadb_user")] public string? MariaDbUser { get; set; }
    [JsonPropertyName("mariadb_password")] public string? MariaDbPassword { get; set; }
    [JsonPropertyName("mariadb_database")] public string? MariaDbDatabase { get; set; }
}