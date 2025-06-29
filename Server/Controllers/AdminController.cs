namespace GPSS_Server.Controllers
{
    using GPSS_Server.Datastore;
    using GPSS_Server.Models;
    using GPSS_Server.Utils;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Defines the <see cref="AdminController" />.
    /// </summary>
    [ApiController]
    [Route("/api/v2/admin")]
    [Produces("application/json")]
    [Authorize]
    public class AdminController(Database database, IMemoryCache cache) : ControllerBase
    {
        /// <summary>
        /// Defines the _supportedEntities.
        /// </summary>
        private readonly string[] _supportedEntities = ["pokemon", "bundles", "bundle"];

        /// <summary>
        /// The DeleteByCode.
        /// </summary>
        /// <param name="entityType">The entityType<see cref="string"/>.</param>
        /// <param name="code">The code<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [HttpDelete("{entityType}/{code}")]
        public async Task<IActionResult> DeleteByCode([FromRoute] string entityType, [FromRoute] string code)
        {
            if (!_supportedEntities.Contains(entityType))
            {
                return BadRequest(new { message = "Invalid entity type." });
            }

            var deleted = await database.DeleteByCodeAsync(entityType, code);
            if (deleted)
                return Accepted();
            return NotFound(new { message = $"{entityType} with code '{code}' not found." });
        }

        /// <summary>
        /// The InvalidateCache.
        /// </summary>
        /// <param name="entityType">The entityType<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [HttpPost("cache/invalidate/{entityType}")]
        public async Task<IActionResult> InvalidateCache([FromRoute] string entityType)
        {
            if (!_supportedEntities.Contains(entityType))
            {
                return BadRequest(new { message = "Invalid entity type." });
            }

            await Helpers.InvalidateSearchCacheAsync(cache, entityType);
            return Accepted();
        }

        /// <summary>
        /// The GetMetrics.
        /// </summary>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics()
        {
            return Ok(new Metrics
            {
                PokemonCount = await database.CountAsync("pokemon"),
                BundleCount = await database.CountAsync("bundle")
            });
        }
    }
}
