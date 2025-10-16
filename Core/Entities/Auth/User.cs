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
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string PanNumber { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Family relationship (for Head and Family Members)
        public int? FamilyHeadId { get; set; } // Reference to Head of Family
        public virtual User? FamilyHead { get; set; }
        public virtual ICollection<User> FamilyMembers { get; set; } = new List<User>();

        // Employee specific fields
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public DateTime? DateOfJoining { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual UserProfile? UserProfile { get; set; }
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
