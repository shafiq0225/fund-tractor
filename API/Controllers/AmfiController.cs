using Core.DTOs;
using Core.Helpers;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                return BadRequest(new { Message = "No file uploaded." });

            var extension = Path.GetExtension(rawdata.FileName).ToLowerInvariant();
            if (extension != ".txt")
                return BadRequest(new { Message = "Only .txt files are allowed." });

            try
            {
                string content;
                using (var reader = new StreamReader(rawdata.OpenReadStream()))
                {
                    content = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(content))
                    return BadRequest(new { Message = "Uploaded file is empty." });

                await amfiRepository.ImportAmfiDataAsync(content);

                return Ok(new { Message = "Imported successfully" });
            }
            catch (FormatException fex)
            {
                return BadRequest(new { Message = "File contains invalid data format.", Details = fex.Message });
            }
            catch (DbUpdateException dbex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "Database error during import.", Details = dbex.InnerException?.Message ?? dbex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An unexpected error occurred during import.", Details = ex.Message });
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
            var workingResult = AmfiDataHelper.GetLastTradingDays();

            try
            {
                var allDates = workingResult.Dates;

                var navs = await amfiRepository.GetSchemesByDateRangeAsync(workingResult.StartWorkingDate, workingResult.EndWorkingDate);

                var schemes = SchemeBuilder.BuildSchemeHistoryForDaily(navs, workingResult.EndWorkingDate);

                return Ok(new SchemeResponseDto
                {
                    StartDate = workingResult.StartWorkingDate,
                    EndDate = workingResult.EndWorkingDate,
                    Schemes = schemes,
                    Message = workingResult.Message
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

            var schemes = SchemeBuilder.BuildSchemeHistory(navs, allDates, startDate, endDate);

            return Ok(new SchemeResponseDto
            {
                StartDate = workingResult.StartWorkingDate,
                EndDate = workingResult.EndWorkingDate,
                Schemes = schemes,
                Message = workingResult.Message
            });
        }

        [HttpGet("compare-two-schemes")]
        public async Task<IActionResult> CompareSchemes([FromQuery] string schemeCode1, [FromQuery] string schemeCode2)
        {
            // ✅ Require both codes
            if (string.IsNullOrWhiteSpace(schemeCode1) || string.IsNullOrWhiteSpace(schemeCode2))
            {
                return BadRequest(new
                {
                    Message = "Both schemeCode1 and schemeCode2 must be provided."
                });
            }

            // ✅ Reject if both codes are the same
            if (schemeCode1.Trim().Equals(schemeCode2.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    Message = "Comparison not allowed: schemeCode1 and schemeCode2 can be the same."
                });
            }


            var today = DateTime.Today;
            var validDates = AmfiDataHelper.GetWorkingDates(today, 10);

            var navs = await amfiRepository.GetSchemesByDateRangeAsync(validDates.Min(), validDates.Max());
            var scheme1Records = navs.Where(x => x.SchemeCode == schemeCode1).ToList();
            var scheme2Records = navs.Where(x => x.SchemeCode == schemeCode2).ToList();

            if (scheme1Records.Count == 0 || scheme2Records.Count == 0)
            {
                return NotFound(new
                {
                    Message = "One or both scheme codes do not exist or have no NAV records."
                });
            }

            var response = new
            {
                Scheme1 = new
                {
                    SchemeCode = schemeCode1,
                    SchemeName = scheme1Records.FirstOrDefault()?.SchemeName,
                    Yesterday = AmfiDataHelper.CalculateChange(scheme1Records, 1),
                    LastWeek = AmfiDataHelper.CalculateChange(scheme1Records, 5),
                    Last10Days = AmfiDataHelper.CalculateChange(scheme1Records, 10)
                },
                Scheme2 = new
                {
                    SchemeCode = schemeCode2,
                    SchemeName = scheme2Records.FirstOrDefault()?.SchemeName,
                    Yesterday = AmfiDataHelper.CalculateChange(scheme2Records, 1),
                    LastWeek = AmfiDataHelper.CalculateChange(scheme2Records, 5),
                    Last10Days = AmfiDataHelper.CalculateChange(scheme2Records, 10)
                }
            };

            return Ok(response);
        }
    }
}
