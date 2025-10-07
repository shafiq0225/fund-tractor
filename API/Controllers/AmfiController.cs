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
        // clean up is required
        [HttpPost("importfile")]
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

        [HttpPost("import/url")]
        public async Task<IActionResult> DownloadAndSaveFromUrlAsync([FromBody] ImportUrlRequest fileUrl)
        {
            if (fileUrl == null || string.IsNullOrWhiteSpace(fileUrl.FileUrl))
                return BadRequest(new { Message = "File URL is required." });

            try
            {
                // ✅ Ensure it’s from AMFI domain
                if (!fileUrl.FileUrl.StartsWith("https://portal.amfiindia.com", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { Message = "Invalid URL. Only AMFI URLs are allowed." });

                Uri uri;
                try
                {
                    uri = new Uri(fileUrl.FileUrl);
                }
                catch (UriFormatException)
                {
                    return BadRequest(new { Message = "Invalid URL format." });
                }

                // ✅ Ensure it’s a .txt file
                var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
                if (extension != ".txt")
                    return BadRequest(new { Message = "Only .txt files are allowed." });

                using var httpClient = new HttpClient();

                HttpResponseMessage response;
                try
                {
                    response = await httpClient.GetAsync(uri);
                }
                catch (HttpRequestException ex)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable,
                        new { Message = "Unable to reach AMFI server.", Details = ex.Message });
                }

                if (!response.IsSuccessStatusCode)
                    return BadRequest(new { Message = "Unable to download the file from the provided URL." });

                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType != null && contentType != "text/plain")
                    return BadRequest(new { Message = "Invalid file type. Only plain text (.txt) files are allowed." });

                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content))
                    return BadRequest(new { Message = "Downloaded file is empty." });

                // ✅ Save in DB via repository
                await amfiRepository.ImportAmfiDataAsync(content);

                return Ok(new { Message = "Imported successfully from AMFI URL" });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An unexpected error occurred during import.", Details = ex.Message });
            }
        }

        // clean up is required
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            await amfiRepository.ImportAmfiDataFromExcelAsync(memoryStream.ToArray());
            return Ok(new { message = "AMFI Excel data imported successfully." });
        }

        [HttpPost("import/file")]
        public async Task<IActionResult> UploadAndSaveFromFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No file uploaded." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            try
            {
                switch (extension)
                {
                    case ".txt":
                        string content;
                        using (var reader = new StreamReader(file.OpenReadStream()))
                        {
                            content = await reader.ReadToEndAsync();
                        }

                        if (string.IsNullOrWhiteSpace(content))
                            return BadRequest(new { Message = "Uploaded file is empty." });

                        await amfiRepository.ImportAmfiDataAsync(content);
                        return Ok(new { Message = "TXT file imported successfully." });

                    case ".xls":
                    case ".xlsx":
                        using (var memoryStream = new MemoryStream())
                        {
                            await file.CopyToAsync(memoryStream);
                            await amfiRepository.ImportAmfiDataFromExcelAsync(memoryStream.ToArray());
                        }
                        return Ok(new { Message = "Excel file imported successfully." });

                    default:
                        return BadRequest(new { Message = "Unsupported file type. Please upload .txt or .xlsx file." });
                }
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

        [HttpPost("addapprovedscheme")]
        public async Task<IActionResult> AddApprovedScheme([FromBody] ApprovedSchemeDto approvedSchemeDto)
        {
            if (string.IsNullOrWhiteSpace(approvedSchemeDto.FundName))
                return BadRequest(new { Message = "Fund name is required." });

            if (string.IsNullOrWhiteSpace(approvedSchemeDto.SchemeId))
                return BadRequest(new { Message = "Scheme ID is required." });

            try
            {
                var (success, message) = await amfiRepository.AddApprovedSchemeAsync(approvedSchemeDto);

                if (!success)
                {
                    return Conflict(new
                    {
                        approvedSchemeDto.FundName,
                        approvedSchemeDto.SchemeId,
                        approvedSchemeDto.IsApproved,
                        Message = message
                    });
                }

                return Ok(new
                {
                    approvedSchemeDto.FundName,
                    approvedSchemeDto.SchemeId,
                    approvedSchemeDto.IsApproved,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    approvedSchemeDto.FundName,
                    approvedSchemeDto.SchemeId,
                    approvedSchemeDto.IsApproved,
                    Error = "An unexpected error occurred while updating scheme approval.",
                    Details = ex.Message
                });
            }
        }

        [HttpPut("updateapprovedscheme")]
        public async Task<IActionResult> UpdateApprovedScheme([FromBody] UpdateSchemeRequest schemeApprovalDto)
        {
            if (string.IsNullOrWhiteSpace(schemeApprovalDto.FundId))
                return BadRequest(new { Message = "Fund ID is required." });

            if (string.IsNullOrWhiteSpace(schemeApprovalDto.SchemeId))
                return BadRequest(new { Message = "Scheme ID is required." });

            try
            {
                var (success, message) = await amfiRepository.UpdateApprovedSchemeAsync(schemeApprovalDto.FundId, schemeApprovalDto.SchemeId, schemeApprovalDto.IsApproved);

                if (!success)
                {
                    return NotFound(new
                    {
                        schemeApprovalDto.FundId,
                        schemeApprovalDto.SchemeId,
                        schemeApprovalDto.IsApproved,
                        Success = false,
                        Message = message
                    });
                }

                return Ok(new
                {
                    schemeApprovalDto.FundId,
                    schemeApprovalDto.SchemeId,
                    schemeApprovalDto.IsApproved,
                    Success = true,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    schemeApprovalDto.FundId,
                    schemeApprovalDto.SchemeId,
                    schemeApprovalDto.IsApproved,
                    Success = false,
                    Error = "An unexpected error occurred while updating scheme approval.",
                    Details = ex.Message
                });
            }
        }

        [HttpPut("updateapprovedfund")]
        public async Task<IActionResult> UpdateApprovedFund([FromBody] UpdateFundRequest updateFundRequest)
        {
            if (string.IsNullOrWhiteSpace(updateFundRequest.FundId))
            {
                return BadRequest(new
                {
                    updateFundRequest.FundId,
                    updateFundRequest.IsApproved,
                    Success = false,
                    Message = "FundId is required."
                });
            }

            try
            {
                var (success, message) = await amfiRepository.UpdateApprovedFundAsync(updateFundRequest.FundId, updateFundRequest.IsApproved);

                if (!success)
                {
                    if (message == "Record not found")
                    {
                        return NotFound(new
                        {
                            updateFundRequest.FundId,
                            updateFundRequest.IsApproved,
                            Success = false,
                            Message = message
                        });
                    }

                    // Generic failure from repo (e.g., "Concurrency conflict" or "Database error")
                    return BadRequest(new
                    {
                        updateFundRequest.FundId,
                        updateFundRequest.IsApproved,
                        Success = false,
                        Message = message
                    });
                }

                return Ok(new
                {
                    updateFundRequest.FundId,
                    updateFundRequest.IsApproved,
                    Success = true,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    updateFundRequest.FundId,
                    updateFundRequest.IsApproved,
                    Success = false,
                    Message = "An unexpected error occurred while updating fund approval.",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("schemes/today")]
        public async Task<IActionResult> GetDailySchemesWithRank()
        {
            try
            {
                var workingResult = AmfiDataHelper.GetLastTradingDays();

                if (!workingResult.Success)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = workingResult.Message
                    });
                }

                var (success, message, navs) = await amfiRepository
                    .GetSchemesByDateRangeAsync(workingResult.StartWorkingDate, workingResult.EndWorkingDate);

                if (!success)
                {
                    if (message.Contains("No records", StringComparison.OrdinalIgnoreCase))
                    {
                        return NotFound(new
                        {
                            workingResult.StartWorkingDate,
                            workingResult.EndWorkingDate,
                            Success = false,
                            Message = message
                        });
                    }

                    return BadRequest(new
                    {
                        workingResult.StartWorkingDate,
                        workingResult.EndWorkingDate,
                        Success = false,
                        Message = message
                    });
                }

                // Success case
                var schemes = SchemeBuilder.BuildSchemeHistoryForDaily(navs!, workingResult.EndWorkingDate);

                var schemesWithRank = schemes
                    .Select(s => new
                    {
                        Scheme = s,
                        LatestPercentage = s.History
                            .OrderByDescending(h => h.Date)
                            .Select(h => decimal.TryParse(h.Percentage, out var pct) ? pct : 0m)
                            .FirstOrDefault()
                    })
                    .OrderByDescending(s => s.LatestPercentage)
                    .ThenBy(s => s.Scheme.FundName) // ensure stable ordering
                    .Select((s, index) =>
                    {
                        // Rank logic: top 3 = 1,2,3; all others = 4
                        s.Scheme.Rank = index < 3 ? index + 1 : 4;
                        return s.Scheme;
                    })
                    .OrderBy(s => s.Rank) // Final rank-based ordering
                    .ToList();

                return Ok(new SchemeResponseDto
                {
                    StartDate = schemesWithRank.SelectMany(s => s.History).Min(h => h.Date),
                    EndDate = schemesWithRank.SelectMany(s => s.History).Max(h => h.Date),
                    Schemes = schemesWithRank,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Error = "An unexpected error occurred while fetching schemes.",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("schemes")]
        public async Task<IActionResult> GetSchemes([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var workingResult = AmfiDataHelper.GetWorkingDates(startDate, endDate);

                if (!workingResult.Success)
                {
                    return Ok(new SchemeResponseDto
                    {
                        StartDate = startDate,
                        EndDate = endDate,
                        Message = workingResult.Message ?? "No working days available in the selected range",
                        Schemes = new List<SchemeDto>()
                    });
                }

                // Repository call wrapped
                var (success, message, navs) = await amfiRepository.GetSchemesByDateRangeAsync(
                    workingResult.StartWorkingDate,
                    workingResult.EndWorkingDate
                );

                if (!success || navs == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        Error = "Failed to fetch schemes from repository.",
                        Details = message
                    });
                }

                var schemes = SchemeBuilder.BuildSchemeHistory(navs, workingResult.Dates, startDate, endDate);

                return Ok(new SchemeResponseDto
                {
                    StartDate = workingResult.StartWorkingDate,
                    EndDate = workingResult.EndWorkingDate,
                    Schemes = schemes,
                    Message = string.IsNullOrEmpty(workingResult.Message) ? "Success" : workingResult.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Error = "An unexpected error occurred while fetching schemes.",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("schemes/compare")]
        public async Task<IActionResult> CompareSchemes([FromQuery] string schemeCode1, [FromQuery] string schemeCode2)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(schemeCode1) || string.IsNullOrWhiteSpace(schemeCode2))
                {
                    return BadRequest(new { Message = "Both schemeCode1 and schemeCode2 must be provided." });
                }

                if (schemeCode1.Trim().Equals(schemeCode2.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { Message = "Comparison not allowed: schemeCode1 and schemeCode2 cannot be the same." });
                }

                var today = DateTime.Today;
                var validDates = AmfiDataHelper.GetWorkingDates(today, 10);

                if (validDates == null || validDates.Count == 0)
                {
                    return BadRequest(new { Message = "No valid trading days found for comparison." });
                }

                var (success, message, navs) = await amfiRepository.GetSchemesByDateRangeAsync(validDates.Min(), validDates.Max());

                if (!success || navs == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        Message = message ?? "Failed to retrieve NAV records."
                    });
                }

                var scheme1Records = navs.Where(x => x.SchemeCode == schemeCode1).ToList();
                var scheme2Records = navs.Where(x => x.SchemeCode == schemeCode2).ToList();

                if (!scheme1Records.Any() || !scheme2Records.Any())
                {
                    return NotFound(new { Message = "One or both scheme codes do not exist or have no NAV records." });
                }

                var response = new
                {
                    Scheme1 = new
                    {
                        SchemeCode = schemeCode1,
                        SchemeName = scheme1Records.FirstOrDefault()?.SchemeName ?? "Unknown",
                        Yesterday = AmfiDataHelper.CalculateChange(scheme1Records, 1),
                        LastWeek = AmfiDataHelper.CalculateChange(scheme1Records, 5),
                        Last10Days = AmfiDataHelper.CalculateChange(scheme1Records, 10)
                    },
                    Scheme2 = new
                    {
                        SchemeCode = schemeCode2,
                        SchemeName = scheme2Records.FirstOrDefault()?.SchemeName ?? "Unknown",
                        Yesterday = AmfiDataHelper.CalculateChange(scheme2Records, 1),
                        LastWeek = AmfiDataHelper.CalculateChange(scheme2Records, 5),
                        Last10Days = AmfiDataHelper.CalculateChange(scheme2Records, 10)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "Unexpected error occurred while comparing schemes.",
                    Details = ex.Message
                });
            }
        }

        [HttpGet("schemeslist")]
        public async Task<IActionResult> GetSchemes()
        {
            try
            {
                var response = await amfiRepository.GetSchemesListAsync();

                if (!response.Success)
                {
                    return BadRequest(new { message = response.Message });
                }

                if (response.Data == null || !response.Data.Any())
                {
                    return NotFound(new { message = "No schemes found." });
                }

                return Ok(new
                {
                    message = response.Message,
                    data = response.Data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }

        [HttpGet("schemeperformance")]
        public async Task<IActionResult> GetSchemePerformance([FromQuery] string schemeCode)
        {
            try
            {
                if (string.IsNullOrEmpty(schemeCode))
                {
                    return BadRequest(new { message = "Scheme code is required." });
                }

                var response = await amfiRepository.GetSchemePerformance(schemeCode);

                if (!response.Success)
                {
                    return BadRequest(new { message = response.Message });
                }

                if (response.schemeDetails == null || !response.schemeDetails.Any())
                {
                    return NotFound(new { message = "No schemes found." });
                }

                // Transform the data
                var transformedData = amfiRepository.TransformToPerformanceResponse(response.schemeDetails);

                return Ok(new
                {
                    message = "Performance data retrieved successfully.",
                    data = transformedData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }
    }
}
