using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Auth
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$")]
        public string PanNumber { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Optional fields (can be null)
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public DateTime? DateOfJoining { get; set; }

        public bool IsActive { get; set; } = true;

        // Audit fields
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // Navigation properties
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual UserProfile? UserProfile { get; set; }
        public virtual User CreatedByUser { get; set; }
    }


    public class UserRole
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty; // Admin, Employee, HeadOfFamily, FamilyMember

        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
    }

    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        // Additional profile fields
        public string? ProfilePicture { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
    }

    // Permission system for fine-grained access control
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // e.g., "ViewReports", "ManageUsers"

        [StringLength(255)]
        public string? Description { get; set; }

        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;

        public int PermissionId { get; set; }
        public virtual Permission Permission { get; set; } = null!;
    }

}
