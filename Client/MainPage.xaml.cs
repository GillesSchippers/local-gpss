using GPSS_Client.Config;
using GPSS_Client.Services;
using CommunityToolkit.Maui.Storage;

namespace GPSS_Client;

public partial class MainPage : ContentPage
{
    private readonly ClientConfig config;
    private readonly ApiService api;
    public MainPage()
    {
        InitializeComponent();
        config = ConfigService.Load();
        api = new ApiService(config);
    }

    private void OnSaveApiUrlClicked(object sender, EventArgs e)
    {
        var url = ApiUrlEntry.Text?.Trim();
        if (!string.IsNullOrEmpty(url))
        {
            config.ApiUrl = url;
            ConfigService.Save(config);
            api.SetBaseUrl(url);
            DisplayAlert("Config", "API URL saved.", "OK");
        }
    }

    private async void OnUploadPokemonClicked(object sender, EventArgs e)
    {
        var file = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Select a PKM file" });
        if (file == null) return;

        var gen = GetGenerationFromFilename(file.FileName);
        if (gen == null)
        {
            await DisplayAlert("Error", "Could not determine generation from file extension.", "OK");
            return;
        }

        using var stream = await file.OpenReadAsync();
        var result = await api.UploadPokemonAsync(stream, gen);
        if (!string.IsNullOrEmpty(result?.Code))
            await DisplayAlert("Upload", $"Pokémon uploaded! Code: {result.Code}", "OK");
        else
            await DisplayAlert("Upload Failed", result?.Error ?? "Unknown error", "OK");
    }

    private async void OnUploadBundleClicked(object sender, EventArgs e)
    {
        var files = await FilePicker.PickMultipleAsync(new PickOptions { PickerTitle = "Select 2-6 PKM files" });
        if (files == null || files.Count() < 2 || files.Count() > 6)
        {
            await DisplayAlert("Error", "Select between 2 and 6 files.", "OK");
            return;
        }

        var gens = new List<string>();
        var streams = new List<Stream>();
        foreach (var file in files)
        {
            var gen = GetGenerationFromFilename(file.FileName);
            if (gen == null)
            {
                await DisplayAlert("Error", $"Could not determine generation for {file.FileName}", "OK");
                return;
            }
            gens.Add(gen);
            streams.Add(await file.OpenReadAsync());
        }

        var result = await api.UploadBundleAsync(streams, gens);
        if (!string.IsNullOrEmpty(result?.Code))
            await DisplayAlert("Upload", $"Bundle uploaded! Code: {result.Code}", "OK");
        else
            await DisplayAlert("Upload Failed", result?.Error ?? "Unknown error", "OK");
    }

    private async void OnCheckLegalityClicked(object sender, EventArgs e)
    {
        var file = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Select a PKM file" });
        if (file == null) return;

        var gen = GetGenerationFromFilename(file.FileName);
        if (gen == null)
        {
            await DisplayAlert("Error", "Could not determine generation from file extension.", "OK");
            return;
        }

        using var stream = await file.OpenReadAsync();
        var result = await api.CheckLegalityAsync(stream, gen);
        if (result == null)
        {
            await DisplayAlert("Legality", "No result.", "OK");
            return;
        }
        if (!string.IsNullOrEmpty(result.Error))
        {
            await DisplayAlert("Legality", result.Error, "OK");
            return;
        }
        await DisplayAlert("Legality", result.Legal ? "Pokémon is legal." : string.Join('\n', result.Report ?? Array.Empty<string>()), "OK");
    }

    private async void OnLegalizePokemonClicked(object sender, EventArgs e)
    {
        var file = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Select a PKM file" });
        if (file == null) return;

        var gen = GetGenerationFromFilename(file.FileName);
        if (gen == null)
        {
            await DisplayAlert("Error", "Could not determine generation from file extension.", "OK");
            return;
        }

        using var stream = await file.OpenReadAsync();
        // For demo, just use "Any" as version. You may want to extract version from file.
        var result = await api.LegalizeAsync(stream, gen, "Any");
        if (result == null)
        {
            await DisplayAlert("Legalize", "No result.", "OK");
            return;
        }
        if (!string.IsNullOrEmpty(result.Error))
        {
            await DisplayAlert("Legalize", result.Error, "OK");
            return;
        }
        if (result.Legal && !result.Ran)
        {
            await DisplayAlert("Legalize", "Pokémon is already legal.", "OK");
            return;
        }
        if (!string.IsNullOrEmpty(result.Pokemon))
        {
            var save = await DisplayAlert("Legalize", "Legalized! Save to file?", "Yes", "No");
            if (save)
            {
                var bytes = Convert.FromBase64String(result.Pokemon);
                var fileName = file.FileName;
                var dest = await FileSaver.Default.SaveAsync(fileName, new MemoryStream(bytes));
                await DisplayAlert("Legalize", $"Saved to {dest?.FilePath}", "OK");
            }
        }
        else
        {
            await DisplayAlert("Legalize", string.Join('\n', result.Report ?? Array.Empty<string>()), "OK");
        }
    }

    private async void OnSearchClicked(object sender, EventArgs e)
    {
        var result = await api.SearchAsync("pokemon");
        ResultsView.ItemsSource = result?.Pokemon ?? new List<PokemonResult>();
    }

    private async void OnDownloadClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.BindingContext is PokemonResult poke)
        {
            var result = await api.DownloadPokemonAsync(poke.Code);
            if (result == null || string.IsNullOrEmpty(result.Base_64))
            {
                await DisplayAlert("Download", "Failed to download.", "OK");
                return;
            }
            var bytes = Convert.FromBase64String(result.Base_64);
            var fileName = $"{poke.Code}.pk{poke.Generation}";
            var dest = await FileSaver.Default.SaveAsync(fileName, new MemoryStream(bytes));
            await DisplayAlert("Download", $"Saved to {dest?.FilePath}", "OK");
        }
    }

    private static string? GetGenerationFromFilename(string filename)
    {
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        if (ext.StartsWith(".pk") && ext.Length > 3 && int.TryParse(ext.Substring(3), out var gen))
            return gen.ToString();
        return null;
    }
}
