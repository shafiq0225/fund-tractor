using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmfiController : ControllerBase
    {
        private readonly AmfiImportService amfiImportService;

        public AmfiController(AmfiImportService amfiImportService)
        {
            this.amfiImportService = amfiImportService;
        }

        [HttpPost("import")]
        public IActionResult Import(IFormFile csvLines)
        {
            using var reader = new StreamReader(csvLines.OpenReadStream());
            var content = reader.ReadToEnd();

            amfiImportService.ImportAmfiData(content);
            return Ok("Imported successfully");
        }

        [HttpPost("set-fund-approval/{fundId}")]
        public IActionResult SetFundApproval(string fundId, [FromQuery] bool isApproved)
        {
            amfiImportService.SetFundApproval(fundId, isApproved);
            return Ok(isApproved ? "Fund approved" : "Fund unapproved");
        }

        [HttpPost("set-scheme-approval/{schemeId}")]
        public IActionResult SetSchemeApproval(string schemeId, [FromQuery] bool isApproved)
        {
            amfiImportService.SetSchemeApproval(schemeId, isApproved);
            return Ok(isApproved ? "Scheme approved" : "Scheme unapproved");
        }
    }
}
