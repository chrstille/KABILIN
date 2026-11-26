using KABILIN.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KABILIN.Filters
{
    /// <summary>
    /// Outside Development, accounts flagged MustChangePassword (the seeded administrator)
    /// can only reach the password-change screens until the password is changed.
    /// </summary>
    public class RequirePasswordChangeFilter : IAsyncActionFilter
    {
        private readonly IWebHostEnvironment _env;
        public RequirePasswordChangeFilter(IWebHostEnvironment env) => _env = env;

        public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (!_env.IsDevelopment()
                && user.Identity?.IsAuthenticated == true
                && user.HasClaim(AuthHelper.PasswordChangeClaim, "1"))
            {
                var controller = context.RouteData.Values["controller"]?.ToString();
                var action = context.RouteData.Values["action"]?.ToString();
                var allowed = controller == "AdminSettings"
                    || (controller == "Account" && action is "ChangePassword" or "Logout" or "Profile" or "AccessDenied");
                if (!allowed)
                {
                    context.Result = user.IsAdmin()
                        ? new RedirectToActionResult("Index", "AdminSettings", null)
                        : new RedirectToActionResult("Profile", "Account", null);
                }
            }
            return context.Result == null ? next() : Task.CompletedTask;
        }
    }
}
