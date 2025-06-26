namespace GPSS_Server.Controllers
{
    using GPSS_Server.Services;
    using GPSS_Server.Utils;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Defines the <see cref="LegalityController" />.
    /// </summary>
    [ApiController]
    [Route("/api/v2/pksm")]
    public class LegalityController : ControllerBase
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
            var result = PKHeXService.LegalityCheck(pkmn, Helpers.EntityContextFromString(generation));

            if (Helpers.DoesPropertyExist(result, "error"))
            {
                return BadRequest(result);
            }

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
            var result = PKHeXService.Legalize(pkmn, Helpers.EntityContextFromString(generation),
                Helpers.GameVersionFromString(version));

            if (Helpers.DoesPropertyExist(result, "error"))
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
