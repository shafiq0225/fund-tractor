using Core.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API
{
    public class ValidateUserStatusFilter : IAsyncActionFilter
    {
        private readonly IUserService _userService;

        public ValidateUserStatusFilter(IUserService userService)
        {
            _userService = userService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                var user = await _userService.GetUserByIdAsync(userId);

                if (user == null || !user.IsActive || !user.Roles.Any())
                {
                    context.Result = new ObjectResult(new
                    {
                        Success = false,
                        Message = !user.IsActive ?
                            "Your account has been deactivated. Please contact administrator." :
                            "No roles assigned to your account. Please contact administrator."
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }
            }

            await next();
        }
    }

}
