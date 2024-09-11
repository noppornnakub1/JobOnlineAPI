using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.Services;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StylesController : Controller
    {
        private readonly IUserService _userService;

        public StylesController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> GetStyleValue(string key)
        {
            var styleValue = await _userService.GetStyleValueAsync(key);
            if (styleValue != null)
            {
                return Ok(styleValue);
            }
            else
            {
                return NotFound($"Style value for key '{key}' not found.");
            }
        }
    }
}
