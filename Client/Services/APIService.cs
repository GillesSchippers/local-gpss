namespace GPSS_Client.Services
{
    using GPSS_Client.Config;
    using GPSS_Client.Models;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text.Json;

    /// <summary>
    /// Defines the <see cref="APIService" />.
    /// </summary>
    public partial class APIService : APIRecords
    {
        /// <summary>
        /// Defines the Client.
        /// </summary>
        private readonly HttpClient Client;

        /// <summary>
        /// Defines the Config.
        /// </summary>
        private readonly ConfigHolder Config;

        /// <summary>
        /// Defines the UserAgentName.
        /// </summary>
        private const string UserAgentName = "PKHeX-GPSS";

        /// <summary>
        /// Defines the UserAgentVersion.
        /// </summary>
        private const string UserAgentVersion = "2.0";

        /// <summary>
        /// Initializes a new instance of the <see cref="APIService"/> class.
        /// </summary>
        /// <param name="configHolder">The configHolder<see cref="ConfigHolder"/>.</param>
        public APIService(ConfigHolder configHolder)
        {
            Config = configHolder;
            Client = new HttpClient();
            Client.DefaultRequestHeaders.UserAgent.Clear();
            Client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(UserAgentName, UserAgentVersion));
            Client.BaseAddress = new Uri(Config.Get(config => config.GpssUrl).TrimEnd('/'));

            // Subscribe to config changes
            Config.ConfigChanged += OnConfigChanged;
        }

        /// <summary>
        /// The OnConfigChanged.
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/>.</param>
        /// <param name="_">The _<see cref="ConfigChangedEventArgs"/>.</param>
        private void OnConfigChanged(object? sender, ConfigChangedEventArgs _)
        {
            Client.BaseAddress = new Uri(Config.Get(config => config.GpssUrl).TrimEnd('/'));
        }

        // --- API Connectors ---

        /// <summary>
        /// The SearchAsync.
        /// </summary>
        /// <param name="entityType">The entityType<see cref="string"/>.</param>
        /// <param name="search">The search<see cref="Search"/>.</param>
        /// <param name="page">The page<see cref="int"/>.</param>
        /// <param name="amount">The amount<see cref="int"/>.</param>
        /// <returns>The <see cref="Task{JsonElement?}"/>.</returns>
        public async Task<JsonElement?> SearchAsync(string entityType, Search search, int page = 1, int amount = 30)
        {
            var response = await Client.PostAsJsonAsync($"/api/v2/gpss/search/{entityType}?page={page}&amount={amount}", search);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }

        /// <summary>
        /// The DownloadPokemonAsync.
        /// </summary>
        /// <param name="code">The code<see cref="string"/>.</param>
        /// <param name="download">The download<see cref="bool"/>.</param>
        /// <returns>The <see cref="Task{GpssPokemon?}"/>.</returns>
        public async Task<GpssPokemon?> DownloadPokemonAsync(string code, bool download = true)
        {
            var response = await Client.GetAsync($"/api/v2/gpss/download/pokemon/{code}?download={download.ToString().ToLower()}");
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<GpssPokemon>();
        }

        /// <summary>
        /// The DownloadBundleAsync.
        /// </summary>
        /// <param name="code">The code<see cref="string"/>.</param>
        /// <param name="download">The download<see cref="bool"/>.</param>
        /// <returns>The <see cref="Task{GpssBundle?}"/>.</returns>
        public async Task<GpssBundle?> DownloadBundleAsync(string code, bool download = true)
        {
            var response = await Client.GetAsync($"/api/v2/gpss/download/bundle/{code}?download={download.ToString().ToLower()}");
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<GpssBundle>();
        }

        /// <summary>
        /// The UploadPokemonAsync.
        /// </summary>
        /// <param name="pkmData">The pkmData<see cref="byte[]"/>.</param>
        /// <param name="generation">The generation<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{string?}"/>.</returns>
        public async Task<string?> UploadPokemonAsync(byte[] pkmData, string generation)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(pkmData), "pkmn", $"pokemon.pk{generation}");
            content.Headers.Add("generation", generation);

            var response = await Client.PostAsync("/api/v2/gpss/upload/pokemon", content);
            if (!response.IsSuccessStatusCode)
                return null;
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : null;
        }

        /// <summary>
        /// The UploadBundleAsync.
        /// </summary>
        /// <param name="pokemons">The pokemons<see cref="List{(byte[] pkmData, string Generation)}"/>.</param>
        /// <returns>The <see cref="Task{string?}"/>.</returns>
        public async Task<string?> UploadBundleAsync(List<(byte[] pkmData, string Generation)> pokemons)
        {
            using var content = new MultipartFormDataContent();
            content.Headers.Add("count", pokemons.Count.ToString());
            content.Headers.Add("generations", string.Join(",", pokemons.ConvertAll(p => p.Generation)));
            for (int i = 0; i < pokemons.Count; i++)
            {
                content.Add(new ByteArrayContent(pokemons[i].pkmData), $"pkmn{i + 1}", $"pokemon{i + 1}.pk{pokemons[i].Generation}");
            }

            var response = await Client.PostAsync("/api/v2/gpss/upload/bundle", content);
            if (!response.IsSuccessStatusCode)
                return null;
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : null;
        }
    }
}
