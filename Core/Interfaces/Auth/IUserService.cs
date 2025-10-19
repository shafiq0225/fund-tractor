using Core.DTOs.Auth;
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
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<bool> UserExistsAsync(string email);
        Task<User?> GetUserByEmailAsync(string email);
        Task<string?> GetCurrentUserNameAsync();
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
