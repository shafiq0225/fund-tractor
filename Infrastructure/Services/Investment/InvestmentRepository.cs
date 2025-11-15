using Core.DTOs.Investment;
using Core.Interfaces.InvestmentRepo;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
// Alternative using directive
using Microsoft.Extensions.Hosting;
namespace Infrastructure.Services.Investment
{
    public class InvestmentRepository : IInvestmentRepository
    {
        private readonly StoreContext _context;
        private readonly IHostingEnvironment _environment;
        private readonly ILogger<InvestmentRepository> _logger;

        public InvestmentRepository(
            StoreContext context,
            IHostingEnvironment environment,
            ILogger<InvestmentRepository> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, Core.Entities.Investment.Investment? Data)> CreateInvestmentAsync(
            CreateInvestmentDto createDto, int createdByUserId, string investBy)
        {
            try
            {
                // Validate investor exists and is either HeadOfFamily or FamilyMember
                var investor = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Id == createDto.InvestorId && u.IsActive);

                if (investor == null)
                    return (false, "Investor not found.", null);

                var validInvestorRoles = new[] { "HeadOfFamily", "FamilyMember" };
                if (!investor.UserRoles.Any(r => validInvestorRoles.Contains(r.RoleName)))
                    return (false, "Investor must be either HeadOfFamily or FamilyMember.", null);

                // Validate scheme exists
                var scheme = await _context.SchemeDetails
                    .FirstOrDefaultAsync(s => s.SchemeCode == createDto.SchemeCode && s.IsVisible);

                if (scheme == null)
                    return (false, "Scheme not found or not visible.", null);

                // Validate createdBy user exists and is Admin/Employee
                var createdByUser = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Id == createdByUserId && u.IsActive);

                if (createdByUser == null)
                    return (false, "Creating user not found.", null);

                var validCreatorRoles = new[] { "Admin", "Employee" };
                if (!createdByUser.UserRoles.Any(r => validCreatorRoles.Contains(r.RoleName)))
                    return (false, "Only Admin or Employee can create investments.", null);

                string imagePath = null;
                if (createDto.ImageFile != null && createDto.ImageFile.Length > 0)
                {
                    imagePath = await SaveInvestmentImageAsync(createDto.ImageFile, createDto.ModeOfInvestment);
                }

                var investment = new Core.Entities.Investment.Investment
                {
                    InvestorId = createDto.InvestorId,
                    SchemeCode = createDto.SchemeCode,
                    SchemeName = createDto.SchemeName,
                    FundName = createDto.FundName,
                    NavRate = createDto.NavRate,
                    DateOfPurchase = createDto.DateOfPurchase,
                    InvestAmount = createDto.InvestAmount,
                    NumberOfUnits = createDto.NumberOfUnits,
                    ModeOfInvestment = createDto.ModeOfInvestment,
                    ImagePath = imagePath,
                    Status = "in progress",
                    IsPublished = false,
                    CreatedBy = createdByUserId,
                    InvestBy = investBy,
                    IsApproved = false,
                    CreatedAt = DateTime.UtcNow,
                    Remarks = createDto.Remarks
                };

                await _context.Investments.AddAsync(investment);
                await _context.SaveChangesAsync();

