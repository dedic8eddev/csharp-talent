using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ikiru.Parsnips.Api.Controllers.Version
{
    [AllowAnonymous]
    [ApiController]
    [Route("/api/[controller]")]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var v = GetType().Assembly.GetName().Version;
            return Ok(new { Version = v.ToString() });
        }
    }
}
