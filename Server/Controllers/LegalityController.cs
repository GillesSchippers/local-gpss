using GPSS_Server.Services;
using GPSS_Server.Utils;
using Microsoft.AspNetCore.Mvc;

namespace GPSS_Server.Controllers
{
    [ApiController]
    [Route("/api/v2/pksm")]
    public class LegalityController : ControllerBase
    {
        [HttpPost("legality")]
        public IActionResult Check([FromForm] IFormFile pkmn, [FromHeader] string generation)
        {
            var result = PKhexService.LegalityCheck(pkmn, Helpers.EntityContextFromString(generation));

            if (Helpers.DoesPropertyExist(result, "error"))
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("legalize")]
        public IActionResult Legalize([FromForm] IFormFile pkmn, [FromHeader] string generation, [FromHeader] string version)
        {
            var result = PKhexService.Legalize(pkmn, Helpers.EntityContextFromString(generation),
                Helpers.GameVersionFromString(version));

            if (Helpers.DoesPropertyExist(result, "error"))
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}