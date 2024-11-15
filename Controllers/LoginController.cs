using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.Services;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IUserService _userService;

        public LoginController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                var user = await _userService.AuthenticateAsync(loginRequest.Username, loginRequest.Password);

                if (user != null)
                {
                    return Ok(new { user.Username, user.Role });
                }

                return Unauthorized("Invalid username or password.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login failed: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }
    }

    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}