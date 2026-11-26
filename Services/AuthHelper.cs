using System.Security.Claims;
using KABILIN.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace KABILIN.Services
{
    public static class AuthHelper
    {
        public const string PasswordChangeClaim = "kabilin:pwd_change";

        public static ClaimsPrincipal BuildPrincipal(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.EmailAddress),
                new(ClaimTypes.Role, user.Role)
            };
            if (user.MustChangePassword) claims.Add(new Claim(PasswordChangeClaim, "1"));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        }

        public static int? GetUserId(this ClaimsPrincipal principal) =>
            int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsInRole(AppRoles.Admin);
    }
}
