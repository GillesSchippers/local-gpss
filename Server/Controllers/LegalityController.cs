namespace GPSS_Server.Controllers
{
    using GPSS_Server.Config;
    using GPSS_Server.Services;
    using GPSS_Server.Utils;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Defines the <see cref="LegalityController" />.
    /// </summary>
    [ApiController]
    [Route("/api/v2/pksm")]
    public class LegalityController(ServerConfig config, IMemoryCache cache, ILogger<LegalityController> logger) : ControllerBase
    {
        /// <summary>
        /// The Check.
        /// </summary>
        /// <param name="pkmn">The pkmn<see cref="IFormFile"/>.</param>
        /// <param name="generation">The generation<see cref="string"/>.</param>
        /// <returns>The <see cref="IActionResult"/>.</returns>
        [HttpPost("legality")]
        public IActionResult Check([FromForm] IFormFile pkmn, [FromHeader] string generation)
        {
            logger.LogInformation("POST /api/v2/pksm/legality | Generation: {Generation} | File: {FileName} | Size: {Size}", generation, pkmn?.FileName, pkmn?.Length);

            if (pkmn == null || pkmn.Length == 0)
            {
                logger.LogWarning("No file uploaded.");
                return BadRequest(new { error = "No file uploaded." });
            }

            string fileHash = Helpers.ComputeSha256Hash(pkmn);
            string cacheKey = $"legality:{fileHash}:{generation}";

            if (cache.TryGetValue(cacheKey, out object? cachedResult) && cachedResult != null)
            {
                logger.LogInformation("Legality check served from cache for key: {CacheKey}", cacheKey);
                return Ok(cachedResult);
            }

            var result = PKHeXService.LegalityCheck(pkmn, Helpers.EntityContextFromString(generation));

            if (Helpers.DoesPropertyExist(result, "error"))
            {
                logger.LogWarning("Legality check failed: {Error}", (object)result.error ?? String.Empty);
                return BadRequest(result);
            }

            cache.Set(cacheKey, (object)result, new MemoryCacheEntryOptions
            {
                Size = Helpers.GetObjectSizeInBytes(result),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(config.CachePokemon)
            });

            logger.LogInformation("Legality check successful. Response cached under key: {CacheKey}", cacheKey);
            return Ok(result);
        }

        /// <summary>
        /// The Legalize.
        /// </summary>
        /// <param name="pkmn">The pkmn<see cref="IFormFile"/>.</param>
        /// <param name="generation">The generation<see cref="string"/>.</param>
        /// <param name="version">The version<see cref="string"/>.</param>
        /// <returns>The <see cref="IActionResult"/>.</returns>
        [HttpPost("legalize")]
        public IActionResult Legalize([FromForm] IFormFile pkmn, [FromHeader] string generation, [FromHeader] string version)
        {
            logger.LogInformation("POST /api/v2/pksm/legalize | Generation: {Generation} | Version: {Version} | File: {FileName} | Size: {Size}", generation, version, pkmn?.FileName, pkmn?.Length);

            if (pkmn == null || pkmn.Length == 0)
            {
                logger.LogWarning("No file uploaded.");
                return BadRequest(new { error = "No file uploaded." });
            }

            string fileHash = Helpers.ComputeSha256Hash(pkmn);
            string cacheKey = $"legalize:{fileHash}:{generation}:{version}";

            if (cache.TryGetValue(cacheKey, out object? cachedResult) && cachedResult != null)
            {
                logger.LogInformation("Legalize served from cache for key: {CacheKey}", cacheKey);
                return Ok(cachedResult);
            }

            var result = PKHeXService.Legalize(pkmn, Helpers.EntityContextFromString(generation),
                Helpers.GameVersionFromString(version));

            if (Helpers.DoesPropertyExist(result, "error"))
            {
                logger.LogWarning("Legalize failed: {Error}", (object)result.error ?? String.Empty);
                return BadRequest(result);
            }

            cache.Set(cacheKey, (object)result, new MemoryCacheEntryOptions
            {
                Size = Helpers.GetObjectSizeInBytes(result),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(config.CachePokemon)
            });

            logger.LogInformation("Legalize successful. Response cached under key: {CacheKey}", cacheKey);
            return Ok(result);
        }
    }
}
