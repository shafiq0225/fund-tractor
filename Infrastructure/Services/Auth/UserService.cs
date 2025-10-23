using Core.DTOs.Auth;
using Core.Entities.Auth;
using Core.Interfaces.Auth;
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

        public UserService(StoreContext context, IPasswordService passwordService, ITokenService tokenService, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.IsActive);

            // Use the same message for both cases for security
            if (user == null || !_passwordService.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid Email ID or Password");
            }

            var token = _tokenService.GenerateToken(user);

            return new LoginResponseDto
            {
                Token = token,
                Expires = DateTime.UtcNow.AddHours(24),
                User = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PanNumber = user.PanNumber,
                    CreatedAt = user.CreatedAt,
                    Roles = user.UserRoles.Select(r => r.RoleName).ToList()
                }
            };
        }
        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            if (await UserExistsAsync(registerDto.Email))
            {
                throw new ArgumentException("An account with this email address already exists. Please use a different email or try signing in.");
            }

            if (await _context.Users.AnyAsync(u => u.PanNumber == registerDto.PanNumber.ToUpper()))
            {
                throw new ArgumentException("This PAN number is already registered. Please check the number or contact support if you believe this is an error.");
            }

            // Validate role
            var validRoles = new[] { "Admin", "Employee", "HeadOfFamily", "FamilyMember" };
            if (!validRoles.Contains(registerDto.Role))
            {
                throw new ArgumentException("Invalid role specified");
            }

            // Validate family relationship
            //if (registerDto.Role == "FamilyMember" && !registerDto.FamilyHeadId.HasValue)
            //{
            //    throw new ArgumentException("Family members must be associated with a family head");
            //}

            //if (registerDto.FamilyHeadId.HasValue)
            //{
            //    var familyHead = await _context.Users
            //        .Include(u => u.UserRoles)
            //        .FirstOrDefaultAsync(u => u.Id == registerDto.FamilyHeadId && u.IsActive);

            //    if (familyHead == null || !familyHead.UserRoles.Any(r => r.RoleName == "HeadOfFamily"))
            //    {
            //        throw new ArgumentException("Invalid family head specified");
            //    }
            //}

            var user = new User
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                PanNumber = registerDto.PanNumber.ToUpper(),
                PasswordHash = _passwordService.HashPassword(registerDto.Password),
                EmployeeId = registerDto.EmployeeId,
                Department = registerDto.Department,
                DateOfJoining = registerDto.DateOfJoining,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                UserRoles = new List<UserRole>() // Initialize the collection
            };

            // Add the role from registerDto (based on your business logic)
            user.UserRoles.Add(new UserRole { RoleName = registerDto.Role });

            // Auto-create UserProfile
            user.UserProfile = new UserProfile();

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return await GetUserDtoAsync(user);
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            // Return basic permissions based on roles for now
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null) return new List<string>();

            var permissions = new List<string>();

            foreach (var role in user.UserRoles)
            {
                switch (role.RoleName)
                {
                    case "Admin":
                        permissions.AddRange(new[] { "ViewDashboard", "ManageUsers", "ViewReports", "ManageInvestments" });
                        break;
                    case "Employee":
                        permissions.AddRange(new[] { "ViewDashboard", "ViewReports", "ManageInvestments" });
                        break;
                    case "HeadOfFamily":
                        permissions.AddRange(new[] { "ViewDashboard", "ManageInvestments", "ViewFamilyData", "ManageFamily" });
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
            var permissions = await GetUserPermissionsAsync(userId);
            return permissions.Contains(permissionName);
        }

        private async Task<UserDto> GetUserDtoAsync(User user)
        {
            var permissions = await GetUserPermissionsAsync(user.Id);

            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PanNumber = user.PanNumber,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(r => r.RoleName).ToList(),
                Permissions = permissions,
                EmployeeId = user.EmployeeId,
                Department = user.Department,
                //FamilyHeadId = user.FamilyHeadId,
                //FamilyHeadName = user.FamilyHeadId.HasValue ?
                //    $"{user.FamilyHead!.FirstName} {user.FamilyHead.LastName}" : null
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
                .Where(u => u.IsActive)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var permissions = await GetUserPermissionsAsync(user.Id);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PanNumber = user.PanNumber,
                    CreatedAt = user.CreatedAt,
                    Roles = user.UserRoles.Select(r => r.RoleName).ToList(),
                    Permissions = permissions,
                    EmployeeId = user.EmployeeId,
                    IsActive = user.IsActive
                });
            }

            return userDtos;
        }


        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null) return null;

            var permissions = await GetUserPermissionsAsync(user.Id);
            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PanNumber = user.PanNumber,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(r => r.RoleName).ToList(),
                Permissions = permissions,
                EmployeeId = user.EmployeeId,
                Department = user.Department,
                IsActive = user.IsActive
            };
        }


        public async Task<bool> UpdateUserRoleAsync(int userId, string newRole, int updatedBy)
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
                .FirstOrDefaultAsync(u => u.Id == updatedBy);

            if (currentUser == null || !currentUser.UserRoles.Any(r => r.RoleName == "Admin"))
            {
                throw new UnauthorizedAccessException("Only administrators can update user roles");
            }

            // Prevent admin from removing their own admin role
            if (userId == updatedBy && newRole != "Admin")
            {
                throw new InvalidOperationException("Cannot remove admin role from yourself");
            }

            // Remove existing roles and add new one
            user.UserRoles.Clear();
            user.UserRoles.Add(new UserRole { RoleName = newRole });

            // Update timestamp
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
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
            user.UpdatedAt = DateTime.UtcNow;

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
            user.UpdatedAt = DateTime.UtcNow;

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

            // Update password
            targetUser.PasswordHash = _passwordService.HashPassword(adminChangePasswordDto.NewPassword);
            targetUser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }

}
