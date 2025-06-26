namespace GPSS_Client
{
    using CommunityToolkit.Maui.Storage;
    using GPSS_Client.Config;
    using GPSS_Client.Models;
    using GPSS_Client.Services;
    using GPSS_Client.Utils;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Defines the <see cref="MainPage" />.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        /// <summary>
        /// Defines the _configHolder.
        /// </summary>
        private readonly ConfigHolder _configHolder;

        /// <summary>
        /// Defines the _logger.
        /// </summary>
        private readonly ILogger<MainPage> _logger;

        /// <summary>
        /// Defines the _api.
        /// </summary>
        private readonly ApiService _api;

        /// <summary>
        /// Defines the _config.
        /// </summary>
        private ClientConfig _config;

        /// <summary>
        /// Defines the currentPage.
        /// </summary>
        private int currentPage = 1;

        /// <summary>
        /// Defines the pageSize.
        /// </summary>
        private const int pageSize = 30;

        /// <summary>
        /// Defines the PkFileType.
        /// </summary>
        private static readonly FilePickerFileType PkFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>> // Future proofing for new platforms
        {
            { DevicePlatform.WinUI, new[] { ".pk1", ".pk2", ".pk3", ".pk4", ".pk5", ".pk6", ".pk7", ".pk8" } },
            { DevicePlatform.macOS, new[] { ".pk1", ".pk2", ".pk3", ".pk4", ".pk5", ".pk6", ".pk7", ".pk8" } },
            { DevicePlatform.iOS, new[] { "public.data" } }, // iOS does not support extension filtering, but you can filter after selection
            { DevicePlatform.Android, new[] { "application/octet-stream" } }, // Android: filter after selection
        });

        /// <summary>
        /// Defines the PkPickOptions.
        /// </summary>
        private static readonly PickOptions PkPickOptions = new()
        {
            PickerTitle = "Select Pokémon PKM file(s)",
            FileTypes = PkFileType
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        /// <param name="configHolder">The configHolder<see cref="ConfigHolder"/>.</param>
        /// <param name="api">The api<see cref="ApiService"/>.</param>
        /// <param name="logger">The logger<see cref="ILogger{MainPage}"/>.</param>
        public MainPage(ConfigHolder configHolder, ApiService api, ILogger<MainPage> logger)
        {
            InitializeComponent();
            _configHolder = configHolder;
            _config = _configHolder.Config;
            _logger = logger;
            _api = api;

            // Subscribe to config changes
            _configHolder.ConfigChanged += OnConfigChanged;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            SearchAsync();
        }

        /// <summary>
        /// The OnConfigChanged.
        /// </summary>
        /// <param name="sender">The sender<see cref="object?"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private void OnConfigChanged(object? sender, EventArgs e)
        {
            _config = _configHolder.Config;
        }

        /// <summary>
        /// The OnUploadPokemonClicked.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private async void OnUploadPokemonClicked(object sender, EventArgs e)
        {
            try
            {
                _logger.LogDebug("Upload Pokémon button clicked.");
                var files = await FilePicker.PickMultipleAsync(PkPickOptions);
                if (files == null || !files.Any())
                {
                    _logger.LogInformation("No files selected for upload.");
                    return;
                }

                // Filter for .pk* files in case the platform doesn't filter
                files = [.. files.Where(f => Path.GetExtension(f.FileName).StartsWith(".pk", StringComparison.InvariantCultureIgnoreCase))];

                if (!files.Any())
                {
                    _logger.LogError("No valid Pokémon files selected for upload.");
                    await ShowAlert("Error", "No valid Pokémon files selected.", "OK");
                    return;
                }

                if (files.Count() > 6)
                {
                    _logger.LogError("More than 6 files selected for upload.");
                    await ShowAlert("Error", "Select up to 6 files.", "OK");
                    return;
                }

                var gens = new List<string>();
                var streams = new List<Stream>();
                foreach (var file in files)
                {
                    var gen = Helpers.GetGenerationFromFilename(file.FileName);
                    if (gen == null)
                    {
                        _logger.LogError("Could not determine generation for file: {FileName}", file.FileName);
                        await ShowAlert("Error", $"Could not determine generation for {file.FileName}", "OK");
                        return;
                    }
                    gens.Add(gen);
                    streams.Add(await file.OpenReadAsync());
                }

                if (files.Count() == 1)
                {
                    _logger.LogInformation("Uploading single Pokémon file: {FileName}, Generation: {Generation}", files.First().FileName, gens[0]);
                    var result = await _api.UploadPokemonAsync(streams[0], gens[0]);
                    if (!string.IsNullOrEmpty(result?.Error))
                    {
                        _logger.LogError("Upload failed: {Error}", result.Error);
                        await ShowAlert("Upload Failed", result.Error, "OK");
                    }
                    else if (!string.IsNullOrEmpty(result?.Code))
                    {
                        _logger.LogInformation("Upload successful. Code: {Code}", result.Code);
                        await ShowAlert("Upload", $"Pokémon uploaded! Code: {result.Code}", "OK");
                    }
                    else
                    {
                        _logger.LogError("Upload failed: Unknown error.");
                        await ShowAlert("Upload Failed", "Unknown error", "OK");
                    }
                }
                else
                {
                    _logger.LogInformation("Uploading bundle of {Count} Pokémon files.", files.Count());
                    var result = await _api.UploadBundleAsync(streams, gens);
                    if (!string.IsNullOrEmpty(result?.Error))
                    {
                        _logger.LogError("Bundle upload failed: {Error}", result.Error);
                        await ShowAlert("Upload Failed", result.Error, "OK");
                    }
                    else if (!string.IsNullOrEmpty(result?.Code))
                    {
                        _logger.LogInformation("Bundle upload successful. Code: {Code}", result.Code);
                        await ShowAlert("Upload", $"Bundle uploaded! Code: {result.Code}", "OK");
                    }
                    else
                    {
                        _logger.LogError("Bundle upload failed: Unknown error.");
                        await ShowAlert("Upload Failed", "Unknown error", "OK");
                    }
                }

                await SearchAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Pokémon upload.");
                await ShowAlert("Error", "An unexpected error occurred during upload.", "OK");
            }
        }

        /// <summary>
        /// The OnCheckLegalityClicked.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private async void OnCheckLegalityClicked(object sender, EventArgs e)
        {
            try
            {
                _logger.LogDebug("Check Legality button clicked.");
                var files = await FilePicker.PickMultipleAsync(PkPickOptions);
                if (files == null || !files.Any())
                {
                    _logger.LogInformation("No files selected for legality check.");
                    return;
                }

                files = [.. files.Where(f => Path.GetExtension(f.FileName).StartsWith(".pk", StringComparison.InvariantCultureIgnoreCase))];

                if (!files.Any())
                {
                    _logger.LogError("No valid Pokémon files selected for legality check.");
                    await ShowAlert("Error", "No valid Pokémon files selected.", "OK");
                    return;
                }

                if (files.Count() > 6)
                {
                    _logger.LogError("More than 6 files selected for legality check.");
                    await ShowAlert("Error", "Select up to 6 files.", "OK");
                    return;
                }

                var results = new List<string>();
                foreach (var file in files)
                {
                    var gen = Helpers.GetGenerationFromFilename(file.FileName);
                    if (gen == null)
                    {
                        _logger.LogError("Could not determine generation for file: {FileName}", file.FileName);
                        results.Add($"{file.FileName}: Could not determine generation.");
                        continue;
                    }

                    using var stream = await file.OpenReadAsync();
                    _logger.LogInformation("Checking legality for file: {FileName}, Generation: {Generation}", file.FileName, gen);
                    var result = await _api.CheckLegalityAsync(stream, gen);
                    if (result == null)
                    {
                        _logger.LogError("No result from API for file: {FileName}", file.FileName);
                        results.Add($"{file.FileName}: No result from API.");
                    }
                    else if (!string.IsNullOrEmpty(result.Error))
                    {
                        _logger.LogError("Error from API for file: {FileName}: {Error}", file.FileName, result.Error);
                        results.Add($"{file.FileName}: Error: {result.Error}");
                    }
                    else if (result.Legal)
                    {
                        results.Add($"{file.FileName}: Pokémon is legal.");
                    }
                    else if (result.Report != null && result.Report.Length > 0)
                    {
                        results.Add($"{file.FileName}: {string.Join("; ", result.Report)}");
                    }
                    else
                    {
                        _logger.LogError("Pokémon is not legal and no details available for file: {FileName}", file.FileName);
                        results.Add($"{file.FileName}: Pokémon is not legal. No details available.");
                    }
                }

                await ShowAlert("Legality Results", string.Join("\n\n", results), "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during legality check.");
                await ShowAlert("Error", "An unexpected error occurred during legality check.", "OK");
            }
        }

        /// <summary>
        /// The OnLegalizePokemonClicked.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private async void OnLegalizePokemonClicked(object sender, EventArgs e)
        {
            try
            {
                _logger.LogDebug("Legalize Pokémon button clicked.");
                var files = await FilePicker.PickMultipleAsync(PkPickOptions);
                if (files == null || !files.Any())
                {
                    _logger.LogInformation("No files selected for legalization.");
                    return;
                }

                files = [.. files.Where(f => Path.GetExtension(f.FileName).StartsWith(".pk", StringComparison.InvariantCultureIgnoreCase))];

                if (!files.Any())
                {
                    _logger.LogError("No valid Pokémon files selected for legalization.");
                    await ShowAlert("Error", "No valid Pokémon files selected.", "OK");
                    return;
                }

                if (files.Count() > 6)
                {
                    _logger.LogError("More than 6 files selected for legalization.");
                    await ShowAlert("Error", "Select up to 6 files.", "OK");
                    return;
                }

                var results = new List<string>();
                foreach (var file in files)
                {
                    var gen = Helpers.GetGenerationFromFilename(file.FileName);
                    if (gen == null)
                    {
                        _logger.LogError("Could not determine generation for file: {FileName}", file.FileName);
                        results.Add($"{file.FileName}: Could not determine generation.");
                        continue;
                    }

                    using var stream = await file.OpenReadAsync();
                    _logger.LogInformation("Legalizing file: {FileName}, Generation: {Generation}", file.FileName, gen);
                    var result = await _api.LegalizeAsync(stream, gen, "Any");
                    if (result == null || !string.IsNullOrEmpty(result.Error))
                    {
                        _logger.LogError("Legalization failed for file: {FileName}", file.FileName);
                        results.Add($"{file.FileName}: Legalization failed.");
                    }
                    else if (result.Legal && !result.Ran)
                    {
                        results.Add($"{file.FileName}: Pokémon is already legal.");
                    }
                    else if (!string.IsNullOrEmpty(result.PokemonBase64))
                    {
                        var save = await ShowAlert("Legalize", $"{file.FileName}: Legalized! Save to file?", "Yes", "No");
                        if (save)
                        {
                            var bytes = Convert.FromBase64String(result.PokemonBase64);
                            var dest = await FileSaver.Default.SaveAsync(file.FileName, new MemoryStream(bytes));
                            if (dest == null || !dest.IsSuccessful)
                            {
                                _logger.LogError("Failed to save legalized Pokémon for file: {FileName}", file.FileName);
                                results.Add($"{file.FileName}: Failed to save legalized Pokémon.");
                                continue;
                            }
                            _logger.LogInformation("Legalized Pokémon saved to {FilePath} for file: {FileName}", dest.FilePath, file.FileName);
                            results.Add($"{file.FileName}: Saved to {dest?.FilePath}");
                        }
                        else
                        {
                            _logger.LogInformation("Legalized Pokémon not saved for file: {FileName}", file.FileName);
                            results.Add($"{file.FileName}: Legalized, not saved.");
                        }
                    }
                    else
                    {
                        _logger.LogError("Legalization did not produce a file for: {FileName}", file.FileName);
                        results.Add($"{file.FileName}: Legalization did not produce a file.");
                    }
                }

                await ShowAlert("Legalize Results", string.Join("\n\n", results), "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Pokémon legalization.");
                await ShowAlert("Error", "An unexpected error occurred during legalization.", "OK");
            }
        }

        /// <summary>
        /// The OnNextPageClicked.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private async void OnNextPageClicked(object sender, EventArgs e)
        {
            _logger.LogDebug("Next page button clicked. Current page: {CurrentPage}", currentPage);
            int nextPage = currentPage + 1;
            var result = await _api.SearchAsync("pokemon", null, nextPage, pageSize);
            if (result == null)
            {
                _logger.LogError("Failed to fetch next page: API returned null.");
                await ShowAlert("Error", "Could not connect to server.", "OK");
                return;
            }
            if (!string.IsNullOrEmpty(result.Error))
            {
                _logger.LogError("Error fetching next page: {Error}", result.Error);
                await ShowAlert("Error", result.Error, "OK");
                return;
            }
            if (result.Pokemon != null && result.Pokemon.Count > 0)
            {
                currentPage = nextPage;
                ResultsView.ItemsSource = result.Pokemon;
                PageLabel.Text = $"Page {currentPage}";
                _logger.LogInformation("Page changed to {CurrentPage}", currentPage);
            }
            else
            {
                _logger.LogInformation("No more Pokémon found on next page.");
            }
        }

        /// <summary>
        /// The OnPreviousPageClicked.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private async void OnPreviousPageClicked(object sender, EventArgs e)
        {
            _logger.LogDebug("Previous page button clicked. Current page: {CurrentPage}", currentPage);
            if (currentPage > 1)
            {
                int prevPage = currentPage - 1;
                var result = await _api.SearchAsync("pokemon", null, prevPage, pageSize);
                if (result == null)
                {
                    _logger.LogError("Failed to fetch previous page: API returned null.");
                    await ShowAlert("Error", "Could not connect to server.", "OK");
                    return;
                }
                if (!string.IsNullOrEmpty(result.Error))
                {
                    _logger.LogError("Error fetching previous page: {Error}", result.Error);
                    await ShowAlert("Error", result.Error, "OK");
                    return;
                }
                if (result.Pokemon != null && result.Pokemon.Count > 0)
                {
                    currentPage = prevPage;
                    ResultsView.ItemsSource = result.Pokemon;
                    PageLabel.Text = $"Page {currentPage}";
                    _logger.LogInformation("Page changed to {CurrentPage}", currentPage);
                }
                else
                {
                    _logger.LogInformation("No more Pokémon found on previous page.");
                }
            }
            else
            {
                _logger.LogInformation("Already at the first page, cannot go back further.");
            }
        }

        /// <summary>
        /// The OnDownloadSinglePokemonClicked.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private async void OnDownloadSinglePokemonClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is PokemonInfoDisplay poke)
            {
                _logger.LogDebug("Download button clicked for Pokémon with code: {Code}", poke.Code);
                var result = await _api.DownloadPokemonAsync(poke.Code.ToString());
                if (result == null)
                {
                    _logger.LogError("Failed to download Pokémon: API returned null for code {Code}", poke.Code);
                    await ShowAlert("Download", "Could not connect to server.", "OK");
                    return;
                }
                if (!string.IsNullOrEmpty(result.Error))
                {
                    _logger.LogError("Error downloading Pokémon with code {Code}: {Error}", poke.Code, result.Error);
                    await ShowAlert("Download", result.Error, "OK");
                    return;
                }
                if (string.IsNullOrEmpty(result.Base64))
                {
                    _logger.LogError("Download failed: No Base64 data for Pokémon with code {Code}", poke.Code);
                    await ShowAlert("Download", "Failed to download.", "OK");
                    return;
                }
                var bytes = Convert.FromBase64String(result.Base64);
                var fileName = $"{poke.Code}.pk{poke.Generation}";
                var dest = await FileSaver.Default.SaveAsync(fileName, new MemoryStream(bytes));
                if (dest == null || !dest.IsSuccessful)
                {
                    _logger.LogError("Failed to save downloaded Pokémon to file for code {Code}", poke.Code);
                    return;
                }
                _logger.LogInformation("Pokémon with code {Code} saved to {FilePath}", poke.Code, dest.FilePath);
                await ShowAlert("Download", $"Saved to {dest?.FilePath}", "OK");
            }
            else
            {
                _logger.LogWarning("Download button clicked but sender or binding context was invalid.");
            }
        }

        /// <summary>
        /// The SearchAsync.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task SearchAsync()
        {
            _logger.LogDebug("Performing Pokémon search. Page: {CurrentPage}, PageSize: {PageSize}", currentPage, pageSize);
            var result = await _api.SearchAsync("pokemon", null, currentPage, pageSize);
            if (result == null)
            {
                _logger.LogError("Search failed: API returned null.");
                await ShowAlert("Error", "Could not connect to server.", "OK");
                return;
            }
            if (!string.IsNullOrEmpty(result.Error))
            {
                _logger.LogError("Search error: {Error}", result.Error);
                await ShowAlert("Error", result.Error, "OK");
                return;
            }

            var displayList = new List<PokemonInfoDisplay>();

            if (result.Pokemon != null)
            {
                foreach (var poke in result.Pokemon)
                {
                    if (!string.IsNullOrEmpty(poke.Base64))
                    {
                        var pkmInfo = PKHexService.GetPokemonInfo(poke.Base64);
                        if (pkmInfo != null)
                        {
                            var display = new PokemonInfoDisplay
                            {
                                Nickname = pkmInfo.Nickname,
                                OT = pkmInfo.OT,
                                SID = pkmInfo.SID,
                                TID = pkmInfo.TID,
                                Level = pkmInfo.Level,
                                IsShiny = pkmInfo.IsShiny,
                                Species = PKHeX.Core.SpeciesName.GetSpeciesName(pkmInfo.Species, pkmInfo.Language),
                                Generation = pkmInfo.Generation.ToString(),
                                Legal = poke.Legal,
                                Code = poke.Code
                            };
                            displayList.Add(display);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to parse Pokémon info from Base64 for code {Code}", poke.Code);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Pokémon entry with code {Code} has no Base64 data.", poke.Code);
                    }
                }
                ResultsView.ItemsSource = null; // Force redraw
                ResultsView.ItemsSource = displayList;
                _logger.LogInformation("Search results updated. Count: {Count}", displayList.Count);
            }
            else
            {
                _logger.LogInformation("No Pokémon found in search results.");
            }
            PageLabel.Text = $"Page {currentPage}";
        }

        // Ensures DisplayAlert always works and shows all text

        /// <summary>
        /// The ShowAlert.
        /// </summary>
        /// <param name="title">The title<see cref="string"/>.</param>
        /// <param name="message">The message<see cref="string"/>.</param>
        /// <param name="accept">The accept<see cref="string"/>.</param>
        /// <param name="cancel">The cancel<see cref="string?"/>.</param>
        /// <returns>The <see cref="Task{bool}"/>.</returns>
        private async Task<bool> ShowAlert(string title, string message, string accept, string? cancel = null)
        {
            _logger.LogDebug("Showing alert. Title: {Title}, Accept: {Accept}, Cancel: {Cancel}", title, accept, cancel);
            bool result = false;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (cancel == null)
                {
                    await DisplayAlert(title, message, accept);
                    result = true;
                }
                else
                {
                    result = await DisplayAlert(title, message, accept, cancel);
                }
            });
            return result;
        }
    }
}
