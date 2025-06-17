using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController(IAdminRepository adminRepository) : ControllerBase
    {
        private readonly IAdminRepository _adminRepository = adminRepository;

        [HttpPost("add-admin")]
        public async Task<IActionResult> AddAdminUser(AdminUser admin)
        {
            var adminId = await _adminRepository.AddAdminUserAsync(admin);
            return Ok(new { AdminId = adminId });
        }

        [HttpPost("verify-password")]
        public async Task<IActionResult> VerifyPassword(string username, string password)
        {
            var isPasswordValid = await _adminRepository.VerifyPasswordAsync(username, password);
            if (!isPasswordValid)
            {
                return Unauthorized("Invalid username or password.");
            }

            return Ok("Password verified.");
        }
    }
}