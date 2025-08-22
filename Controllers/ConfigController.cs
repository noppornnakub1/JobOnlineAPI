using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.Services;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController(IUserService userService) : ControllerBase
    {
        private readonly IUserService _userService = userService;

        [HttpGet("{key}")]
        public async Task<IActionResult> GetConfigValue(string key)
        {
            var configValue = await _userService.GetConfigValueAsync(key);
            if (configValue != null)
            {
                return Ok(configValue);
            }
            else
            {
                return NotFound($"Config value for key '{key}' not found.");
            }
        }
    }
}