using System.Text.Json;
using Datastore;
using Models;
using Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PKHeX.Core;

namespace Controllers
{
    [ApiController]
    [Route("/api/v2/gpss")]
    public class GpssController : ControllerBase
    {
        private readonly string[] _supportedEntities = ["pokemon", "bundles", "bundle"];
        private readonly Database _database;

        public GpssController(Database database)
        {
            _database = database;
        }

        [HttpPost("search/{entityType}")]
        public async Task<IActionResult> List([FromRoute] string entityType, [FromBody] JsonElement? searchBody, [FromQuery] int page = 1,
            [FromQuery] int amount = 30)
        {
            if (!_supportedEntities.Contains(entityType))
                return BadRequest(new { message = "Invalid entity type." });

            Search? search = null;
            if (searchBody.HasValue) search = Helpers.SearchTranslation(searchBody.Value);

            int count;
            object items;
            if (entityType == "pokemon")
            {
                count = await _database.CountAsync("pokemon");
                items = await _database.ListPokemonsAsync(page, amount, search);
            }
            else
            {
                count = await _database.CountAsync("bundle");
                items = await _database.ListBundlesAsync(page, amount, search);
            }

            var pages = count != 0 ? Math.Ceiling((double)count / amount) : 1;
            if (pages == 0) pages = 1;

            return Ok(new Dictionary<string, object>
            {
                { "page", page },
                { "pages", pages },
                { "total", count },
                { entityType, items }
            });
        }

        [HttpPost("upload/{entityType}")]
        public async Task<IActionResult> Upload([FromRoute] string entityType)
        {
            if (!_supportedEntities.Contains(entityType))
                return BadRequest(new { message = "Invalid entity type." });

            var files = Request.Form.Files;
            var headers = Request.Headers;

            if (entityType == "pokemon")
            {
                if (!headers.TryGetValue("generation", out var generation))
                    return BadRequest(new { error = "missing generation header" });

                var pkmnFile = files.GetFile("pkmn");
                if (pkmnFile == null)
                    return BadRequest(new { error = "pkmn file is missing." });

                var payload = Helpers.PokemonAndBase64FromForm(pkmnFile, Helpers.EntityContextFromString(generation!));
                if (payload.pokemon == null)
                    return BadRequest(new { error = "not a pokemon" });

                long? id = await _database.GetPokemonIdAsync(payload.base64);
                if (id.HasValue)
                {
                    // Fetch the download code for the existing Pokémon
                    var code = await _database.GetPokemonDownloadCodeAsync(payload.base64);
                    return Ok(new { code });
                }

                var legality = new LegalityAnalysis(payload.pokemon);
                var newCode = await Helpers.GenerateDownloadCodeAsync(_database, "pokemon");
                await _database.InsertPokemonAsync(payload.base64, legality.Valid, newCode, generation!);
                return Ok(new { code = newCode });
            }

            if (!headers.TryGetValue("count", out var countStr)) return BadRequest(new { error = "missing count header" });
            if (!int.TryParse(countStr, out var count)) return BadRequest(new { error = "count is not an integer" });
            if (count < 2 || count > 6) return BadRequest(new { error = "count must be between 2 and 6" });
            if (!headers.TryGetValue("generations", out var generationsStr)) return BadRequest(new { error = "missing generations header" });

            List<string> generations = [.. generationsStr.ToString().Split(',')];
            List<long> ids = [];
            bool bundleLegal = true;
            EntityContext? minGen = null;
            EntityContext? maxGen = null;

            if (generations.Count != count) return BadRequest(new { error = "number of generations does not match" });

            for (var i = 0; i < count; i++)
            {
                var pkmnFile = files.GetFile($"pkmn{i + 1}");
                if (pkmnFile == null) return BadRequest(new { error = $"pkmn{i + 1} file is missing." });

                var gen = Helpers.EntityContextFromString(generations[i]);
                var payload = Helpers.PokemonAndBase64FromForm(pkmnFile, gen);
                if (payload.pokemon == null) return BadRequest(new { error = $"pkmn{i + 1} is not a pokemon" });

                if (!minGen.HasValue || (gen != EntityContext.None ? gen : payload.pokemon.Context) < minGen.Value)
                    minGen = gen != EntityContext.None ? gen : payload.pokemon.Context;
                if (!maxGen.HasValue || (gen != EntityContext.None ? gen : payload.pokemon.Context) > maxGen.Value)
                    maxGen = gen != EntityContext.None ? gen : payload.pokemon.Context;

                long? id = await _database.GetPokemonIdAsync(payload.base64);

                var legality = new LegalityAnalysis(payload.pokemon);
                if (!legality.Valid) bundleLegal = false;

                if (!id.HasValue)
                {
                    var code = await Helpers.GenerateDownloadCodeAsync(_database, "pokemon");
                    id = await _database.InsertPokemonAsync(payload.base64, legality.Valid, code, generations[i]);
                    ids.Add(id.Value);
                }
                else
                {
                    ids.Add(id.Value);
                }
            }

            var bundleCode = await _database.CheckIfBundleExistsAsync(ids);
            if (bundleCode != null) return Ok(new { code = bundleCode });

            bundleCode = await Helpers.GenerateDownloadCodeAsync(_database, "bundle");
            await _database.InsertBundleAsync(bundleLegal, bundleCode, ((int)minGen!.Value).ToString(), ((int)maxGen!.Value).ToString(), ids);

            return Ok(new { code = bundleCode });
        }

        [HttpGet("download/{entityType}/{code}")]
        public async Task<IActionResult> Download(
            [FromRoute] string entityType,
            [FromRoute] string code,
            [FromQuery] bool download = false)
        {
            if (!_supportedEntities.Contains(entityType))
                return BadRequest(new { message = "Invalid entity type." });

            await _database.IncrementDownloadAsync(
                entityType == "bundles" || entityType == "bundle" ? "bundle" : "pokemon",
                code);

            if (!download)
                return Ok();

            if (entityType == "pokemon")
            {
                var pokemon = (await _database.ListPokemonsAsync(1, 1, new Search { DownloadCode = code })).FirstOrDefault();
                if (string.IsNullOrEmpty(pokemon.DownloadCode))
                    return NotFound(new { message = "Pokemon not found." });
                return Ok(pokemon);
            }
            else
            {
                var bundle = (await _database.ListBundlesAsync(1, 1, new Search { DownloadCode = code })).FirstOrDefault();
                if (string.IsNullOrEmpty(bundle.DownloadCode))
                    return NotFound(new { message = "Bundle not found." });
                return Ok(bundle);
            }
        }
    }
}