namespace GPSS_Server.Controllers
{
    using GPSS_Server.Datastore;
    using GPSS_Server.Models;
    using GPSS_Server.Utils;
    using Microsoft.AspNetCore.Mvc;
    using PKHeX.Core;
    using System.Text.Json;

    /// <summary>
    /// Defines the <see cref="GpssController" />.
    /// </summary>
    [ApiController]
    [Route("/api/v2/gpss")]
    public class GpssController(Database database) : ControllerBase
    {
        /// <summary>
        /// Defines the _supportedEntities.
        /// </summary>
        private readonly string[] _supportedEntities = ["pokemon", "bundles", "bundle"];

        /// <summary>
        /// The List.
        /// </summary>
        /// <param name="entityType">The entityType<see cref="string"/>.</param>
        /// <param name="searchBody">The searchBody<see cref="JsonElement?"/>.</param>
        /// <param name="page">The page<see cref="int"/>.</param>
        /// <param name="amount">The amount<see cref="int"/>.</param>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
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
                count = await database.CountAsync("pokemon");
                items = await database.ListPokemonsAsync(page, amount, search);
            }
            else
            {
                count = await database.CountAsync("bundle");
                items = await database.ListBundlesAsync(page, amount, search);
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

        /// <summary>
        /// The Upload.
        /// </summary>
        /// <param name="entityType">The entityType<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
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

                long? id = await database.GetPokemonIdAsync(payload.base64);
                if (id.HasValue)
                {
                    // Fetch the download code for the existing Pokémon
                    var code = await database.GetPokemonDownloadCodeAsync(payload.base64);
                    return Ok(new { code });
                }

                var legality = new LegalityAnalysis(payload.pokemon);
                var newCode = await Helpers.GenerateDownloadCodeAsync(database, "pokemon");
                await database.InsertPokemonAsync(payload.base64, legality.Valid, newCode, generation!);
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

                long? id = await database.GetPokemonIdAsync(payload.base64);

                var legality = new LegalityAnalysis(payload.pokemon);
                if (!legality.Valid) bundleLegal = false;

                if (!id.HasValue)
                {
                    var code = await Helpers.GenerateDownloadCodeAsync(database, "pokemon");
                    id = await database.InsertPokemonAsync(payload.base64, legality.Valid, code, generations[i]);
                    ids.Add(id.Value);
                }
                else
                {
                    ids.Add(id.Value);
                }
            }

            var bundleCode = await database.CheckIfBundleExistsAsync(ids);
            if (bundleCode != null) return Ok(new { code = bundleCode });

            bundleCode = await Helpers.GenerateDownloadCodeAsync(database, "bundle");
            await database.InsertBundleAsync(bundleLegal, bundleCode, ((int)minGen!.Value).ToString(), ((int)maxGen!.Value).ToString(), ids);

            return Ok(new { code = bundleCode });
        }

        /// <summary>
        /// The Download.
        /// </summary>
        /// <param name="entityType">The entityType<see cref="string"/>.</param>
        /// <param name="code">The code<see cref="string"/>.</param>
        /// <param name="download">The download<see cref="bool"/>.</param>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [HttpGet("download/{entityType}/{code}")]
        public async Task<IActionResult> Download(
            [FromRoute] string entityType,
            [FromRoute] string code,
            [FromQuery] bool download = false)
        {
            if (!_supportedEntities.Contains(entityType))
                return BadRequest(new { message = "Invalid entity type." });

            await database.IncrementDownloadAsync(
                entityType == "bundles" || entityType == "bundle" ? "bundle" : "pokemon",
                code);

            if (!download)
                return Ok();

            if (entityType == "pokemon")
            {
                var pokemon = (await database.ListPokemonsAsync(1, 1, new Search { DownloadCode = code })).FirstOrDefault();
                if (string.IsNullOrEmpty(pokemon.DownloadCode))
                    return NotFound(new { message = "Pokemon not found." });
                return Ok(pokemon);
            }
            else
            {
                var bundle = (await database.ListBundlesAsync(1, 1, new Search { DownloadCode = code })).FirstOrDefault();
                if (string.IsNullOrEmpty(bundle.DownloadCode))
                    return NotFound(new { message = "Bundle not found." });
                return Ok(bundle);
            }
        }
    }
}
