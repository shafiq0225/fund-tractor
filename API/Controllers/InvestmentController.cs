using Core.DTOs.Investment;
using Core.Interfaces.Auth;
using Core.Interfaces.InvestmentRepo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvestmentController : ControllerBase
    {
        private readonly IInvestmentRepository _investmentRepository;
        private readonly IUserService _userService;

        public InvestmentController(IInvestmentRepository investmentRepository, IUserService userService)
        {
            _investmentRepository = investmentRepository;
            _userService = userService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<InvestmentResponseDto>> CreateInvestment([FromForm] CreateInvestmentDto createDto)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                    return Unauthorized(new { Message = "Unable to identify current user." });

                var currentUserName = await _userService.GetCurrentUserNameAsync();
                if (string.IsNullOrEmpty(currentUserName))
                    return Unauthorized(new { Message = "Unable to get current user name." });

                var result = await _investmentRepository.CreateInvestmentAsync(createDto, currentUserId.Value, currentUserName);

                if (!result.Success)
                    return BadRequest(new InvestmentResponseDto { Success = false, Message = result.Message });

                var investmentDto = MapToInvestmentDto(result.Data!);
                return Ok(new InvestmentResponseDto { Success = true, Message = result.Message, Data = investmentDto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new InvestmentResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while creating investment.",
                });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<InvestmentResponseDto>> UpdateInvestment(int id, [FromForm] UpdateInvestmentDto updateDto)
        {
            try
            {
                var result = await _investmentRepository.UpdateInvestmentAsync(id, updateDto);

                if (!result.Success)
                    return BadRequest(new InvestmentResponseDto { Success = false, Message = result.Message });

                var investmentDto = MapToInvestmentDto(result.Data!);
                return Ok(new InvestmentResponseDto { Success = true, Message = result.Message, Data = investmentDto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new InvestmentResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while updating investment.",
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InvestmentResponseDto>> GetInvestment(int id)
        {
            try
            {
                var result = await _investmentRepository.GetInvestmentByIdAsync(id);

                if (!result.Success)
                    return NotFound(new InvestmentResponseDto { Success = false, Message = result.Message });

                var investmentDto = MapToInvestmentDto(result.Data!);
                return Ok(new InvestmentResponseDto { Success = true, Message = result.Message, Data = investmentDto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new InvestmentResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving investment.",
                });
            }
        }

        [HttpGet("investor/{investorId}")]
        public async Task<ActionResult<InvestmentListResponseDto>> GetInvestmentsByInvestor(int investorId)
        {
            try
            {
                // Validate user access first
                var accessResult = await ValidateUserAccess(investorId);
                if (accessResult is not OkResult)
                    return accessResult;

                // Rest of the existing code...
                var currentUserId = GetCurrentUserId();
                var currentUser = await _userService.GetUserByIdAsync(currentUserId.Value);

                // Users can only see their own investments unless they are Admin/Employee
                if (!currentUser.Roles.Any(r => new[] { "Admin", "Employee" }.Contains(r)) && currentUserId != investorId)
                    return Forbid("You can only view your own investments.");

                var result = await _investmentRepository.GetInvestmentsByInvestorAsync(investorId);

                if (!result.Success)
                    return BadRequest(new InvestmentListResponseDto { Success = false, Message = result.Message });

                var investmentDtos = result.Data?.Select(MapToInvestmentDto).ToList() ?? new List<InvestmentDto>();
                return Ok(new InvestmentListResponseDto
                {
                    Success = true,
                    Message = result.Message,
                    Data = investmentDtos,
                    TotalCount = investmentDtos.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new InvestmentListResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving investments.",
                });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<InvestmentListResponseDto>> GetAllInvestments()
        {
            try
            {
                var result = await _investmentRepository.GetAllInvestmentsAsync();

                if (!result.Success)
                    return BadRequest(new InvestmentListResponseDto { Success = false, Message = result.Message });

                var investmentDtos = result.Data?.Select(MapToInvestmentDto).ToList() ?? new List<InvestmentDto>();
                return Ok(new InvestmentListResponseDto
                {
                    Success = true,
                    Message = result.Message,
                    Data = investmentDtos,
                    TotalCount = investmentDtos.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new InvestmentListResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving investments.",
                });
            }
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<InvestmentListResponseDto>> GetInvestmentsByStatus(string status)
        {
            try
            {
                var validStatuses = new[] { "in progress", "invested", "waiting for statement", "completed" };
                if (!validStatuses.Contains(status.ToLower()))
                    return BadRequest(new { Message = "Invalid status. Valid statuses: in progress, invested, waiting for statement, completed" });

                var result = await _investmentRepository.GetInvestmentsByStatusAsync(status);

                if (!result.Success)
                    return BadRequest(new InvestmentListResponseDto { Success = false, Message = result.Message });

                var investmentDtos = result.Data?.Select(MapToInvestmentDto).ToList() ?? new List<InvestmentDto>();
                return Ok(new InvestmentListResponseDto
                {
                    Success = true,
                    Message = result.Message,
                    Data = investmentDtos,
                    TotalCount = investmentDtos.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new InvestmentListResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving investments.",
                });
            }
        }

        [HttpGet("approval/{isApproved}")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<InvestmentListResponseDto>> GetInvestmentsByApprovalStatus(bool isApproved)
        {
            try
            {
                var result = await _investmentRepository.GetInvestmentsByApprovalStatusAsync(isApproved);

                if (!result.Success)
                    return BadRequest(new InvestmentListResponseDto { Success = false, Message = result.Message });

                var investmentDtos = result.Data?.Select(MapToInvestmentDto).ToList() ?? new List<InvestmentDto>();
                return Ok(new InvestmentListResponseDto
                {
                    Success = true,
                    Message = result.Message,
                    Data = investmentDtos,
                    TotalCount = investmentDtos.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new InvestmentListResponseDto
                {
                    Success = false,
                    Message = "An unexpected error occurred while retrieving investments.",
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInvestment(int id)
        {
            try
            {
                var result = await _investmentRepository.DeleteInvestmentAsync(id);

                if (!result.Success)
                    return BadRequest(new { Message = result.Message });

                return Ok(new { Message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An unexpected error occurred while deleting investment." });
            }
        }

        [HttpGet("investors")]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<ActionResult<List<InvestorDto>>> GetInvestors()
        {
            try
            {
                var investors = await _userService.GetInvestorsAsync();
                return Ok(investors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving investors", error = ex.Message });
            }
        }
        
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }

        private InvestmentDto MapToInvestmentDto(Core.Entities.Investment.Investment investment)
        {
            return new InvestmentDto
            {
                Id = investment.Id,
                InvestorId = investment.InvestorId,
                InvestorName = $"{investment.Investor.FirstName} {investment.Investor.LastName}",
                SchemeCode = investment.SchemeCode,
                SchemeName = investment.SchemeName,
                FundName = investment.FundName,
                NavRate = investment.NavRate,
                DateOfPurchase = investment.DateOfPurchase,
                InvestAmount = investment.InvestAmount,
                NumberOfUnits = investment.NumberOfUnits,
                ModeOfInvestment = investment.ModeOfInvestment,
                ImagePath = investment.ImagePath,
                Status = investment.Status,
                IsPublished = investment.IsPublished,
                InvestBy = investment.InvestBy,
                CreatedByUserName = $"{investment.CreatedByUser.FirstName} {investment.CreatedByUser.LastName}",
                IsApproved = investment.IsApproved,
                CreatedAt = investment.CreatedAt,
                UpdatedAt = investment.UpdatedAt,
                Remarks = investment.Remarks
            };
        }

        private async Task<ActionResult> ValidateUserAccess(int? investorId = null)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { Message = "Unable to identify current user." });

            var currentUser = await _userService.GetUserByIdAsync(currentUserId.Value);

            // Check if user is active and has roles
            if (currentUser == null || !currentUser.IsActive || !currentUser.Roles.Any())
                return Forbid("Your account is inactive or has no roles assigned.");

            // For investor-specific endpoints, validate access
            if (investorId.HasValue && currentUserId != investorId)
            {
                var isAdminOrEmployee = currentUser.Roles.Any(r => new[] { "Admin", "Employee" }.Contains(r));
                if (!isAdminOrEmployee)
                    return Forbid("You can only access your own investments.");
            }

            return Ok();
        }
    }
}