using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace JobOnlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HRStaffController(IHRStaffRepository hrStaffRepository) : ControllerBase
    {
        private readonly IHRStaffRepository _hrStaffRepository = hrStaffRepository;

        [HttpGet]
        public async Task<IEnumerable<HRStaff>> GetHRStaff()
        {
            return await _hrStaffRepository.GetAllHRStaffAsync();
        }

        [HttpGet("{email}")]
        public async Task<ActionResult<HRStaff>> GetHRStaffByEmail(string email)
        {
            var hrStaff = await _hrStaffRepository.GetHRStaffByEmailAsync(email);

            if (hrStaff == null)
            {
                return NotFound();
            }

            return hrStaff;
        }

        [HttpGet("GetStaffNew")]
        public async Task<IActionResult> GetStaffNew([FromQuery] string? email)
        {
            var result = await _hrStaffRepository.GetAllStaffAsyncNew(email);
            return Ok(result);
        }
    }
}