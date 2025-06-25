using System.Text.Json.Serialization;

namespace GPSS_Client.Config
{
    public class ClientConfig
    {
        [JsonPropertyName("api_url")]
        public string ApiUrl { get; set; } = "http://127.0.0.1:8080";
    }
}