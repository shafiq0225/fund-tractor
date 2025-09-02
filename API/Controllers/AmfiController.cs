using Core.DTOs;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmfiController(IAmfiRepository amfiRepository) : ControllerBase
    {
        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile rawdata)
        {
            if (rawdata == null || rawdata.Length == 0)
                return BadRequest("No file uploaded.");

            var extension = Path.GetExtension(rawdata.FileName).ToLowerInvariant();
            if (extension != ".txt")
                return BadRequest("Only .txt files are allowed.");

            try
            {
                using var reader = new StreamReader(rawdata.OpenReadStream());
                var content = await reader.ReadToEndAsync();

                await amfiRepository.ImportAmfiDataAsync(content);

                return Ok(new { Message = "Imported successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred during import", Details = ex.Message });
            }
        }

        [HttpPost("import-amfi-url")]
        public async Task<IActionResult> ImportAmfiFromUrl([FromBody] ImportUrlRequest fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl.FileUrl))
                return BadRequest("File URL is required.");

            try
            {
                // ✅ Ensure it’s from AMFI domain
                if (!fileUrl.FileUrl.StartsWith("https://www.amfiindia.com", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Invalid URL. Only AMFI URLs are allowed.");

                // ✅ Ensure it’s a .txt file
                var extension = Path.GetExtension(new Uri(fileUrl.FileUrl).AbsolutePath).ToLowerInvariant();
                if (extension != ".txt")
                    return BadRequest("Only .txt files are allowed.");

                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(fileUrl.FileUrl);

                if (!response.IsSuccessStatusCode)
                    return BadRequest("Unable to download the file from the provided URL.");

                // ✅ Check MIME type
                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType != null && contentType != "text/plain")
                    return BadRequest("Invalid file type. Only plain text (.txt) files are allowed.");

                // ✅ Read content
                var content = await response.Content.ReadAsStringAsync();

                // ✅ Save in DB via repository
                await amfiRepository.ImportAmfiDataAsync(content);

                return Ok(new { Message = "Imported successfully from AMFI URL" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred during import", Details = ex.Message });
            }
        }


        [HttpPost("funds/{fundId}/approval")]
        public async Task<IActionResult> UpdateFundApprovalAsync(string fundId, [FromQuery] bool isApproved)
        {
            try
            {
                bool success = await amfiRepository.SetFundApprovalAsync(fundId, isApproved);

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
                bool success = await amfiRepository.SetSchemeApprovalAsync(schemeId, isApproved);

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
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    SchemeId = schemeId,
                    Approved = isApproved,
                    Error = "An error occurred while updating scheme approval.",
                    Details = ex.Message
                });
            }
        }
    }
}
