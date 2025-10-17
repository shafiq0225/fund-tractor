using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Auth
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.RegularExpressions;

    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public UserDto User { get; set; } = null!;
    }

    public class RegisterDto
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$", ErrorMessage = "Invalid PAN format")]
        public string PanNumber { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Role assignment (default to FamilyMember)
        public string Role { get; set; } = "FamilyMember";
        // Additional fields for employees
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public DateTime? DateOfJoining { get; set; }

        // For family members
        public int? FamilyHeadId { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public int? FamilyHeadId { get; set; }
        public string? FamilyHeadName { get; set; }
    }

    public class CreateUserDto : RegisterDto
    {
        // Additional fields for admin creating users
        public bool IsActive { get; set; } = true;
    }

    public class AssignFamilyMemberDto
    {
        [Required]
        public int FamilyHeadId { get; set; }

        [Required]
        public int FamilyMemberId { get; set; }
    }


    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }


}
