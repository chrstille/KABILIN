using System.Security.Claims;

using KABILIN.Data;
using KABILIN.Models;
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

        public AccountController(
            AppDbContext context,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // =====================================
        // REGISTRATION - GET
        // =====================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            return View();
        }

        // =====================================
        // REGISTRATION - POST
        // =====================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Normalize email address
            string email = model.EmailAddress.Trim().ToLowerInvariant();

            // Check if email is already registered
            bool emailExists = await _context.Users
                .AnyAsync(u => u.EmailAddress.ToLower() == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.EmailAddress),
                    "This email address is already registered."
                );

                return View(model);
            }

            // Create user account
            var user = new User
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                MiddleName = model.MiddleName?.Trim(),
                ContactNum = model.ContactNum.Trim(),
                EmailAddress = email,
                Address = model.Address.Trim(),
                City = model.City.Trim(),

                // Public registration is for families only
                Role = "Family"
            };

            // Securely hash the password
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                model.Password
            );

            // Save user to PostgreSQL
            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Account created successfully! You can now log in.";

            return RedirectToAction(nameof(Login));
        }

        // =====================================
        // LOGIN - GET
        // =====================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            return View();
        }

        // =====================================
        // LOGIN - POST
        // =====================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Dashboard));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = model.EmailAddress.Trim().ToLowerInvariant();

            // Find user in PostgreSQL
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailAddress.ToLower() == email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email address or password."
                );

                return View(model);
            }

            // Verify password
            var verificationResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.Password
            );

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email address or password."
                );

                return View(model);
            }

            // Upgrade the password hash when needed
            if (verificationResult ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(
                    user,
                    model.Password
                );

                await _context.SaveChangesAsync();
            }

            // Create authenticated user claims
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserID.ToString()
                ),

                new Claim(
                    ClaimTypes.Name,
                    $"{user.FirstName} {user.LastName}"
                ),

                new Claim(
                    ClaimTypes.Email,
                    user.EmailAddress
                ),

                new Claim(
                    ClaimTypes.Role,
                    user.Role
                )
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            // Sign in the user
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe
                }
            );

            return RedirectToAction(nameof(Dashboard));
        }

        // =====================================
        // TEMPORARY DASHBOARD
        // =====================================

        [HttpGet]
        [Authorize]
        public IActionResult Dashboard()
        {
            return View();
        }

        // =====================================
        // LOGOUT
        // =====================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction(nameof(Login));
        }
    }
}