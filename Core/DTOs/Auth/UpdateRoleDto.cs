using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Auth
{
    public class UpdateRoleDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string NewRole { get; set; } = string.Empty;
    }

    public class UserListDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string? EmployeeId { get; set; }
    }
}