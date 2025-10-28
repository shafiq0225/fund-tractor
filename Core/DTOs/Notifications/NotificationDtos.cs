using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Notifications
{
    public class CreateNotificationDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class StoreEmailDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [EmailAddress]
        public string ToEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class StoredEmailDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ViewedAt { get; set; }
        public string? UserName { get; set; }
    }

    public class NotificationDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public class RoleUpdateNotificationDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [EmailAddress]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string OldRole { get; set; } = string.Empty;

        [Required]
        public string NewRole { get; set; } = string.Empty;

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}