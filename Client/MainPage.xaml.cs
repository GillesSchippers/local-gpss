using CommunityToolkit.Maui.Storage;
using GPSS_Client.Config;
using GPSS_Client.Services;

namespace GPSS_Client;

public partial class MainPage : ContentPage
{
    private readonly ClientConfig _config;
    private readonly ApiService _api;

    private int currentPage = 1;
    private const int pageSize = 30;

    private IList<object> _selectedPokemon = [];

    private static readonly FilePickerFileType PkFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.WinUI, new[] { ".pk1", ".pk2", ".pk3", ".pk4", ".pk5", ".pk6", ".pk7", ".pk8" } },
        { DevicePlatform.macOS, new[] { ".pk1", ".pk2", ".pk3", ".pk4", ".pk5", ".pk6", ".pk7", ".pk8" } },
        { DevicePlatform.iOS, new[] { "public.data" } }, // iOS does not support extension filtering, but you can filter after selection
        { DevicePlatform.Android, new[] { "application/octet-stream" } }, // Android: filter after selection
    });

    private static readonly PickOptions PkPickOptions = new PickOptions
    {
        PickerTitle = "Select Pokémon PKM file(s)",
        FileTypes = PkFileType
    };

    public string ApiUrl
    {
        get => _config.ApiUrl;
        set => _config.ApiUrl = value;
    }

    public MainPage(ClientConfig config, ApiService api)
    {
        InitializeComponent();
        _config = config;
        _api = api;
        _ = SearchAsync();
    }

    private async void OnUploadPokemonClicked(object sender, EventArgs e)
    {
        var files = await FilePicker.PickMultipleAsync(PkPickOptions);
        if (files == null || files.Count() == 0)
            return;

        // Filter for .pk* files in case the platform doesn't filter
        files = files.Where(f => Path.GetExtension(f.FileName).ToLowerInvariant().StartsWith(".pk")).ToList();

        if (files.Count() == 0)
        {
            await ShowAlert("Error", "No valid Pokémon files selected.", "OK");
            return;
        }

        if (files.Count() > 6)
        {
            await ShowAlert("Error", "Select up to 6 files.", "OK");
            return;
        }

        var gens = new List<string>();
        var streams = new List<Stream>();
        foreach (var file in files)
        {
            var gen = GetGenerationFromFilename(file.FileName);
            if (gen == null)
            {
                await ShowAlert("Error", $"Could not determine generation for {file.FileName}", "OK");
                return;
            }
            gens.Add(gen);
            streams.Add(await file.OpenReadAsync());
        }

        if (files.Count() == 1)
        {
            var result = await _api.UploadPokemonAsync(streams[0], gens[0]);
            if (!string.IsNullOrEmpty(result?.Code))
                await ShowAlert("Upload", $"Pokémon uploaded! Code: {result.Code}", "OK");
            else
                await ShowAlert("Upload Failed", result?.Error ?? "Unknown error", "OK");
        }
        else
        {
            var result = await _api.UploadBundleAsync(streams, gens);
            if (!string.IsNullOrEmpty(result?.Code))
                await ShowAlert("Upload", $"Bundle uploaded! Code: {result.Code}", "OK");
            else
                await ShowAlert("Upload Failed", result?.Error ?? "Unknown error", "OK");
        }
    }

    private async void OnCheckLegalityClicked(object sender, EventArgs e)
    {
        var files = await FilePicker.PickMultipleAsync(PkPickOptions);
        if (files == null || files.Count() == 0)
            return;

        files = files.Where(f => Path.GetExtension(f.FileName).ToLowerInvariant().StartsWith(".pk")).ToList();

        if (files.Count() == 0)
        {
            await ShowAlert("Error", "No valid Pokémon files selected.", "OK");
            return;
        }

        if (files.Count() > 6)
        {
            await ShowAlert("Error", "Select up to 6 files.", "OK");
            return;
        }

        var results = new List<string>();
        foreach (var file in files)
        {
            var gen = GetGenerationFromFilename(file.FileName);
            if (gen == null)
            {
                results.Add($"{file.FileName}: Could not determine generation.");
                continue;
            }

            using var stream = await file.OpenReadAsync();
            var result = await _api.CheckLegalityAsync(stream, gen);
            if (result == null)
            {
                results.Add($"{file.FileName}: No result from API.");
            }
            else if (!string.IsNullOrEmpty(result.Error))
            {
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
                results.Add($"{file.FileName}: Pokémon is not legal. No details available.");
            }
        }

        await ShowAlert("Legality Results", string.Join("\n\n", results), "OK");
    }

    private async void OnLegalizePokemonClicked(object sender, EventArgs e)
    {
        var files = await FilePicker.PickMultipleAsync(PkPickOptions);
        if (files == null || files.Count() == 0)
            return;

        files = files.Where(f => Path.GetExtension(f.FileName).ToLowerInvariant().StartsWith(".pk")).ToList();

        if (files.Count() == 0)
        {
            await ShowAlert("Error", "No valid Pokémon files selected.", "OK");
            return;
        }

        if (files.Count() > 6)
        {
            await ShowAlert("Error", "Select up to 6 files.", "OK");
            return;
        }

        var results = new List<string>();
        foreach (var file in files)
        {
            var gen = GetGenerationFromFilename(file.FileName);
            if (gen == null)
            {
                results.Add($"{file.FileName}: Could not determine generation.");
                continue;
            }

            using var stream = await file.OpenReadAsync();
            var result = await _api.LegalizeAsync(stream, gen, "Any");
            if (result == null || !string.IsNullOrEmpty(result.Error))
            {
                results.Add($"{file.FileName}: Legalization failed.");
            }
            else if (result.Legal && !result.Ran)
            {
                results.Add($"{file.FileName}: Pokémon is already legal.");
            }
            else if (!string.IsNullOrEmpty(result.Pokemon))
            {
                var save = await ShowAlert("Legalize", $"{file.FileName}: Legalized! Save to file?", "Yes", "No");
                if (save)
                {
                    var bytes = Convert.FromBase64String(result.Pokemon);
                    var dest = await FileSaver.Default.SaveAsync(file.FileName, new MemoryStream(bytes));
                    results.Add($"{file.FileName}: Saved to {dest?.FilePath}");
                }
                else
                {
                    results.Add($"{file.FileName}: Legalized, not saved.");
                }
            }
            else
            {
                results.Add($"{file.FileName}: Legalization did not produce a file.");
            }
        }

        await ShowAlert("Legalize Results", string.Concat("\n\n", results), "OK");
    }

    private async void OnNextPageClicked(object sender, EventArgs e)
    {
        int nextPage = currentPage + 1;
        var result = await _api.SearchAsync("pokemon", null, nextPage, pageSize);
        if (result?.Pokemon != null && result.Pokemon.Count > 0)
        {
            currentPage = nextPage;
            ResultsView.ItemsSource = result.Pokemon;
            PageLabel.Text = $"Page {currentPage}";
        }
        // else: do nothing, stay on current page
    }

    private async void OnPreviousPageClicked(object sender, EventArgs e)
    {
        if (currentPage > 1)
        {
            int prevPage = currentPage - 1;
            var result = await _api.SearchAsync("pokemon", null, prevPage, pageSize);
            if (result?.Pokemon != null && result.Pokemon.Count > 0)
            {
                currentPage = prevPage;
                ResultsView.ItemsSource = result.Pokemon;
                PageLabel.Text = $"Page {currentPage}";
            }
            // else: do nothing, stay on current page
        }
    }

    private async Task SearchAsync()
    {
        var result = await _api.SearchAsync("pokemon", null, currentPage, pageSize);
        var displayList = new List<PokemonInfoDisplay>();

        if (result?.Pokemon != null)
        {
            foreach (var poke in result.Pokemon)
            {
                if (!string.IsNullOrEmpty(poke.Base_64))
                {
                    var pkmInfo = PkhexService.GetPokemonInfo(poke.Base_64);
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
                }
            }
            ResultsView.ItemsSource = null; // Force redraw
            ResultsView.ItemsSource = displayList;
        }
        PageLabel.Text = $"Page {currentPage}";
    }

    //private async void OnDownloadClicked(object sender, EventArgs e)
    //{
    //    if (sender is Button btn && btn.BindingContext is PokemonResult poke)
    //    {
    //        var result = await _api.DownloadPokemonAsync(poke.Code);
    //        if (result == null || string.IsNullOrEmpty(result.Base_64))
    //        {
    //            await ShowAlert("Download", "Failed to download.", "OK");
    //            return;
    //        }
    //        var bytes = Convert.FromBase64String(result.Base_64);
    //        var fileName = $"{poke.Code}.pk{poke.Generation}";
    //        var dest = await FileSaver.Default.SaveAsync(fileName, new MemoryStream(bytes));
    //        await ShowAlert("Download", $"Saved to {dest?.FilePath}", "OK");
    //    }
    //}

    private async Task DownloadSelectedPokemonAsync()
    {
        if (_selectedPokemon == null || _selectedPokemon.Count == 0)
            return;

        if (_selectedPokemon.Count == 1)
        {
            // Single file: ask for file location
            var poke = _selectedPokemon[0] as PokemonInfoDisplay;
            if (poke == null) return;
            var result = await _api.DownloadPokemonAsync(poke.Code.ToString());
            if (result == null || string.IsNullOrEmpty(result.Base_64))
            {
                await ShowAlert("Download", "Failed to download.", "OK");
                return;
            }
            var bytes = Convert.FromBase64String(result.Base_64);
            var fileName = $"{poke.Code}.pk{poke.Generation}";
            var dest = await FileSaver.Default.SaveAsync(fileName, new MemoryStream(bytes));
            await ShowAlert("Download", $"Saved to {dest?.FilePath}", "OK");
        }
        else
        {
            // Multiple files: ask for folder
            var folderResult = await FolderPicker.Default.PickAsync();
            if (folderResult.IsSuccessful)
            {
                foreach (var obj in _selectedPokemon)
                {
                    if (obj is not PokemonInfoDisplay poke)
                        continue;
                    var result = await _api.DownloadPokemonAsync(poke.Code.ToString());
                    if (result == null || string.IsNullOrEmpty(result.Base_64))
                        continue;
                    var bytes = Convert.FromBase64String(result.Base_64);
                    var fileName = $"{poke.Code}.pk{poke.Generation}";
                    var filePath = Path.Combine(folderResult.Folder.Path, fileName);
                    using var fs = File.Create(filePath);
                    await fs.WriteAsync(bytes, 0, bytes.Length);
                }
                await ShowAlert("Download", "All selected Pokémon saved.", "OK");
            }
        }
    }

    private void OnResultsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedPokemon = e.CurrentSelection.ToList();
    }

    public async void OnDownloadSelectedPokemon(object sender, EventArgs e)
    {
        await DownloadSelectedPokemonAsync();
    }

    public Command<object> ShowDownloadMenuCommand => new(async (item) =>
    {
        if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
        {
            var action = await DisplayActionSheet("Actions", "Cancel", null, "Download");
            if (action == "Download")
                await DownloadSelectedPokemonAsync();
        }
    });

    private static string? GetGenerationFromFilename(string filename)
    {
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        if (ext.StartsWith(".pk") && ext.Length > 3 && int.TryParse(ext.Substring(3), out var gen))
            return gen.ToString();
        return null;
    }

    // Ensures DisplayAlert always works and shows all text
    private async Task<bool> ShowAlert(string title, string message, string accept, string? cancel = null)
    {
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
