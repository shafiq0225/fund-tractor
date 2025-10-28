using Core.DTOs.Notifications;
using Core.Entities.Email;
using Core.Interfaces.Notification;
using DocumentFormat.OpenXml.Spreadsheet;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly StoreContext _context;

        public NotificationService(StoreContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateNotificationAsync(CreateNotificationDto createNotificationDto)
        {
            try
            {
                var notification = new Core.Entities.Notification.Notification
                {
                    UserId = createNotificationDto.UserId,
                    Title = createNotificationDto.Title,
                    Message = createNotificationDto.Message,
                    Type = createNotificationDto.Type,
                    Metadata = SerializeMetadata(createNotificationDto.Metadata),
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> StoreEmailAsync(StoreEmailDto storeEmailDto)
        {
            try
            {
                var storedEmail = new StoredEmail
                {
                    UserId = storeEmailDto.UserId,
                    ToEmail = storeEmailDto.ToEmail,
                    Subject = storeEmailDto.Subject,
                    Body = storeEmailDto.Body,
                    Type = storeEmailDto.Type,
                    Status = "pending",
                    Metadata = SerializeMetadata(storeEmailDto.Metadata),
                    CreatedAt = DateTime.UtcNow
                };

                _context.StoredEmails.Add(storedEmail);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<List<StoredEmailDto>> GetUserEmailsAsync(int userId)
        {
            var emails = await _context.StoredEmails
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    ToEmail = e.ToEmail,
                    Subject = e.Subject,
                    Body = e.Body,
                    Type = e.Type,
                    Status = e.Status,
                    Metadata = e.Metadata, // Keep as string
                    CreatedAt = e.CreatedAt,
                    ViewedAt = e.ViewedAt
                })
                .ToListAsync();

            // Deserialize after materializing the query
            return emails.Select(e => new StoredEmailDto
            {
                Id = e.Id,
                UserId = e.UserId,
                ToEmail = e.ToEmail,
                Subject = e.Subject,
                Body = e.Body,
                Type = e.Type,
                Status = e.Status,
                Metadata = DeserializeMetadata(e.Metadata),
                CreatedAt = e.CreatedAt,
                ViewedAt = e.ViewedAt
            }).ToList();
        }

        public async Task<List<StoredEmailDto>> GetAllEmailsAsync()
        {
            var emails = await _context.StoredEmails
                .Include(e => e.User)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    Id = e.Id,
                    UserId = e.UserId,
                    ToEmail = e.ToEmail,
                    Subject = e.Subject,
                    Body = e.Body,
                    Type = e.Type,
                    Status = e.Status,
                    Metadata = e.Metadata, // Keep as string
                    CreatedAt = e.CreatedAt,
                    ViewedAt = e.ViewedAt,
                    FirstName = e.User.FirstName,
                    LastName = e.User.LastName
                })
                .ToListAsync();

            // Deserialize after materializing the query
            return emails.Select(e => new StoredEmailDto
            {
                Id = e.Id,
                UserId = e.UserId,
                ToEmail = e.ToEmail,
                Subject = e.Subject,
                Body = e.Body,
                Type = e.Type,
                Status = e.Status,
                Metadata = DeserializeMetadata(e.Metadata),
                CreatedAt = e.CreatedAt,
                ViewedAt = e.ViewedAt,
                UserName = $"{e.FirstName} {e.LastName}"
            }).ToList();
        }

        public async Task<bool> MarkEmailAsViewedAsync(int emailId)
        {
            try
            {
                var email = await _context.StoredEmails.FindAsync(emailId);
                if (email == null) return false;

                email.Status = "viewed";
                email.ViewedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    Metadata = n.Metadata, // Keep as string
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                })
                .ToListAsync();

            // Deserialize after materializing the query
            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                Metadata =DeserializeMetadata(n.Metadata),
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt
            }).ToList();
        }

        public async Task<bool> MarkNotificationAsReadAsync(int notificationId)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(notificationId);
                if (notification == null) return false;

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> SendRoleUpdateNotificationAsync(RoleUpdateNotificationDto roleUpdateDto)
        {
            try
            {
                // Create internal notification
                var notificationDto = new CreateNotificationDto
                {
                    UserId = roleUpdateDto.UserId,
                    Title = "Role Updated",
                    Message = $"Your role has been changed from {roleUpdateDto.OldRole} to {roleUpdateDto.NewRole} by {roleUpdateDto.UpdatedBy}",
                    Type = "role_update",
                    Metadata = new Dictionary<string, object>
            {
                { "oldRole", roleUpdateDto.OldRole },
                { "newRole", roleUpdateDto.NewRole },
                { "updatedBy", roleUpdateDto.UpdatedBy },
                { "updatedAt", DateTime.UtcNow }
            }
                };

                await CreateNotificationAsync(notificationDto);

                // Store email content
                var emailBody = GenerateRoleUpdateEmailBody(
                    roleUpdateDto.UserName,
                    roleUpdateDto.OldRole,
                    roleUpdateDto.NewRole,
                    roleUpdateDto.UpdatedBy
                );

                var emailDto = new StoreEmailDto
                {
                    UserId = roleUpdateDto.UserId,
                    ToEmail = roleUpdateDto.UserEmail,
                    Subject = "Your Role Has Been Updated",
                    Body = emailBody,
                    Type = "role_update",
                    Metadata = new Dictionary<string, object>
            {
                { "userName", roleUpdateDto.UserName },
                { "oldRole", roleUpdateDto.OldRole },
                { "newRole", roleUpdateDto.NewRole },
                { "updatedBy", roleUpdateDto.UpdatedBy },
                { "date", DateTime.UtcNow.ToString("MMMM dd, yyyy") }
            }
                };

                await StoreEmailAsync(emailDto);

                return true;
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - we don't want role update to fail because of notification
                Console.WriteLine($"Failed to send role update notification: {ex.Message}");
                return false;
            }
        }

        private string GenerateRoleUpdateEmailBody(string userName, string oldRole, string newRole, string updatedBy)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #f8f9fa;'>
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: white;'>
                        <h1 style='margin: 0; font-size: 24px;'>Role Update Notification</h1>
                    </div>
                    
                    <div style='padding: 30px;'>
                        <h2 style='color: #333; margin-bottom: 20px;'>Hello {userName},</h2>
                        
                        <div style='background: white; padding: 25px; border-radius: 8px; border-left: 4px solid #667eea; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <p style='color: #666; line-height: 1.6; margin-bottom: 20px;'>
                                Your account role has been successfully updated. Here are the details of this change:
                            </p>
                            
                            <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                                <tr>
                                    <td style='padding: 12px; border-bottom: 1px solid #eee; font-weight: bold; color: #333; width: 40%;'>Previous Role:</td>
                                    <td style='padding: 12px; border-bottom: 1px solid #eee; color: #666;'>{oldRole}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 12px; border-bottom: 1px solid #eee; font-weight: bold; color: #333;'>New Role:</td>
                                    <td style='padding: 12px; border-bottom: 1px solid #eee; color: #22c55e; font-weight: bold;'>{newRole}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 12px; border-bottom: 1px solid #eee; font-weight: bold; color: #333;'>Updated By:</td>
                                    <td style='padding: 12px; border-bottom: 1px solid #eee; color: #666;'>{updatedBy}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 12px; border-bottom: 1px solid #eee; font-weight: bold; color: #333;'>Date & Time:</td>
                                    <td style='padding: 12px; border-bottom: 1px solid #eee; color: #666;'>{DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' hh:mm tt")}</td>
                                </tr>
                            </table>
                            
                            <div style='background: #f0f9ff; padding: 15px; border-radius: 6px; border: 1px solid #bae6fd; margin: 20px 0;'>
                                <p style='color: #0369a1; margin: 0; font-size: 14px;'>
                                    <strong>Note:</strong> This role change may affect your access permissions within the system.
                                    If you have any questions, please contact your administrator.
                                </p>
                            </div>
                        </div>
                        
                        <div style='margin-top: 30px; padding: 20px; background: white; border-radius: 8px; text-align: center; border-top: 1px solid #e5e7eb;'>
                            <p style='color: #999; font-size: 12px; margin: 0;'>
                                This is an automated message from the system. Please do not reply to this notification.
                            </p>
                        </div>
                    </div>
                </div>
            ";
        }

        public async Task<StoredEmailDto?> GetEmailByIdAsync(int emailId)
        {
            var email = await _context.StoredEmails
                .FirstOrDefaultAsync(e => e.Id == emailId);

            if (email == null)
                return null;

            return new StoredEmailDto
            {
                Id = email.Id,
                UserId = email.UserId,
                ToEmail = email.ToEmail,
                Subject = email.Subject,
                Body = email.Body,
                Type = email.Type,
                Status = email.Status,
                Metadata = DeserializeMetadata(email.Metadata),
                CreatedAt = email.CreatedAt,
                ViewedAt = email.ViewedAt,
                UserName = email.ToEmail
            };
        }

        private Dictionary<string, object>? DeserializeMetadata(string? metadataJson)
        {
            if (string.IsNullOrEmpty(metadataJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson);
            }
            catch (JsonException)
            {
                // Return empty dictionary or null on error
                return null;
            }
        }

        // Helper method to serialize metadata
        private string? SerializeMetadata(Dictionary<string, object>? metadata)
        {
            if (metadata == null)
                return null;

            try
            {
                return JsonSerializer.Serialize(metadata);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public async Task<bool> SendPasswordResetNotificationAsync(PasswordResetNotificationDto passwordResetDto)
        {
            try
            {
                // Create internal notification
                var notificationDto = new CreateNotificationDto
                {
                    UserId = passwordResetDto.UserId,
                    Title = "Password Reset",
                    Message = $"Your password has been reset by {passwordResetDto.ResetBy} on {passwordResetDto.ResetAt:MMMM dd, yyyy 'at' hh:mm tt}",
                    Type = "password_reset",
                    Metadata = new Dictionary<string, object>
            {
                { "resetBy", passwordResetDto.ResetBy },
                { "resetAt", passwordResetDto.ResetAt },
                { "action", "admin_password_reset" }
            }
                };

                await CreateNotificationAsync(notificationDto);

                // Store email content
                var emailBody = GeneratePasswordResetEmailBody(
                    passwordResetDto.Firstname,
                    passwordResetDto.Lastname,
                    passwordResetDto.ResetBy
                );

                var emailDto = new StoreEmailDto
                {
                    UserId = passwordResetDto.UserId,
                    ToEmail = passwordResetDto.UserEmail,
                    Subject = "Your Password Has Been Reset",
                    Body = emailBody,
                    Type = "password_reset",
                    Metadata = new Dictionary<string, object>
            {
                { "Firstname", passwordResetDto.Firstname },
                { "Lastname", passwordResetDto.Lastname },
                { "resetBy", passwordResetDto.ResetBy },
                { "date", DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                { "time", DateTime.UtcNow.ToString("hh:mm tt") }
            }
                };

                await StoreEmailAsync(emailDto);

                return true;
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - we don't want password reset to fail because of notification
                Console.WriteLine($"Failed to send password reset notification: {ex.Message}");
                return false;
            }
        }

        private string GeneratePasswordResetEmailBody(string firstname,string lastname, string resetBy)
        {
            return $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; background: #f8f9fa;'>
            <div style='background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); padding: 30px; text-align: center; color: white;'>
                <h1 style='margin: 0; font-size: 24px;'>Password Reset Notification</h1>
            </div>
            
            <div style='padding: 30px;'>
                <h2 style='color: #333; margin-bottom: 20px;'>Hello {firstname} {lastname},</h2>
                
                <div style='background: white; padding: 25px; border-radius: 8px; border-left: 4px solid #ef4444; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <p style='color: #666; line-height: 1.6; margin-bottom: 20px;'>
                        Your account password has been reset by an administrator. Here are the details of this action:
                    </p>
                    
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                        <tr>
                            <td style='padding: 12px; border-bottom: 1px solid #eee; font-weight: bold; color: #333; width: 40%;'>Action:</td>
                            <td style='padding: 12px; border-bottom: 1px solid #eee; color: #666;'>Password Reset</td>
                        </tr>
                        <tr>
                            <td style='padding: 12px; border-bottom: 1px solid #eee; font-weight: bold; color: #333;'>Reset By:</td>
                            <td style='padding: 12px; border-bottom: 1px solid #eee; color: #666;'>{resetBy}</td>
                        </tr>
                        <tr>
                            <td style='padding: 12px; border-bottom: 1px solid #eee; font-weight: bold; color: #333;'>Date & Time:</td>
                            <td style='padding: 12px; border-bottom: 1px solid #eee; color: #666;'>{DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' hh:mm tt")} UTC</td>
                        </tr>
                    </table>
                    
                    <div style='background: #fef2f2; padding: 15px; border-radius: 6px; border: 1px solid #fecaca; margin: 20px 0;'>
                        <p style='color: #dc2626; margin: 0; font-size: 14px;'>
                            <strong>Important Security Notice:</strong> 
                            If you did not request this password reset or believe this action was taken in error, 
                            please contact your system administrator immediately and consider securing your account.
                        </p>
                    </div>
                    
                    <div style='background: #f0f9ff; padding: 15px; border-radius: 6px; border: 1px solid #bae6fd; margin: 20px 0;'>
                        <p style='color: #0369a1; margin: 0; font-size: 14px;'>
                            <strong>Next Steps:</strong> 
                            You can now login to the system using your new password. 
                            We recommend changing your password after your first login for security purposes.
                        </p>
                    </div>
                </div>
                
                <div style='margin-top: 30px; padding: 20px; background: white; border-radius: 8px; text-align: center; border-top: 1px solid #e5e7eb;'>
                    <p style='color: #999; font-size: 12px; margin: 0;'>
                        This is an automated security message from the system. Please do not reply to this email.
                    </p>
                </div>
            </div>
        </div>
    ";
        }
    }

}
