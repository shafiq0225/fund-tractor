using Core.DTOs.Auth;
using Core.DTOs.Investment;
using Core.Entities.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.Auth
{
    public interface IUserService
    {
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto, int createdByUserId);
        Task<(bool Success, string Message, UserDto? Data)> UpdateUserAsync(int userId, UpdateUserDto updateUserDto, int updatedByUserId);
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<bool> UserExistsAsync(string email);
        Task<User?> GetUserByEmailAsync(string email);
        Task<string?> GetCurrentUserNameAsync();

        Task<List<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<bool> UpdateUserRoleAsync(int userId, string newRole, int updatedBy);
        Task<bool> DeleteUserAsync(int userId, int deletedBy);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
        Task<bool> AdminChangePasswordAsync(int adminUserId, AdminChangePasswordDto adminChangePasswordDto);
        Task<List<InvestorDto>> GetInvestorsAsync();

    }

    public interface ITokenService
    {
        string GenerateToken(User user);
    }

    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }

}
