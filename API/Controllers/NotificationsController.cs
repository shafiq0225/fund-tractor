using Core.DTOs.Auth;
using Core.DTOs.Notifications;
using Core.Interfaces;
using Core.Interfaces.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> CreateNotification([FromBody] CreateNotificationDto createNotificationDto)
        {
            try
            {
                var success = await _notificationService.CreateNotificationAsync(createNotificationDto);

                if (success)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Notification created successfully"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to create notification"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while creating notification"
                });
            }
        }

        [HttpPost("store-email")]
        public async Task<ActionResult<ApiResponse<object>>> StoreEmail([FromBody] StoreEmailDto storeEmailDto)
        {
            try
            {
                var success = await _notificationService.StoreEmailAsync(storeEmailDto);

                if (success)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Email stored successfully"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to store email"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while storing email"
                });
            }
        }

        [HttpGet("emails/{userId}")]
        public async Task<ActionResult<ApiResponse<List<StoredEmailDto>>>> GetUserEmails(int userId)
        {
            try
            {
                var emails = await _notificationService.GetUserEmailsAsync(userId);
                return Ok(new ApiResponse<List<StoredEmailDto>>
                {
                    Success = true,
                    Data = emails
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<StoredEmailDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving emails"
                });
            }
        }

        [HttpGet("emails")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<StoredEmailDto>>>> GetAllEmails()
        {
            try
            {
                var emails = await _notificationService.GetAllEmailsAsync();
                return Ok(new ApiResponse<List<StoredEmailDto>>
                {
                    Success = true,
                    Data = emails
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<StoredEmailDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving emails"
                });
            }
        }

        [HttpPut("emails/{emailId}/view")]
        public async Task<ActionResult<ApiResponse<object>>> MarkEmailAsViewed(int emailId)
        {
            try
            {
                var success = await _notificationService.MarkEmailAsViewedAsync(emailId);

                if (success)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Email marked as viewed"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to mark email as viewed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating email status"
                });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> GetUserNotifications(int userId)
        {
            try
            {
                var notifications = await _notificationService.GetUserNotificationsAsync(userId);
                return Ok(new ApiResponse<List<NotificationDto>>
                {
                    Success = true,
                    Data = notifications
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<NotificationDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving notifications"
                });
            }
        }

        [HttpPut("{notificationId}/read")]
        public async Task<ActionResult<ApiResponse<object>>> MarkNotificationAsRead(int notificationId)
        {
            try
            {
                var success = await _notificationService.MarkNotificationAsReadAsync(notificationId);

                if (success)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Notification marked as read"
                    });
                }

                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Failed to mark notification as read"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while updating notification"
                });
            }
        }

        [HttpGet("unread-count/{userId}")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(int userId)
        {
            try
            {
                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Ok(new ApiResponse<int>
                {
                    Success = true,
                    Data = count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<int>
                {
                    Success = false,
                    Message = "An error occurred while retrieving unread count"
                });
            }
        }

        [HttpGet("emails/{emailId}/detail")]
        public async Task<ActionResult<ApiResponse<StoredEmailDto>>> GetEmailById(int emailId)
        {
            try
            {
                var email = await _notificationService.GetEmailByIdAsync(emailId);

                if (email == null)
                {
                    return NotFound(new ApiResponse<StoredEmailDto>
                    {
                        Success = false,
                        Message = "Email not found"
                    });
                }

                return Ok(new ApiResponse<StoredEmailDto>
                {
                    Success = true,
                    Data = email
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<StoredEmailDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving email"
                });
            }
        }

    }
}