                return (true, "Investment created successfully.", investment);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while creating investment");
                return (false, $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating investment");
                return (false, $"Unexpected error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, Core.Entities.Investment.Investment? Data)> UpdateInvestmentAsync(
            int investmentId, UpdateInvestmentDto updateDto)
        {
            try
            {
                var investment = await _context.Investments
                    .FirstOrDefaultAsync(i => i.Id == investmentId);

                if (investment == null)
                    return (false, "Investment not found.", null);

                // Update fields if provided
                if (!string.IsNullOrEmpty(updateDto.Status))
                    investment.Status = updateDto.Status;

                if (updateDto.IsPublished.HasValue)
                    investment.IsPublished = updateDto.IsPublished.Value;

                if (updateDto.IsApproved.HasValue)
                    investment.IsApproved = updateDto.IsApproved.Value;

                if (!string.IsNullOrEmpty(updateDto.Remarks))
                    investment.Remarks = updateDto.Remarks;

                if (updateDto.ImageFile != null && updateDto.ImageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(investment.ImagePath))
                    {
                        DeleteInvestmentImage(investment.ImagePath);
                    }
                    investment.ImagePath = await SaveInvestmentImageAsync(updateDto.ImageFile, investment.ModeOfInvestment);
                }

                investment.UpdatedAt = DateTime.UtcNow;
                _context.Investments.Update(investment);
                await _context.SaveChangesAsync();

                return (true, "Investment updated successfully.", investment);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while updating investment");
                return (false, $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating investment");
                return (false, $"Unexpected error: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, Core.Entities.Investment.Investment? Data)> GetInvestmentByIdAsync(int investmentId)
        {
            try
            {
                var investment = await _context.Investments
                    .Include(i => i.Investor)
                    .Include(i => i.CreatedByUser)
                    .FirstOrDefaultAsync(i => i.Id == investmentId);

                if (investment == null)
                    return (false, "Investment not found.", null);

                return (true, "Investment retrieved successfully.", investment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investment by ID");
                return (false, $"Error retrieving investment: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, List<Core.Entities.Investment.Investment>? Data)> GetInvestmentsByInvestorAsync(int investorId)
        {
            try
            {
                var investments = await _context.Investments
                    .Include(i => i.Investor)
                    .Include(i => i.CreatedByUser)
                    .Where(i => i.InvestorId == investorId)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return (true, "Investments retrieved successfully.", investments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investments by investor");
                return (false, $"Error retrieving investments: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, List<Core.Entities.Investment.Investment>? Data)> GetAllInvestmentsAsync()
        {
            try
            {
                var investments = await _context.Investments
                    .Include(i => i.Investor)
                    .Include(i => i.CreatedByUser)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return (true, "All investments retrieved successfully.", investments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all investments");
                return (false, $"Error retrieving investments: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, List<Core.Entities.Investment.Investment>? Data)> GetInvestmentsByStatusAsync(string status)
        {
            try
            {
                var investments = await _context.Investments
                    .Include(i => i.Investor)
                    .Include(i => i.CreatedByUser)
                    .Where(i => i.Status == status)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return (true, $"Investments with status '{status}' retrieved successfully.", investments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investments by status");
                return (false, $"Error retrieving investments: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message, List<Core.Entities.Investment.Investment>? Data)> GetInvestmentsByApprovalStatusAsync(bool isApproved)
        {
            try
            {
                var investments = await _context.Investments
                    .Include(i => i.Investor)
                    .Include(i => i.CreatedByUser)
                    .Where(i => i.IsApproved == isApproved)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return (true, $"Investments with approval status '{isApproved}' retrieved successfully.", investments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investments by approval status");
                return (false, $"Error retrieving investments: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> DeleteInvestmentAsync(int investmentId)
        {
            try
            {
                var investment = await _context.Investments.FindAsync(investmentId);
                if (investment == null)
                    return (false, "Investment not found.");

                // Delete associated image
                if (!string.IsNullOrEmpty(investment.ImagePath))
                {
                    DeleteInvestmentImage(investment.ImagePath);
                }

                _context.Investments.Remove(investment);
                await _context.SaveChangesAsync();

                return (true, "Investment deleted successfully.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while deleting investment");
                return (false, $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting investment");
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        public async Task<bool> InvestorExistsAsync(int investorId)
        {
            return await _context.Users
                .AnyAsync(u => u.Id == investorId && u.IsActive);
        }

        private async Task<string> SaveInvestmentImageAsync(IFormFile imageFile, string modeOfInvestment)
        {
            try
            {
                var uploadsFolder = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "investments");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileExtension = Path.GetExtension(imageFile.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                return $"/uploads/investments/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving investment image");
                throw new Exception("Failed to save investment image", ex);
            }
        }


        private void DeleteInvestmentImage(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath)) return;

                var fullPath = Path.Combine(_environment.ContentRootPath, "wwwroot", imagePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting investment image");
                // Don't throw exception for image deletion failure
            }
        }
    }

}
