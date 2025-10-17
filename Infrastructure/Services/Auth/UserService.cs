using Core.DTOs.Auth;
using Core.Entities.Auth;
using Core.Interfaces.Auth;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Auth
{
    public class UserService : IUserService
    {
        private readonly StoreContext _context;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;

        public UserService(StoreContext context, IPasswordService passwordService, ITokenService tokenService)
        {
            _context = context;
            _passwordService = passwordService;
            _tokenService = tokenService;
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
    }

}
