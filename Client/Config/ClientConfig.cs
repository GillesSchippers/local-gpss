namespace GPSS_Client.Config
{
    using System.ComponentModel;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="ClientConfig" />.
    /// </summary>
    public class ClientConfig
    {
        /// <summary>
        /// Gets or sets the ApiUrl.
        /// </summary>
        [JsonPropertyName("api_url")]
        [DisplayName("API URL")]
        public string ApiUrl { get; set; } = "https://pksm.gustav-serv.net";
    }
}
