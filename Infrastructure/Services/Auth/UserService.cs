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

            if (user == null || !_passwordService.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
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
                throw new ArgumentException("User with this email already exists");
            }

            if (await _context.Users.AnyAsync(u => u.PanNumber == registerDto.PanNumber.ToUpper()))
            {
                throw new ArgumentException("User with this PAN number already exists");
            }

            var user = new User
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                PanNumber = registerDto.PanNumber.ToUpper(),
                PasswordHash = _passwordService.HashPassword(registerDto.Password),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Add default role
            user.UserRoles.Add(new UserRole { RoleName = "User" });

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PanNumber = user.PanNumber,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles.Select(r => r.RoleName).ToList()
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
