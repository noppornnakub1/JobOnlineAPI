using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenerateFormController : ControllerBase
    {
        [HttpGet("generate-form")]
        public IActionResult GenerateForm()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var form = new PersonalDetailsForm
            {
                JobTitle = "FullStack Developer",
                FullNameTH = "ธนากร ดวงแก้ว",
                FullNameENG = "Thanakorn Duangkaew",
                NickNameTH = "คอปเตอร์",
                NickNameENG = "Copter",
                Weight = "94.2",
                height = "174",
                Salary = "60,000",
                JobStartDate = "08/09/2025",
                CurrentAddress = "219 ซอย รัชดาภิเษก 32",
                CurrentDistrict = "จตุจักร",
                CurrentSubDistrict = "จตุจักร",
                CurrentProvince = "กรุงเทพมหานคร",
                ZipCode = "10900",
                BirthDate = "08/10/1998",
                Age = 26,
                IDCard = "156010055226",
                Phone = "0979595858",
                Email = "TestSystem@gmail.com"
            };

            var pdf = form.GeneratePdf();
            return File(pdf, "application/pdf", "personal-details.pdf");
        }
    }
}
