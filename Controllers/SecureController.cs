using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SecureController : ControllerBase
    {
        [Authorize]
        [HttpGet("SecureEndpoint")]
        public IActionResult SecureEndpoint()
        {
            return Ok(new { message = "You have access to this secure endpoint!" });
        }
    }
}