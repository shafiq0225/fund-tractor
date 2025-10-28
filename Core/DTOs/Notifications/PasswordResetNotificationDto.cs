using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTOs.Notifications
{
    // Add this to your Core/DTOs/Notifications folder
    public class PasswordResetNotificationDto
    {
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
        public string ResetBy { get; set; } = string.Empty;
        public DateTime ResetAt { get; set; }
    }
}
