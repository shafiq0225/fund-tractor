using Infrastructure.Services;
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
        public async Task<IActionResult> Import(IFormFile rawdata)
        {
            if (rawdata == null || rawdata.Length == 0)
                return BadRequest("No file uploaded.");

            try
            {
                using var reader = new StreamReader(rawdata.OpenReadStream());
                var content = await reader.ReadToEndAsync();

                await amfiImportService.ImportAmfiDataAsync(content);

                return Ok(new { Message = "Imported successfully" });
            }
            catch (Exception ex)
            {
                // Log error here if you have logging
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred during import", Details = ex.Message });
            }
        }

        [HttpPost("funds/{fundId}/approval")]
        public async Task<IActionResult> UpdateFundApprovalAsync(string fundId, [FromQuery] bool isApproved)
        {
            try
            {
                bool success = await amfiImportService.SetFundApprovalAsync(fundId, isApproved);

                if (!success)
                    return NotFound($"Fund with id {fundId} not found.");

                return Ok(new
                {
                    FundId = fundId,
                    Approved = isApproved,
                    Message = isApproved
                        ? "Fund approved successfully."
                        : "Fund unapproved successfully."
                });
            }
            catch (Exception ex)
            {
                // Log error if you have ILogger
                // _logger.LogError(ex, "Error updating approval for fund {FundId}", fundId);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    FundId = fundId,
                    Approved = isApproved,
                    Error = "An error occurred while updating fund approval.",
                    Details = ex.Message
                });
            }
        }

        [HttpPost("schemes/{schemeId}/approval")]
        public async Task<IActionResult> UpdateSchemeApprovalAsync(string schemeId, [FromQuery] bool isApproved)
        {
            try
            {
                bool success = await amfiImportService.SetSchemeApprovalAsync(schemeId, isApproved);

                if (!success)
                    return NotFound($"Scheme with id {schemeId} not found.");

                return Ok(new
                {
                    SchemeId = schemeId,
                    Approved = isApproved,
                    Message = isApproved
                        ? "Scheme approved successfully."
                        : "Scheme unapproved successfully."
                });
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error updating scheme approval for {SchemeId}", schemeId);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    SchemeId = schemeId,
                    Approved = isApproved,
                    Error = "An error occurred while updating scheme approval.",
                    Details = ex.Message // ⚠️ hide in prod
                });
            }
        }
    }
}
