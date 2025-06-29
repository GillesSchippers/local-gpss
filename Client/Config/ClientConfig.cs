namespace GPSS_Client.Config
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Defines the <see cref="ClientConfig" />.
    /// </summary>
    public class ClientConfig
    {
        /// <summary>
        /// Gets or sets the GpssUrl.
        /// </summary>
        [JsonPropertyName("gpss_url")]
        public string GpssUrl { get; set; } = "https://pksm.gustav-serv.net";
    }
}
