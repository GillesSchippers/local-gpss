using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GPSS_Client.Config;

namespace GPSS_Client.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private string _baseUrl;

        public ApiService(ClientConfig config)
        {
            _baseUrl = config.ApiUrl.TrimEnd('/');
            _httpClient = new HttpClient();
        }

        public void SetBaseUrl(string url)
        {
            _baseUrl = url.TrimEnd('/');
        }

        // --- LegalityController ---

        public async Task<LegalityCheckResult?> CheckLegalityAsync(Stream pkmnFile, string generation)
        {
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(pkmnFile);
            content.Add(fileContent, "pkmn", "pkmn");
            content.Headers.Add("generation", generation);

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/v2/pksm/legality", content);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LegalityCheckResult>(json);
        }

        public async Task<LegalizeResult?> LegalizeAsync(Stream pkmnFile, string generation, string version)
        {
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(pkmnFile);
            content.Add(fileContent, "pkmn", "pkmn");
            content.Headers.Add("generation", generation);
            content.Headers.Add("version", version);

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/v2/pksm/legalize", content);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LegalizeResult>(json);
        }

        // --- GpssController ---

        public async Task<SearchResult?> SearchAsync(string entityType, object? searchBody = null, int page = 1, int amount = 30)
        {
            var url = $"{_baseUrl}/api/v2/gpss/search/{entityType}?page={page}&amount={amount}";
            var json = searchBody != null ? JsonSerializer.Serialize(searchBody) : "";
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var resultJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SearchResult>(resultJson);
        }

        public async Task<UploadResult?> UploadPokemonAsync(Stream pkmnFile, string generation)
        {
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(pkmnFile);
            content.Add(fileContent, "pkmn", "pkmn");
            content.Headers.Add("generation", generation);

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/v2/gpss/upload/pokemon", content);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UploadResult>(json);
        }

        public async Task<UploadResult?> UploadBundleAsync(List<Stream> pkmnFiles, List<string> generations)
        {
            var content = new MultipartFormDataContent();
            content.Headers.Add("count", pkmnFiles.Count.ToString());
            content.Headers.Add("generations", string.Join(",", generations));

            for (int i = 0; i < pkmnFiles.Count; i++)
            {
                var fileContent = new StreamContent(pkmnFiles[i]);
                content.Add(fileContent, $"pkmn{i + 1}", $"pkmn{i + 1}");
            }

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/v2/gpss/upload/bundle", content);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UploadResult>(json);
        }

        public async Task<PokemonDownloadResult?> DownloadPokemonAsync(string code, bool download = true)
        {
            var url = $"{_baseUrl}/api/v2/gpss/download/pokemon/{code}?download={download.ToString().ToLower()}";
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PokemonDownloadResult>(json);
        }

        public async Task<BundleDownloadResult?> DownloadBundleAsync(string code, bool download = true)
        {
            var url = $"{_baseUrl}/api/v2/gpss/download/bundle/{code}?download={download.ToString().ToLower()}";
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BundleDownloadResult>(json);
        }
    }

    // DTOs for deserialization (simplified, expand as needed)
    public class LegalityCheckResult
    {
        public bool Legal { get; set; }
        public string[]? Report { get; set; }
        public string? Error { get; set; }
    }

    public class LegalizeResult
    {
        public bool Legal { get; set; }
        public bool Success { get; set; }
        public bool Ran { get; set; }
        public string[]? Report { get; set; }
        public string? Pokemon { get; set; }
        public string? Error { get; set; }
    }

    public class UploadResult
    {
        public string? Code { get; set; }
        public string? Error { get; set; }
    }

    public class SearchResult
    {
        public int Page { get; set; }
        public int Pages { get; set; }
        public int Total { get; set; }
        public List<PokemonResult>? Pokemon { get; set; }
        public List<BundleResult>? Bundles { get; set; }
    }

    public class PokemonResult
    {
        public bool Legal { get; set; }
        public string Base_64 { get; set; }
        public string Code { get; set; }
        public string Generation { get; set; }
    }

    public class BundleResult
    {
        public List<PokemonResult> Pokemons { get; set; }
        public List<string> DownloadCodes { get; set; }
        public string DownloadCode { get; set; }
        public string MinGen { get; set; }
        public string MaxGen { get; set; }
        public int Count { get; set; }
        public bool Legality { get; set; }
    }

    public class PokemonDownloadResult
    {
        public bool Legal { get; set; }
        public string Base_64 { get; set; }
        public string Code { get; set; }
        public string Generation { get; set; }
    }

    public class BundleDownloadResult
    {
        public List<PokemonResult> Pokemons { get; set; }
        public List<string> DownloadCodes { get; set; }
        public string DownloadCode { get; set; }
        public string MinGen { get; set; }
        public string MaxGen { get; set; }
        public int Count { get; set; }
        public bool Legality { get; set; }
    }
}