namespace GPSS_Client
{
    using CommunityToolkit.Maui.Storage;
    using GPSS_Client.Config;
    using GPSS_Client.Models;
    using GPSS_Client.Services;
    using GPSS_Client.Utils;
    using Microsoft.Extensions.Logging;
    using Microsoft.Maui.Controls;
    using System.Linq;

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
        /// Defines the _searchRefreshCts.
        /// </summary>
        private CancellationTokenSource? _searchRefreshCts;

        /// <summary>
        /// Defines the currentBox.
        /// </summary>
        private int currentBox = 1;

        /// <summary>
        /// Defines the boxSize.
        /// </summary>
        private const int boxSize = 30;

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

            _searchRefreshCts = new CancellationTokenSource();
            StartPeriodicSearchRefresh(_searchRefreshCts.Token);
        }

        /// <summary>
        /// The OnDisappearing.
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _searchRefreshCts?.Cancel();
            _searchRefreshCts?.Dispose();
            _searchRefreshCts = null;
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
        /// The StartPeriodicSearchRefresh.
        /// </summary>
        /// <param name="token">The token<see cref="CancellationToken"/>.</param>
        private async void StartPeriodicSearchRefresh(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await PollForSearchUpdateAsync(5, 30);
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                }
            }
            catch (TaskCanceledException) { /* Ignore */ }
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
        /// The OnNextBoxClicked.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private async void OnNextBoxClicked(object sender, EventArgs e)
        {
            _logger.LogDebug("Next box button clicked. Current box: {CurrentBox}", currentBox);
            int nextBox = currentBox + 1;
            var result = await _api.SearchAsync("pokemon", null, nextBox, boxSize);
            if (result == null)
            {
                _logger.LogError("Failed to fetch next box: API returned null.");
                await ShowAlert("Error", "Could not connect to server.", "OK");
                return;
            }
            if (!string.IsNullOrEmpty(result.Error))
            {
                _logger.LogError("Error fetching next box: {Error}", result.Error);
                await ShowAlert("Error", result.Error, "OK");
                return;
            }
            if (result.Pokemon != null && result.Pokemon.Count > 0)
            {
                currentBox = nextBox;
                bool changed = await UpdateGuiAsync(result);
                _logger.LogInformation("Box changed to {CurrentBox}. Changed: {Changed}", currentBox, changed);
            }
            else
            {
                _logger.LogInformation("No more Pokémon found on next box.");
            }
        }

        /// <summary>
        /// The OnPreviousBoxClicked.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="EventArgs"/>.</param>
        private async void OnPreviousBoxClicked(object sender, EventArgs e)
        {
            _logger.LogDebug("Previous box button clicked. Current box: {CurrentBox}", currentBox);
            if (currentBox > 1)
            {
                int prevBox = currentBox - 1;
                var result = await _api.SearchAsync("pokemon", null, prevBox, boxSize);
                if (result == null)
                {
                    _logger.LogError("Failed to fetch previous box: API returned null.");
                    await ShowAlert("Error", "Could not connect to server.", "OK");
                    return;
                }
                if (!string.IsNullOrEmpty(result.Error))
                {
                    _logger.LogError("Error fetching previous box: {Error}", result.Error);
                    await ShowAlert("Error", result.Error, "OK");
                    return;
                }
                if (result.Pokemon != null && result.Pokemon.Count > 0)
                {
                    currentBox = prevBox;
                    bool changed = await UpdateGuiAsync(result);
                    _logger.LogInformation("Box changed to {CurrentBox}. Changed: {Changed}", currentBox, changed);
                }
                else
                {
                    _logger.LogInformation("No more Pokémon found on previous box.");
                }
            }
            else
            {
                _logger.LogInformation("Already at the first box, cannot go back further.");
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
        /// The OnPkFileDrop.
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/>.</param>
        /// <param name="e">The e<see cref="DropEventArgs"/>.</param>
        private async void OnPkFileDrop(object sender, DropEventArgs e)
        {
            try
            {
                var filePaths = new List<string>();

#if WINDOWS
                // Windows: Use PlatformArgs to get file paths from drag-and-drop
                if (e.PlatformArgs is not null &&
                    e.PlatformArgs.DragEventArgs.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
                {
                    var items = await e.PlatformArgs.DragEventArgs.DataView.GetStorageItemsAsync();
                    foreach (var item in items)
                    {
                        if (item is Windows.Storage.StorageFile file)
                            filePaths.Add(file.Path);
                    }
                }
#else
                // Fallback for other platforms (if supported)
                if (e.Data is not null && e.Data.Contains(Microsoft.Maui.ApplicationModel.DataTransfer.DropDataFormats.Files))
                {
                    var names = await e.Data.GetFileNamesAsync();
                    filePaths.AddRange(names);
                }
#endif

                // Only accept .pk* files
                var pkFiles = filePaths
                    .Where(f => Path.GetExtension(f).StartsWith(".pk", StringComparison.InvariantCultureIgnoreCase))
                    .ToList();

                if (pkFiles.Count == 0)
                {
                    await ShowAlert("Error", "No valid Pokémon files dropped.", "OK");
                    return;
                }

                if (pkFiles.Count > 6)
                {
                    await ShowAlert("Error", "Select up to 6 files.", "OK");
                    return;
                }

                var gens = new List<string>();
                var streams = new List<Stream>();
                try
                {
                    foreach (var filePath in pkFiles)
                    {
                        var gen = Helpers.GetGenerationFromFilename(Path.GetFileName(filePath));
                        if (gen == null)
                        {
                            await ShowAlert("Error", $"Could not determine generation for {Path.GetFileName(filePath)}", "OK");
                            return;
                        }
                        gens.Add(gen);
                        streams.Add(File.OpenRead(filePath));
                    }

                    if (pkFiles.Count == 1)
                    {
                        var result = await _api.UploadPokemonAsync(streams[0], gens[0]);
                        if (!string.IsNullOrEmpty(result?.Error))
                            await ShowAlert("Upload Failed", result.Error, "OK");
                        else if (!string.IsNullOrEmpty(result?.Code))
                            await ShowAlert("Upload", $"Pokémon uploaded! Code: {result.Code}", "OK");
                        else
                            await ShowAlert("Upload Failed", "Unknown error", "OK");
                    }
                    else
                    {
                        var result = await _api.UploadBundleAsync(streams, gens);
                        if (!string.IsNullOrEmpty(result?.Error))
                            await ShowAlert("Upload Failed", result.Error, "OK");
                        else if (!string.IsNullOrEmpty(result?.Code))
                            await ShowAlert("Upload", $"Bundle uploaded! Code: {result.Code}", "OK");
                        else
                            await ShowAlert("Upload Failed", "Unknown error", "OK");
                    }
                }
                finally
                {
                    foreach (var stream in streams)
                    {
                        stream.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Pokémon drag-and-drop upload.");
                await ShowAlert("Error", "An unexpected error occurred during upload.", "OK");
            }
        }

        /// <summary>
        /// The UpdateGuiAsync.
        /// </summary>
        /// <param name="result">The result<see cref="SearchResult"/>.</param>
        /// <returns>The <see cref="Task{bool}"/>.</returns>
        private async Task<bool> UpdateGuiAsync(SearchResult result)
        {
            var newDisplayList = new List<PokemonInfoDisplay>();

            if (result.Pokemon != null)
            {
                foreach (var poke in result.Pokemon)
                {
                    if (!string.IsNullOrEmpty(poke.Base64))
                    {
                        var pkmInfo = PKHeXService.GetPokemonInfo(poke.Base64);
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
                            newDisplayList.Add(display);
                        }
                    }
                }
            }

            // Get current codes for change detection
            var currentCodes = (ResultsView.ItemsSource as IEnumerable<PokemonInfoDisplay>)?
                .Select(p => p.Code)
                .ToList() ?? new List<string>();
            var newCodes = newDisplayList.Select(p => p.Code).ToList();

            bool changed = !currentCodes.SequenceEqual(newCodes);

            if (changed)
            {
                ResultsView.ItemsSource = null;
                ResultsView.ItemsSource = newDisplayList;
                BoxLabel.Text = $"Box {currentBox}";
                _logger.LogInformation("Search results updated. Count: {Count}", newDisplayList.Count);
            }

            await Task.CompletedTask;
            return changed;
        }

        /// <summary>
        /// The PollForSearchUpdateAsync.
        /// </summary>
        /// <param name="intervalSeconds">The intervalSeconds<see cref="int"/>.</param>
        /// <param name="timeoutSeconds">The timeoutSeconds<see cref="int"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task PollForSearchUpdateAsync(int intervalSeconds = 2, int timeoutSeconds = 20)
        {
            var start = DateTime.UtcNow;

            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds));

                var result = await _api.SearchAsync("pokemon", null, currentBox, boxSize);
                if (result == null || result.Pokemon == null)
                    continue;

                if (await UpdateGuiAsync(result))
                    break;

                if ((DateTime.UtcNow - start).TotalSeconds > timeoutSeconds)
                    break;
            }
        }

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
