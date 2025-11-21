using Core.DTOs.Auth;
using Core.DTOs.Investment;
using Core.DTOs.Notifications;
using Core.Entities.Auth;
using Core.Interfaces.Auth;
using Core.Interfaces.Notification;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Infrastructure.Services.Auth
{
    public class UserService : IUserService
    {
        private readonly StoreContext _context;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;
        public UserService(StoreContext context, IPasswordService passwordService, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, INotificationService notificationService)
        {
            _context = context;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto, int createdByUserId)
        {
            // Validate PAN number uniqueness
            if (await _context.Users.AnyAsync(u => u.PanNumber == createUserDto.PanNumber.ToUpper()))
            {
                throw new ArgumentException("This PAN number is already registered.");
            }

            var user = new User
            {
                FirstName = createUserDto.FirstName,
                LastName = createUserDto.LastName,
                PanNumber = createUserDto.PanNumber.ToUpper(),
                Email = string.Empty, // Empty initially - will be set during public registration
                IsActive = createUserDto.IsActive,
                CreatedBy = createdByUserId,
                CreatedDate = DateTime.UtcNow,
                UserRoles = new List<UserRole>(), // Start with empty roles
                UserProfile = new UserProfile() // Auto-create profile
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return await GetUserDtoAsync(user);
        }

        public async Task<(bool Success, string Message, UserDto? Data)> UpdateUserAsync(int userId, UpdateUserDto updateUserDto, int updatedByUserId)
        {
            try
            {
                // Find the user to update
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return (false, "User not found.", null);
                }

                // Check if PAN number is being changed and validate uniqueness
                if (user.PanNumber != updateUserDto.PanNumber.ToUpper())
                {
                    if (await _context.Users.AnyAsync(u => u.PanNumber == updateUserDto.PanNumber.ToUpper() && u.Id != userId))
                    {
                        return (false, "This PAN number is already registered with another user.", null);
                    }
                    user.PanNumber = updateUserDto.PanNumber.ToUpper();
                }

                // Check if EmployeeId is being changed and validate uniqueness
                if (user.EmployeeId != updateUserDto.EmployeeId && !string.IsNullOrEmpty(updateUserDto.EmployeeId))
                {
                    if (await _context.Users.AnyAsync(u => u.EmployeeId == updateUserDto.EmployeeId && u.Id != userId))
                    {
                        return (false, "This Employee ID is already registered with another user.", null);
                    }
                    user.EmployeeId = updateUserDto.EmployeeId;
                }

                // Update basic information
                user.FirstName = updateUserDto.FirstName;
                user.LastName = updateUserDto.LastName;
                user.IsActive = updateUserDto.IsActive;

                // Update audit fields
                user.UpdatedBy = updatedByUserId;
                user.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedUserDto = await GetUserDtoAsync(user);
                return (true, "User updated successfully.", updatedUserDto);
            }
            catch (DbUpdateException dbEx)
            {
                return (false, $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}", null);
            }
        }
        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            // Login only with email
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email
                                       && !string.IsNullOrEmpty(u.PasswordHash));

            if (user == null || !_passwordService.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Your account has been deactivated. Please contact administrator.");
            }

            // Check if user has any roles assigned
            if (!user.UserRoles.Any())
            {
                throw new UnauthorizedAccessException("No roles assigned to your account. Please contact administrator to assign roles.");
            }

            var token = _tokenService.GenerateToken(user);
            var userDto = await GetUserDtoAsync(user);

            return new LoginResponseDto
            {
                Token = token,
                Expires = DateTime.UtcNow.AddHours(24),
                User = userDto
            };
        }
        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            // Check if user exists in UserMaster (by PAN only)
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.PanNumber == registerDto.PanNumber.ToUpper() && u.IsActive);

            if (existingUser == null)
            {
                throw new ArgumentException("User not found in system. Please contact admin to create your account first.");
            }

            // Check if user already has password set (already registered)
            if (!string.IsNullOrEmpty(existingUser.PasswordHash))
            {
                throw new ArgumentException("An account with this PAN already exists. Please try signing in.");
            }

            // Validate email uniqueness
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email && u.Id != existingUser.Id && !string.IsNullOrEmpty(u.Email)))
            {
                throw new ArgumentException("This email is already registered with another account.");
            }

            // Update user details from registration
            existingUser.Email = registerDto.Email;
            existingUser.PasswordHash = _passwordService.HashPassword(registerDto.Password);
            existingUser.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetUserDtoAsync(existingUser);
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            // Return empty permissions if user is inactive OR has no roles
            if (user == null || !user.IsActive || !user.UserRoles.Any())
                return new List<string>();

            var permissions = new List<string>();

            foreach (var role in user.UserRoles)
            {
                switch (role.RoleName)
                {
                    case "Admin":
                        permissions.AddRange(new[] { "ViewDashboard", "ManageUsers", "ViewReports", "ManageInvestments", "ExportData", "SystemConfig" });
                        break;
                    case "Employee":
                        permissions.AddRange(new[] { "ViewDashboard", "ViewReports", "ManageInvestments", "ExportData" });
                        break;
                    case "HeadOfFamily":
                        permissions.AddRange(new[] { "ViewDashboard", "ManageInvestments", "ViewFamilyData", "ManageFamily", "ExportData" });
                        break;
                    case "FamilyMember":
                        permissions.AddRange(new[] { "ViewDashboard", "ManageInvestments" });
                        break;
                }
            }

            return permissions.Distinct().ToList();
        }

        public async Task<bool> HasPermissionAsync(int userId, string permissionName)
        {
            // First check if user is active and has roles
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.IsActive || !user.UserRoles.Any())
                return false;

            var permissions = await GetUserPermissionsAsync(userId);
            return permissions.Contains(permissionName);
        }

        private async Task<UserDto> GetUserDtoAsync(User user)
        {
            var permissions = await GetUserPermissionsAsync(user.Id);

            // Get created by user name
            var createdByUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.CreatedBy);
            var createdByName = createdByUser != null ? $"{createdByUser.FirstName} {createdByUser.LastName}" : "System";

            // Get updated by user name if exists
            string? updatedByName = null;
            if (user.UpdatedBy.HasValue)
            {
                var updatedByUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.UpdatedBy.Value);
                updatedByName = updatedByUser != null ? $"{updatedByUser.FirstName} {updatedByUser.LastName}" : "Unknown";
            }

            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PanNumber = user.PanNumber,
                Roles = user.UserRoles.Select(r => r.RoleName).ToList(),
                Permissions = permissions,
                EmployeeId = user.EmployeeId,
                Department = user.Department,
                DateOfJoining = user.DateOfJoining,
                IsActive = user.IsActive,
                CreatedBy = user.CreatedBy,
                CreatedDate = user.CreatedDate,
                UpdatedBy = user.UpdatedBy,
                UpdatedDate = user.UpdatedDate,
                CreatedByName = createdByName,
                UpdatedByName = updatedByName
            };
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }

        public async Task<string?> GetCurrentUserNameAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userEmail = httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return null;
            }

            var user = await GetUserByEmailAsync(userEmail);
            return user != null ? $"{user.FirstName} {user.LastName}" : null;
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var userDto = await GetUserDtoAsync(user);
                userDtos.Add(userDto);
            }

            return userDtos;
        }


        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            return await GetUserDtoAsync(user);
        }

        public async Task<bool> UpdateUserRoleAsync(int userId, string newRole, int updatedByUserId)
        {
            // Validate role
            var validRoles = new[] { "Admin", "Employee", "HeadOfFamily", "FamilyMember" };
            if (!validRoles.Contains(newRole))
            {
                throw new ArgumentException("Invalid role specified");
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Check if the user performing the update is an admin
            var currentUser = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == updatedByUserId);

            if (currentUser == null || !currentUser.UserRoles.Any(r => r.RoleName == "Admin"))
            {
                throw new UnauthorizedAccessException("Only administrators can update user roles");
            }

            // Prevent admin from removing their own admin role
            if (userId == updatedByUserId && newRole != "Admin")
            {
                throw new InvalidOperationException("Cannot remove admin role from yourself");
            }

            var oldRole = user.UserRoles.FirstOrDefault()?.RoleName ?? "No Role";

            // Remove existing roles and add new one
            user.UserRoles.Clear();
            user.UserRoles.Add(new UserRole { RoleName = newRole });

            // Update timestamp
            user.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var updatedByUser = await _context.Users
               .FirstOrDefaultAsync(u => u.Id == updatedByUserId);

            var updatedBy = updatedByUser != null
                ? $"{updatedByUser.FirstName} {updatedByUser.LastName}"
                : "System Administrator";

            await _notificationService.SendRoleUpdateNotificationAsync(new RoleUpdateNotificationDto
            {
                UserId = userId,
                UserEmail = user.Email,
                UserName = $"{user.FirstName} {user.LastName}",
                OldRole = oldRole,
                NewRole = newRole,
                UpdatedBy = updatedBy
            });

            return true;
        }


        public async Task<bool> DeleteUserAsync(int userId, int deletedBy)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Check if the user performing the deletion is an admin
            var currentUser = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == deletedBy);

            if (currentUser == null || !currentUser.UserRoles.Any(r => r.RoleName == "Admin"))
            {
                throw new UnauthorizedAccessException("Only administrators can delete users");
            }

            // Prevent admin from deleting themselves
            if (userId == deletedBy)
            {
                throw new InvalidOperationException("Cannot delete your own account");
            }

            // Soft delete
            user.IsActive = false;
            user.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            // Validate input
            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmPassword)
            {
                throw new ArgumentException("New password and confirmation password do not match");
            }

            // Password strength validation
            if (string.IsNullOrWhiteSpace(changePasswordDto.NewPassword) || changePasswordDto.NewPassword.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters long");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Verify current password
            if (!_passwordService.VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Current password is incorrect");
            }

            // Check if new password is different from current password
            if (_passwordService.VerifyPassword(changePasswordDto.NewPassword, user.PasswordHash))
            {
                throw new ArgumentException("New password must be different from current password");
            }

            // Update password
            user.PasswordHash = _passwordService.HashPassword(changePasswordDto.NewPassword);
            user.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AdminChangePasswordAsync(int adminUserId, AdminChangePasswordDto adminChangePasswordDto)
        {
            // Validate input
            if (adminChangePasswordDto.NewPassword != adminChangePasswordDto.ConfirmPassword)
            {
                throw new ArgumentException("New password and confirmation password do not match");
            }

            // Password strength validation
            if (string.IsNullOrWhiteSpace(adminChangePasswordDto.NewPassword) || adminChangePasswordDto.NewPassword.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters long");
            }

            // Check if admin user exists and has admin role
            var adminUser = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == adminUserId && u.IsActive);

            if (adminUser == null || !adminUser.UserRoles.Any(r => r.RoleName == "Admin"))
            {
                throw new UnauthorizedAccessException("Only administrators can change other users' passwords");
            }

            // Prevent admin from changing their own password using this method
            if (adminUserId == adminChangePasswordDto.UserId)
            {
                throw new InvalidOperationException("Please use the regular change password endpoint to change your own password");
            }

            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == adminChangePasswordDto.UserId && u.IsActive);

            if (targetUser == null)
            {
                throw new ArgumentException("User not found");
            }
            if (_passwordService.VerifyPassword(adminChangePasswordDto.NewPassword, targetUser.PasswordHash))
            {
                throw new ArgumentException("New password must be different from current password");
            }
            // Update password
            targetUser.PasswordHash = _passwordService.HashPassword(adminChangePasswordDto.NewPassword);
            targetUser.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            // Send notification after successful password reset
            var adminUserName = $"{adminUser.FirstName} {adminUser.LastName}";

            await _notificationService.SendPasswordResetNotificationAsync(new PasswordResetNotificationDto
            {
                UserId = targetUser.Id,
                UserEmail = targetUser.Email,
                Firstname = targetUser.FirstName,
                Lastname = targetUser.LastName,
                ResetBy = adminUserName,
                ResetAt = DateTime.UtcNow
            });

            return true;
        }

        public async Task<List<InvestorDto>> GetInvestorsAsync()
        {
            var validRoles = new[] { "HeadOfFamily", "FamilyMember" };

            var investors = await _context.Users
                .Include(u => u.UserRoles)
                .Where(u => u.IsActive && u.UserRoles.Any(ur => validRoles.Contains(ur.RoleName)))
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new InvestorDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email
                })
                .ToListAsync();

            return investors;
        }
    }

}
