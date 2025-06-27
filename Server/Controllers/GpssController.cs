namespace GPSS_Server.Controllers
{
    using GPSS_Server.Config;
    using GPSS_Server.Datastore;
    using GPSS_Server.Models;
    using GPSS_Server.Utils;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using PKHeX.Core;
    using System.Text.Json;

    /// <summary>
    /// Defines the <see cref="GpssController" />.
    /// </summary>
    [ApiController]
    [Route("/api/v2/gpss")]
    public class GpssController(ServerConfig config, Database database, IMemoryCache cache, ILogger<GpssController> logger) : ControllerBase
    {
        /// <summary>
        /// Defines the _supportedEntities.
        /// </summary>
        private readonly string[] _supportedEntities = ["pokemon", "bundles", "bundle"];

        /// <summary>
        /// The Search.
        /// </summary>
        /// <param name="entityType">The entityType<see cref="string"/>.</param>
        /// <param name="searchBody">The searchBody<see cref="JsonElement?"/>.</param>
        /// <param name="page">The page<see cref="int"/>.</param>
        /// <param name="amount">The amount<see cref="int"/>.</param>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [HttpPost("search/{entityType}")]
        public async Task<IActionResult> Search([FromRoute] string entityType, [FromBody] JsonElement? searchBody, [FromQuery] int page = 1,
            [FromQuery] int amount = 30)
        {
            logger.LogInformation("POST /api/v2/gpss/search/{EntityType} | Page: {Page} | Amount: {Amount} | SearchBody: {SearchBody}", entityType, page, amount, searchBody?.ToString());

            if (!_supportedEntities.Contains(entityType))
            {
                logger.LogWarning("Invalid entity type: {EntityType}", entityType);
                return BadRequest(new { message = "Invalid entity type." });
            }

            string cacheKey = $"{entityType}:{page}:{amount}:{searchBody?.ToString() ?? ""}";
            bool fromCache = cache.TryGetValue(cacheKey, out Dictionary<string, object>? result);

            if (!fromCache)
            {
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

                result = new Dictionary<string, object>
                {
                    { "page", page },
                    { "pages", pages },
                    { "total", count },
                    { entityType, items }
                };
                cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                {
                    Size = Helpers.GetObjectSizeInBytes(result),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(config.CacheSearch)
                });
            }

            logger.LogInformation("Search response for {EntityType} | Page: {Page} | Amount: {Amount} | Cached: {FromCache}", entityType, page, amount, fromCache);
            return Ok(result);
        }

        /// <summary>
        /// The Upload.
        /// </summary>
        /// <param name="entityType">The entityType<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [HttpPost("upload/{entityType}")]
        public async Task<IActionResult> Upload([FromRoute] string entityType)
        {
            logger.LogInformation("POST /api/v2/gpss/upload/{EntityType}", entityType);

            if (!_supportedEntities.Contains(entityType))
            {
                logger.LogWarning("Invalid entity type: {EntityType}", entityType);
                return BadRequest(new { message = "Invalid entity type." });
            }

            var files = Request.Form.Files;
            var headers = Request.Headers;

            if (entityType == "pokemon")
            {
                if (!headers.TryGetValue("generation", out var generation))
                {
                    logger.LogWarning("Missing generation header.");
                    return BadRequest(new { error = "missing generation header" });
                }

                var pkmnFile = files.GetFile("pkmn");
                if (pkmnFile == null)
                {
                    logger.LogWarning("pkmn file is missing.");
                    return BadRequest(new { error = "pkmn file is missing." });
                }

                var payload = Helpers.PokemonAndBase64FromForm(pkmnFile, Helpers.EntityContextFromString(generation!));
                if (payload.pokemon == null)
                {
                    logger.LogWarning("Not a pokemon file.");
                    return BadRequest(new { error = "not a pokemon" });
                }

                var hash = Helpers.ComputeSha256Hash(payload.base64);
                string cacheKey = $"upload:pokemon:{hash}";

                if (cache.TryGetValue(cacheKey, out string? cachedCode) && !string.IsNullOrEmpty(cachedCode))
                {
                    logger.LogInformation("Upload response served from cache for hash: {Hash}", (object)hash ?? String.Empty);
                    return Ok(new { code = cachedCode });
                }

                long? id = await database.GetPokemonIdAsync(payload.base64);
                if (id.HasValue)
                {
                    string? code = await database.GetPokemonDownloadCodeAsync(payload.base64);
                    cache.Set(cacheKey, code, new MemoryCacheEntryOptions
                    {
                        Size = Helpers.GetObjectSizeInBytes(code),
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
                    logger.LogInformation("Pokemon already exists. Returning code: {Code}", code);
                    return Ok(new { code });
                }

                var legality = new LegalityAnalysis(payload.pokemon);
                var newCode = await Helpers.GenerateDownloadCodeAsync(database, entityType);
                await database.InsertPokemonAsync(payload.base64, legality.Valid, newCode, generation!);
                Helpers.InvalidateSearchCacheAsync(cache, entityType);
                cache.Set(cacheKey, newCode, new MemoryCacheEntryOptions
                {
                    Size = Helpers.GetObjectSizeInBytes(newCode),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(config.CachePokemon)
                });
                logger.LogInformation("Pokemon uploaded successfully. New code: {Code}", newCode);
                return Ok(new { code = newCode });
            }

            if (!headers.TryGetValue("count", out var countStr))
            {
                logger.LogWarning("Missing count header.");
                return BadRequest(new { error = "missing count header" });
            }
            if (!int.TryParse(countStr, out var count))
            {
                logger.LogWarning("Count is not an integer.");
                return BadRequest(new { error = "count is not an integer" });
            }
            if (count < 2 || count > 6)
            {
                logger.LogWarning("Count must be between 2 and 6.");
                return BadRequest(new { error = "count must be between 2 and 6" });
            }
            if (!headers.TryGetValue("generations", out var generationsStr))
            {
                logger.LogWarning("Missing generations header.");
                return BadRequest(new { error = "missing generations header" });
            }

            List<string> generations = [.. generationsStr.ToString().Split(',')];
            List<string> pokemonHashes = [];
            List<long> ids = [];
            bool bundleLegal = true;
            EntityContext? minGen = null;
            EntityContext? maxGen = null;

            if (generations.Count != count)
            {
                logger.LogWarning("Number of generations does not match count.");
                return BadRequest(new { error = "number of generations does not match" });
            }

            bool needsCacheInvalidation = false;

            for (var i = 0; i < count; i++)
            {
                var pkmnFile = files.GetFile($"pkmn{i + 1}");
                if (pkmnFile == null)
                {
                    logger.LogWarning("pkmn{Index} file is missing.", i + 1);
                    return BadRequest(new { error = $"pkmn{i + 1} file is missing." });
                }

                var gen = Helpers.EntityContextFromString(generations[i]);
                var payload = Helpers.PokemonAndBase64FromForm(pkmnFile, gen);
                if (payload.pokemon == null)
                {
                    logger.LogWarning("pkmn{Index} is not a pokemon.", i + 1);
                    return BadRequest(new { error = $"pkmn{i + 1} is not a pokemon" });
                }

                if (!minGen.HasValue || (gen != EntityContext.None ? gen : payload.pokemon.Context) < minGen.Value)
                    minGen = gen != EntityContext.None ? gen : payload.pokemon.Context;
                if (!maxGen.HasValue || (gen != EntityContext.None ? gen : payload.pokemon.Context) > maxGen.Value)
                    maxGen = gen != EntityContext.None ? gen : payload.pokemon.Context;

                var hash = Helpers.ComputeSha256Hash(payload.base64);
                pokemonHashes.Add(hash);

                long? id = await database.GetPokemonIdAsync(payload.base64);

                var legality = new LegalityAnalysis(payload.pokemon);
                if (!legality.Valid) bundleLegal = false;

                if (!id.HasValue)
                {
                    var code = await Helpers.GenerateDownloadCodeAsync(database, "pokemon");
                    id = await database.InsertPokemonAsync(payload.base64, legality.Valid, code, generations[i]);
                    needsCacheInvalidation = true;
                    cache.Set($"upload:pokemon:{hash}", code, new MemoryCacheEntryOptions
                    {
                        Size = Helpers.GetObjectSizeInBytes(code),
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(config.CachePokemon)
                    });
                    ids.Add(id.Value);
                }
                else
                {
                    ids.Add(id.Value);
                }
            }

            if (needsCacheInvalidation)
                Helpers.InvalidateSearchCacheAsync(cache, "pokemon");

            var bundleKeyRaw = string.Join(",", pokemonHashes);
            var bundleKeyHash = Helpers.ComputeSha256Hash(bundleKeyRaw);
            string bundleCacheKey = $"upload:bundle:{bundleKeyHash}";

            if (cache.TryGetValue(bundleCacheKey, out string? cachedBundleCode) && !string.IsNullOrEmpty(cachedBundleCode))
            {
                logger.LogInformation("Bundle upload response served from cache for hash: {Hash}", bundleKeyHash);
                return Ok(new { code = cachedBundleCode });
            }

            var bundleCode = await database.CheckIfBundleExistsAsync(ids);
            if (bundleCode != null)
            {
                cache.Set(bundleCacheKey, bundleCode, new MemoryCacheEntryOptions
                {
                    Size = Helpers.GetObjectSizeInBytes(bundleCode),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(config.CacheBundle)
                });
                logger.LogInformation("Bundle already exists. Returning code: {Code}", bundleCode);
                return Ok(new { code = bundleCode });
            }

            bundleCode = await Helpers.GenerateDownloadCodeAsync(database, "bundle");
            await database.InsertBundleAsync(bundleLegal, bundleCode, ((int)minGen!.Value).ToString(), ((int)maxGen!.Value).ToString(), ids);
            Helpers.InvalidateSearchCacheAsync(cache, "bundle");
            cache.Set(bundleCacheKey, bundleCode, new MemoryCacheEntryOptions
            {
                Size = Helpers.GetObjectSizeInBytes(bundleCode),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(config.CacheBundle)
            });
            logger.LogInformation("Bundle uploaded successfully. New code: {Code}", bundleCode);
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
            logger.LogInformation("GET /api/v2/gpss/download/{EntityType}/{Code} | Download: {Download}", entityType, code, download);

            if (!_supportedEntities.Contains(entityType))
            {
                logger.LogWarning("Invalid entity type: {EntityType}", entityType);
                return BadRequest(new { message = "Invalid entity type." });
            }

            await database.IncrementDownloadAsync(
                entityType == "bundles" || entityType == "bundle" ? "bundle" : "pokemon",
                code);

            if (!download)
            {
                logger.LogInformation("Download flag is false, returning Ok.");
                return Ok();
            }

            string cacheKey = $"download:{entityType}:{code}:download={download}";

            if (entityType == "pokemon")
            {
                if (cache.TryGetValue(cacheKey, out GpssPokemon? cachedPokemon) && cachedPokemon != null)
                {
                    logger.LogInformation("Pokemon download response served from cache for code: {Code}", code);
                    return Ok(cachedPokemon);
                }

                var pokemon = (await database.ListPokemonsAsync(1, 1, new Search { DownloadCode = code })).FirstOrDefault();
                if (string.IsNullOrEmpty(pokemon.DownloadCode))
                {
                    logger.LogWarning("Pokemon not found for code: {Code}", code);
                    return NotFound(new { message = "Pokemon not found." });
                }

                cache.Set(cacheKey, pokemon, new MemoryCacheEntryOptions
                {
                    Size = Helpers.GetObjectSizeInBytes(pokemon),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(config.CachePokemon)
                });
                logger.LogInformation("Pokemon download successful for code: {Code}", code);
                return Ok(pokemon);
            }
            else
            {
                if (cache.TryGetValue(cacheKey, out GpssBundle? cachedBundle) && cachedBundle != null)
                {
                    logger.LogInformation("Bundle download response served from cache for code: {Code}", code);
                    return Ok(cachedBundle);
                }

                var bundle = (await database.ListBundlesAsync(1, 1, new Search { DownloadCode = code })).FirstOrDefault();
                if (string.IsNullOrEmpty(bundle.DownloadCode))
                {
                    logger.LogWarning("Bundle not found for code: {Code}", code);
                    return NotFound(new { message = "Bundle not found." });
                }

                cache.Set(cacheKey, bundle, new MemoryCacheEntryOptions
                {
                    Size = Helpers.GetObjectSizeInBytes(bundle),
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(config.CacheBundle)
                });
                logger.LogInformation("Bundle download successful for code: {Code}", code);
                return Ok(bundle);
            }
        }
    }
}
