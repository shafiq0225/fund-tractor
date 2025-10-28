using Core.DTOs.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces.Notification
{
    public interface INotificationService
    {
        Task<bool> CreateNotificationAsync(CreateNotificationDto createNotificationDto);
        Task<bool> StoreEmailAsync(StoreEmailDto storeEmailDto);
        Task<List<StoredEmailDto>> GetUserEmailsAsync(int userId);
        Task<List<StoredEmailDto>> GetAllEmailsAsync();
        Task<bool> MarkEmailAsViewedAsync(int emailId);
        Task<List<NotificationDto>> GetUserNotificationsAsync(int userId);
        Task<bool> MarkNotificationAsReadAsync(int notificationId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<bool> SendRoleUpdateNotificationAsync(RoleUpdateNotificationDto roleUpdateDto);
        Task<StoredEmailDto?> GetEmailByIdAsync(int emailId);
    }
}
