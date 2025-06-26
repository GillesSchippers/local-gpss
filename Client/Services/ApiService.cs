using GPSS_Client.Config;
using GPSS_Client.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace GPSS_Client.Services
{
    public class ApiService
    {
        private readonly ConfigHolder _configHolder;
        private readonly ILogger<ApiService> _logger;
        private readonly HttpClient _httpClient;
        private ClientConfig _config;

        public ApiService(ConfigHolder configHolder, ILogger<ApiService> logger)
        {
            _configHolder = configHolder;
            _config = _configHolder.Config;
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GPSS-Client PKSM/PKHeX");
#if DEBUG
            _httpClient.BaseAddress = new Uri("http://localhost:8080");
#else
            _httpClient.BaseAddress = new Uri(_config.ApiUrl.TrimEnd('/'));
#endif

            // Subscribe to config changes
            _configHolder.ConfigChanged += OnConfigChanged;
        }

        private void OnConfigChanged(object? sender, EventArgs e)
        {
            _config = _configHolder.Config;
            _httpClient.BaseAddress = new Uri(_config.ApiUrl.TrimEnd('/'));
        }

        public async Task<LegalityCheckResult?> CheckLegalityAsync(Stream pkmnFile, string generation)
        {
            try
            {
                var url = "/api/v2/pksm/legality";
                var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(pkmnFile);
                content.Add(fileContent, "pkmn", "pkmn");
                content.Headers.Add("generation", generation);

                _logger.LogInformation("POST {Url} | Generation: {Generation}", url, generation);

                var response = await _httpClient.PostAsync(url, content);
                var json = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response {Url} | Status: {StatusCode} | Body: {Body}", url, response.StatusCode, json);

                return JsonSerializer.Deserialize<LegalityCheckResult>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CheckLegalityAsync failed: {Message}", ex.Message);
                return new LegalityCheckResult { Error = "Could not connect to server." };
            }
        }

        public async Task<LegalizeResult?> LegalizeAsync(Stream pkmnFile, string generation, string version)
        {
            try
            {
                var url = "/api/v2/pksm/legalize";
                var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(pkmnFile);
                content.Add(fileContent, "pkmn", "pkmn");
                content.Headers.Add("generation", generation);
                content.Headers.Add("version", version);

                _logger.LogInformation("POST {Url} | Generation: {Generation}, Version: {Version}", url, generation, version);

                var response = await _httpClient.PostAsync(url, content);
                var json = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response {Url} | Status: {StatusCode} | Body: {Body}", url, response.StatusCode, json);

                return JsonSerializer.Deserialize<LegalizeResult>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LegalizeAsync failed: {Message}", ex.Message);
                return new LegalizeResult { Error = "Could not connect to server." };
            }
        }

        public async Task<SearchResult?> SearchAsync(string entityType, object? searchBody = null, int page = 1, int amount = 30)
        {
            try
            {
                var url = $"/api/v2/gpss/search/{entityType}?page={page}&amount={amount}";
                var json = searchBody != null ? JsonSerializer.Serialize(searchBody) : "{}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("POST {Url} | Page: {Page}, Amount: {Amount}, Body: {Body}", url, page, amount, json);

                var response = await _httpClient.PostAsync(url, content);
                var resultJson = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response {Url} | Status: {StatusCode} | Body: {Body}", url, response.StatusCode, resultJson);

                return JsonSerializer.Deserialize<SearchResult>(resultJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchAsync failed: {Message}", ex.Message);
                return new SearchResult { Error = "Could not connect to server." };
            }
        }

        public async Task<UploadResult?> UploadPokemonAsync(Stream pkmnFile, string generation)
        {
            try
            {
                var url = "/api/v2/gpss/upload/pokemon";
                var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(pkmnFile);
                content.Add(fileContent, "pkmn", "pkmn");
                content.Headers.Add("generation", generation);

                _logger.LogInformation("POST {Url} | Generation: {Generation}", url, generation);

                var response = await _httpClient.PostAsync(url, content);
                var json = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response {Url} | Status: {StatusCode} | Body: {Body}", url, response.StatusCode, json);

                return JsonSerializer.Deserialize<UploadResult>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadPokemonAsync failed: {Message}", ex.Message);
                return new UploadResult { Error = "Could not connect to server." };
            }
        }

        public async Task<UploadResult?> UploadBundleAsync(List<Stream> pkmnFiles, List<string> generations)
        {
            try
            {
                var url = "/api/v2/gpss/upload/bundle";
                var content = new MultipartFormDataContent();
                content.Headers.Add("count", pkmnFiles.Count.ToString());
                content.Headers.Add("generations", string.Join(",", generations));

                for (int i = 0; i < pkmnFiles.Count; i++)
                {
                    var fileContent = new StreamContent(pkmnFiles[i]);
                    content.Add(fileContent, $"pkmn{i + 1}", $"pkmn{i + 1}");
                }

                _logger.LogInformation("POST {Url} | Count: {Count}, Generations: {Generations}", url, pkmnFiles.Count, string.Join(",", generations));

                var response = await _httpClient.PostAsync(url, content);
                var json = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response {Url} | Status: {StatusCode} | Body: {Body}", url, response.StatusCode, json);

                return JsonSerializer.Deserialize<UploadResult>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadBundleAsync failed: {Message}", ex.Message);
                return new UploadResult { Error = "Could not connect to server." };
            }
        }

        public async Task<PokemonDownloadResult?> DownloadPokemonAsync(string code, bool download = true)
        {
            try
            {
                var url = $"/api/v2/gpss/download/pokemon/{code}?download={download.ToString().ToLower()}";
                _logger.LogInformation("GET {Url} | Code: {Code}, Download: {Download}", url, code, download);

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response {Url} | Status: {StatusCode} | Body: {Body}", url, response.StatusCode, json);

                return JsonSerializer.Deserialize<PokemonDownloadResult>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownloadPokemonAsync failed: {Message}", ex.Message);
                return new PokemonDownloadResult { Error = "Could not connect to server." };
            }
        }

        public async Task<BundleDownloadResult?> DownloadBundleAsync(string code, bool download = true)
        {
            try
            {
                var url = $"/api/v2/gpss/download/bundle/{code}?download={download.ToString().ToLower()}";
                _logger.LogInformation("GET {Url} | Code: {Code}, Download: {Download}", url, code, download);

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response {Url} | Status: {StatusCode} | Body: {Body}", url, response.StatusCode, json);

                return JsonSerializer.Deserialize<BundleDownloadResult>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DownloadBundleAsync failed: {Message}", ex.Message);
                return new BundleDownloadResult { Error = "Could not connect to server." };
            }
        }
    }
}