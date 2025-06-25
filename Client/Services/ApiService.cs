using GPSS_Client.Config;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GPSS_Client.Services
{
    public class ApiService
    {
        private readonly ClientConfig _config;
        private readonly HttpClient _httpClient;
        private string _baseUrl;

        public ApiService(ClientConfig config)
        {
            _config = config;
            _baseUrl = _config.ApiUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GPSS-Client PKSM/PKHeX");
        }

        public void SetBaseUrl(string url)
        {
            _baseUrl = url.TrimEnd('/');
        }

        // --- LegalityController ---

        public async Task<LegalityCheckResult?> CheckLegalityAsync(Stream pkmnFile, string generation)
        {
            var url = $"{_baseUrl}/api/v2/pksm/legality";
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(pkmnFile);
            content.Add(fileContent, "pkmn", "pkmn");
            content.Headers.Add("generation", generation);

            Debug.WriteLine($"[Request] POST {url} | Generation: {generation}");

            var response = await _httpClient.PostAsync(url, content);
            var json = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[Response] {url} | Status: {response.StatusCode} | Body: {json}");

            return JsonSerializer.Deserialize<LegalityCheckResult>(json);
        }

        public async Task<LegalizeResult?> LegalizeAsync(Stream pkmnFile, string generation, string version)
        {
            var url = $"{_baseUrl}/api/v2/pksm/legalize";
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(pkmnFile);
            content.Add(fileContent, "pkmn", "pkmn");
            content.Headers.Add("generation", generation);
            content.Headers.Add("version", version);

            Debug.WriteLine($"[Request] POST {url} | Generation: {generation}, Version: {version}");

            var response = await _httpClient.PostAsync(url, content);
            var json = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[Response] {url} | Status: {response.StatusCode} | Body: {json}");

            return JsonSerializer.Deserialize<LegalizeResult>(json);
        }

        // --- GpssController ---

        public async Task<SearchResult?> SearchAsync(string entityType, object? searchBody = null, int page = 1, int amount = 30)
        {
            var url = $"{_baseUrl}/api/v2/gpss/search/{entityType}?page={page}&amount={amount}";
            var json = searchBody != null ? JsonSerializer.Serialize(searchBody) : "{}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Debug.WriteLine($"[Request] POST {url} | Page: {page}, Amount: {amount}, Body: {json}");

            var response = await _httpClient.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[Response] {url} | Status: {response.StatusCode} | Body: {resultJson}");

            return JsonSerializer.Deserialize<SearchResult>(resultJson);
        }

        public async Task<UploadResult?> UploadPokemonAsync(Stream pkmnFile, string generation)
        {
            var url = $"{_baseUrl}/api/v2/gpss/upload/pokemon";
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(pkmnFile);
            content.Add(fileContent, "pkmn", "pkmn");
            content.Headers.Add("generation", generation);

            Debug.WriteLine($"[Request] POST {url} | Generation: {generation}");

            var response = await _httpClient.PostAsync(url, content);
            var json = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[Response] {url} | Status: {response.StatusCode} | Body: {json}");

            return JsonSerializer.Deserialize<UploadResult>(json);
        }

        public async Task<UploadResult?> UploadBundleAsync(List<Stream> pkmnFiles, List<string> generations)
        {
            var url = $"{_baseUrl}/api/v2/gpss/upload/bundle";
            var content = new MultipartFormDataContent();
            content.Headers.Add("count", pkmnFiles.Count.ToString());
            content.Headers.Add("generations", string.Join(",", generations));

            for (int i = 0; i < pkmnFiles.Count; i++)
            {
                var fileContent = new StreamContent(pkmnFiles[i]);
                content.Add(fileContent, $"pkmn{i + 1}", $"pkmn{i + 1}");
            }

            Debug.WriteLine($"[Request] POST {url} | Count: {pkmnFiles.Count}, Generations: {string.Join(",", generations)}");

            var response = await _httpClient.PostAsync(url, content);
            var json = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[Response] {url} | Status: {response.StatusCode} | Body: {json}");

            return JsonSerializer.Deserialize<UploadResult>(json);
        }

        public async Task<PokemonDownloadResult?> DownloadPokemonAsync(string code, bool download = true)
        {
            var url = $"{_baseUrl}/api/v2/gpss/download/pokemon/{code}?download={download.ToString().ToLower()}";
            Debug.WriteLine($"[Request] GET {url} | Code: {code}, Download: {download}");

            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[Response] {url} | Status: {response.StatusCode} | Body: {json}");

            return JsonSerializer.Deserialize<PokemonDownloadResult>(json);
        }

        public async Task<BundleDownloadResult?> DownloadBundleAsync(string code, bool download = true)
        {
            var url = $"{_baseUrl}/api/v2/gpss/download/bundle/{code}?download={download.ToString().ToLower()}";
            Debug.WriteLine($"[Request] GET {url} | Code: {code}, Download: {download}");

            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"[Response] {url} | Status: {response.StatusCode} | Body: {json}");

            return JsonSerializer.Deserialize<BundleDownloadResult>(json);
        }
    }

    // DTOs for deserialization (simplified, expand as needed)
    public class LegalityCheckResult
    {
        [JsonPropertyName("legal")]
        public bool Legal { get; set; }
        [JsonPropertyName("report")]
        public string[]? Report { get; set; }
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class LegalizeResult
    {
        [JsonPropertyName("legal")]
        public bool Legal { get; set; }
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("ran")]
        public bool Ran { get; set; }
        [JsonPropertyName("report")]
        public string[]? Report { get; set; }
        [JsonPropertyName("pokemon")]
        public string? Pokemon { get; set; }
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class UploadResult
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class SearchResult
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }
        [JsonPropertyName("pages")]
        public int Pages { get; set; }
        [JsonPropertyName("total")]
        public int Total { get; set; }
        [JsonPropertyName("pokemon")]
        public List<PokemonResult>? Pokemon { get; set; }
        [JsonPropertyName("bundles")]
        public List<BundleResult>? Bundles { get; set; }
    }

    public class PokemonResult
    {
        [JsonPropertyName("legal")]
        public bool Legal { get; set; }
        [JsonPropertyName("base_64")]
        public string Base_64 { get; set; }
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("generation")]
        public string Generation { get; set; }
    }

    public class BundleResult
    {
        [JsonPropertyName("pokemons")]
        public List<PokemonResult> Pokemons { get; set; }
        [JsonPropertyName("download_codes")]
        public List<string> DownloadCodes { get; set; }
        [JsonPropertyName("download_code")]
        public string DownloadCode { get; set; }
        [JsonPropertyName("min_gen")]
        public string MinGen { get; set; }
        [JsonPropertyName("max_gen")]
        public string MaxGen { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("legal")]
        public bool Legality { get; set; }
    }

    public class PokemonDownloadResult
    {
        [JsonPropertyName("legal")]
        public bool Legal { get; set; }
        [JsonPropertyName("base_64")]
        public string Base_64 { get; set; }
        [JsonPropertyName("code")]
        public string Code { get; set; }
        [JsonPropertyName("generation")]
        public string Generation { get; set; }
    }

    public class BundleDownloadResult
    {
        [JsonPropertyName("pokemons")]
        public List<PokemonResult> Pokemons { get; set; }
        [JsonPropertyName("download_codes")]
        public List<string> DownloadCodes { get; set; }
        [JsonPropertyName("download_code")]
        public string DownloadCode { get; set; }
        [JsonPropertyName("min_gen")]
        public string MinGen { get; set; }
        [JsonPropertyName("max_gen")]
        public string MaxGen { get; set; }
        [JsonPropertyName("count")]
        public int Count { get; set; }
        [JsonPropertyName("legal")]
        public bool Legality { get; set; }
    }
}