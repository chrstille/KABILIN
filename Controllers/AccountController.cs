using System.Security.Claims;

using KABILIN.Data;
using KABILIN.Models;
using KABILIN.Services;
using KABILIN.ViewModels;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KABILIN.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IActivityLogger _log;
        private readonly INotificationService _notify;

        public AccountController(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher,
            IActivityLogger log,
            INotificationService notify)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _log = log;
            _notify = notify;
        }

        private IActionResult HomeFor(ClaimsPrincipal user) =>
            user.IsAdmin()
                ? RedirectToAction("Index", "AdminDashboard")
                : RedirectToAction(nameof(Dashboard));

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index() =>
            User.Identity?.IsAuthenticated == true ? HomeFor(User) : RedirectToAction(nameof(Login));

        // =====================================
        // REGISTRATION
        // =====================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true) return HomeFor(User);
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true) return HomeFor(User);
            if (!ModelState.IsValid) return View(model);

            string email = model.EmailAddress.Trim().ToLowerInvariant();

            if (await _context.Users.AnyAsync(u => u.EmailAddress.ToLower() == email))
            {
                ModelState.AddModelError(nameof(model.EmailAddress), "This email address is already registered.");
                return View(model);
            }

            var user = new User
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim(),
                ContactNum = model.ContactNum.Trim(),
                EmailAddress = email,
                Address = model.Address.Trim(),
                City = model.City.Trim(),
                Role = AppRoles.Family          // public registration is for families only
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _log.Add(user.UserID, "User registered", $"{user.FullName} created a family representative account.", "User", user.UserID);
            _notify.Add(user.UserID, "Welcome to KABILIN",
                "Your account is ready. Start by adding a deceased-person record, then choose the document you need.");
            await _context.SaveChangesAsync();

            TempData["Success"] = "Account created. You can now log in.";
            return RedirectToAction(nameof(Login));
        }

        // =====================================
        // LOGIN
        // =====================================
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return HomeFor(User);
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return HomeFor(User);
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);

            string email = model.EmailAddress.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailAddress.ToLower() == email);

            if (user == null ||
                _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Invalid email address or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "This account has been deactivated. Please contact the KABILIN administrator.");
                return View(model);
            }

            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            }

            user.LastLoginAt = DateTime.UtcNow;
            _log.Add(user.UserID, "Login", $"{user.FullName} signed in.", "User", user.UserID);
            await _context.SaveChangesAsync();

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                AuthHelper.BuildPrincipal(user),
                new AuthenticationProperties { IsPersistent = model.RememberMe });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return user.Role == AppRoles.Admin
                ? RedirectToAction("Index", "AdminDashboard")
                : RedirectToAction(nameof(Dashboard));
        }

        // =====================================
        // LOGOUT
        // =====================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            if (User.GetUserId() is int id)
            {
                _log.Add(id, "Logout", $"{User.Identity?.Name} signed out.", "User", id);
                await _context.SaveChangesAsync();
            }
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        // =====================================
        // FAMILY DASHBOARD
        // =====================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            if (User.IsAdmin()) return RedirectToAction("Index", "AdminDashboard");

            if (User.GetUserId() is not int userId)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Login));
            }

            var firstName = await _context.Users.AsNoTracking()
                .Where(u => u.UserID == userId).Select(u => u.FirstName).FirstOrDefaultAsync();
            if (firstName == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Login));
            }

            var mine = _context.DocumentRequests.AsNoTracking().Where(r => r.UserID == userId);

            var byStatus = await mine.GroupBy(r => r.StatusID)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);
            int Count(params int[] ids) => ids.Sum(i => byStatus.TryGetValue(i, out var c) ? c : 0);

            var vm = new DashboardViewModel
            {
                FirstName = firstName,
                TotalDeceased = await _context.Deceased.AsNoTracking().CountAsync(d => d.UserID == userId),
                TotalRequests = byStatus.Values.Sum(),
                PendingRequests = Count(StatusIds.Submitted, StatusIds.UnderReview, StatusIds.Incomplete),
                ProcessingRequests = Count(StatusIds.Processing, StatusIds.Approved),
                CompletedRequests = Count(StatusIds.Completed),
                NeedAttention = Count(StatusIds.Incomplete),
                UnreadNotifications = await _context.Notifications.AsNoTracking()
                    .CountAsync(n => n.UserID == userId && !n.IsRead)
            };

            vm.RecentRequests = await mine
                .OrderByDescending(r => r.DateFiled).ThenByDescending(r => r.RequestID)
                .Take(5)
                .Select(r => new DashboardRequestItem
                {
                    RequestID = r.RequestID,
                    ReferenceNumber = r.ReferenceNumber,
                    TypeName = r.Type!.TypeName,
                    DeceasedName = r.Deceased!.FirstName + " " + r.Deceased.LastName,
                    StatusName = r.Status!.StatusName,
                    DateFiled = r.DateFiled
                }).ToListAsync();

            vm.RecentNotifications = await _context.Notifications.AsNoTracking()
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.NotificationID)
                .Take(4)
                .Select(n => new DashboardNotificationItem
                {
                    NotificationID = n.NotificationID,
                    RequestID = n.RequestID,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToListAsync();

            vm.RecentActivities = await _context.ActivityLogs.AsNoTracking()
                .Where(a => a.RequestID != null && a.Request!.UserID == userId && a.Action != "Notification sent")
                .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.LogID)
                .Take(5)
                .Select(a => new DashboardActivityItem
                {
                    Action = a.Action,
                    Description = a.Description,
                    Reference = a.Request!.ReferenceNumber,
                    RequestID = a.RequestID,
                    CreatedAt = a.CreatedAt
                }).ToListAsync();

            vm.RequestTypes = await _context.RequestTypes.AsNoTracking()
                .OrderBy(t => t.SortOrder)
                .Select(t => new DashboardTypeItem { TypeID = t.TypeID, TypeName = t.TypeName, Description = t.Description })
                .ToListAsync();

            return View(vm);
        }

        // =====================================
        // PROFILE + PASSWORD
        // =====================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            if (User.GetUserId() is not int userId) return RedirectToAction(nameof(Login));
            var page = await BuildProfilePageAsync(userId);
            return page == null ? RedirectToAction(nameof(Login)) : View(page);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Profile([Bind(Prefix = "Profile")] ProfileViewModel model)
        {
            if (User.GetUserId() is not int userId) return RedirectToAction(nameof(Login));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return RedirectToAction(nameof(Login));

            // Only the profile form is being posted; clear unrelated password-form errors.
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Password.")).ToList())
                ModelState.Remove(key);

            if (!ModelState.IsValid)
            {
                return View(new ProfilePageViewModel { Profile = WithEmail(model, user.EmailAddress) });
            }

            user.FirstName = model.FirstName.Trim();
            user.MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim();
            user.LastName = model.LastName.Trim();
            user.ContactNum = model.ContactNum.Trim();
            user.Address = model.Address.Trim();
            user.City = model.City.Trim();

            _log.Add(userId, "Profile updated", $"{user.FullName} updated their profile.", "User", userId);
            await _context.SaveChangesAsync();

            // Refresh the display name stored in the cookie.
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                AuthHelper.BuildPrincipal(user), new AuthenticationProperties { IsPersistent = false });

            TempData["Success"] = "Your profile was updated.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordViewModel model, string? returnTo)
        {
            if (User.GetUserId() is not int userId) return RedirectToAction(nameof(Login));
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return RedirectToAction(nameof(Login));

            bool fromAdmin = returnTo == "admin" && User.IsAdmin();

            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Profile.")).ToList())
                ModelState.Remove(key);

            if (ModelState.IsValid &&
                _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Password.CurrentPassword", "Your current password is not correct.");
            }

            if (ModelState.IsValid && model.NewPassword == model.CurrentPassword)
            {
                ModelState.AddModelError("Password.NewPassword", "Choose a password different from your current one.");
            }

            if (!ModelState.IsValid)
            {
                if (fromAdmin)
                {
                    TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return RedirectToAction("Index", "AdminSettings");
                }
                var page = await BuildProfilePageAsync(userId);
                page!.Password = new ChangePasswordViewModel();
                ViewData["PasswordErrors"] = true;
                return View(nameof(Profile), page);
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
            user.MustChangePassword = false;
            _log.Add(userId, "Password changed", $"{user.FullName} changed their password.", "User", userId);
            await _context.SaveChangesAsync();

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                AuthHelper.BuildPrincipal(user), new AuthenticationProperties { IsPersistent = false });

            TempData["Success"] = "Your password was changed.";
            return fromAdmin ? RedirectToAction("Index", "AdminSettings") : RedirectToAction(nameof(Profile));
        }

        private async Task<ProfilePageViewModel?> BuildProfilePageAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == userId);
            if (user == null) return null;
            return new ProfilePageViewModel
            {
                Profile = new ProfileViewModel
                {
                    FirstName = user.FirstName,
                    MiddleName = user.MiddleName,
                    LastName = user.LastName,
                    ContactNum = user.ContactNum,
                    Address = user.Address,
                    City = user.City,
                    EmailAddress = user.EmailAddress
                }
            };
        }

        private static ProfileViewModel WithEmail(ProfileViewModel m, string email)
        {
            m.EmailAddress = email;
            return m;
        }
    }
}
