using Core.DTOs;
using Core.Helpers;
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

        [HttpPost("schemes/{fundName}/{schemeId}/approval")]
        public async Task<IActionResult> AddApprovedScheme(string fundName, string schemeId, [FromQuery] bool isApproved)
        {
            try
            {
                var (success, message) = await amfiRepository.AddApprovedSchemeAsync(fundName, schemeId, isApproved);

                return Ok(new
                {
                    FundName = fundName,
                    SchemeId = schemeId,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    FundName = fundName,
                    SchemeId = schemeId,
                    Approved = isApproved,
                    Error = "An error occurred while updating scheme approval.",
                    Details = ex.Message
                });
            }
        }

        [HttpPut("schemes/{fundId}/{schemeId}")]
        public async Task<IActionResult> UpdateApprovedScheme(string fundId, string schemeId, [FromQuery] bool isApproved)
        {
            var (success, message) = await amfiRepository.UpdateApprovedSchemeAsync(fundId, schemeId, isApproved);

            return Ok(new
            {
                FundId = fundId,
                SchemeId = schemeId,
                Approved = isApproved,
                Success = success,
                Message = message
            });
        }

        [HttpPut("funds/{fundId}")]
        public async Task<IActionResult> UpdateApprovedFund(string fundId, [FromQuery] bool isApproved)
        {
            try
            {
                var (success, message) = await amfiRepository.UpdateApprovedFundAsync(fundId, isApproved);

                if (!success && message == "Record not found")
                {
                    return NotFound(new
                    {
                        FundId = fundId,
                        Approved = isApproved,
                        Success = success,
                        Message = message
                    });
                }

                return Ok(new
                {
                    FundId = fundId,
                    Approved = isApproved,
                    Success = success,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    FundId = fundId,
                    Approved = isApproved,
                    Success = false,
                    Error = "An error occurred while updating fund approval.",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("schemes/by-today")]
        public async Task<IActionResult> GetTodayAndPreviousWorkingDaySchemes()
        {
            var today = DateTime.Today;
            var (startDate, endDate) = AmfiDataHelper.GetLastThreeWorkingDays(today);

            try
            {
                var rawSchemes = await amfiRepository.GetSchemesByDateRangeAsync(startDate, endDate);

                var result = SchemeTransformer.TransformSchemes(rawSchemes);

                if (!result.IsSuccess)
                {
                    return NotFound(new
                    {
                        StartDate = startDate,
                        EndDate = endDate,
                        Message = result.Message
                    });
                }
                return Ok(new
                {
                    StartDate = result.Date2, // earliest previous date
                    EndDate = result.Date1,   // latest date (today)
                    Count = result.Count,
                    Schemes = result.Schemes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Error = "An error occurred while fetching schemes.",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("schemes/by-date-range")]
        public async Task<IActionResult> GetSchemes(DateTime startDate, DateTime endDate)
        {
            var workingResult = AmfiDataHelper.GetWorkingDates(startDate, endDate);

            if (!workingResult.IsSuccess)
            {
                return Ok(new SchemeResponseDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Message = "No working days available in the selected range",
                    Schemes = new List<SchemeDto>()
                });
            }

            var allDates = workingResult.Dates;

            var navs = await amfiRepository.GetSchemesByDateRangeAsync(
                workingResult.StartWorkingDate,
                workingResult.EndWorkingDate
            );

            var schemes = SchemeTransformer.BuildSchemeHistory(navs, allDates, startDate, endDate);

            return Ok(new SchemeResponseDto
            {
                StartDate = workingResult.StartWorkingDate,
                EndDate = workingResult.EndWorkingDate,
                Schemes = schemes,
                Message = workingResult.Message
            });
        }
    }
}
