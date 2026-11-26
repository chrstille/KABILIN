using KABILIN.Data;
using KABILIN.Helpers;
using KABILIN.Models;
using KABILIN.Services;
using KABILIN.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KABILIN.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    [Route("Admin/Users")]
    public class AdminUsersController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IActivityLogger _log;
        public AdminUsersController(AppDbContext db, IActivityLogger log) { _db = db; _log = log; }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, string? status, int page = 1)
        {
            var query = _db.Users.AsNoTracking().Where(u => u.Role == AppRoles.Family);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(u => (u.FirstName + " " + u.LastName).ToLower().Contains(term)
                    || u.EmailAddress.ToLower().Contains(term) || u.ContactNum.Contains(term));
            }
            if (status == "active") query = query.Where(u => u.IsActive);
            if (status == "inactive") query = query.Where(u => !u.IsActive);

            var rows = query.OrderByDescending(u => u.CreatedAt).Select(u => new AdminUserRow
            {
                UserID = u.UserID,
                FullName = u.FirstName + " " + u.LastName,
                EmailAddress = u.EmailAddress,
                ContactNum = u.ContactNum,
                City = u.City,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                DeceasedCount = _db.Deceased.Count(d => d.UserID == u.UserID),
                RequestCount = _db.DocumentRequests.Count(r => r.UserID == u.UserID)
            });

            ViewData["Query"] = q; ViewData["StatusFilter"] = status;
            return View(await PagedResult<AdminUserRow>.CreateAsync(rows, page, 10));
        }

        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null) return NotFound();

            return View(new AdminUserDetailsViewModel
            {
                User = user,
                Deceased = await _db.Deceased.AsNoTracking().Where(d => d.UserID == id).OrderByDescending(d => d.CreatedAt).ToListAsync(),
                Requests = await AdminRequestsController.ProjectRows(
                    _db.DocumentRequests.AsNoTracking().Where(r => r.UserID == id).OrderByDescending(r => r.DateFiled)).ToListAsync()
            });
        }

        [HttpPost("Toggle/{id:int}")]
        public async Task<IActionResult> Toggle(int id, string? returnTo)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null) return NotFound();

            var me = User.GetUserId();
            if (user.UserID == me || user.Role == AppRoles.Admin)
            {
                TempData["Error"] = "Administrator accounts cannot be deactivated here.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            _log.Add(me, user.IsActive ? "Account activated" : "Account deactivated",
                $"{(user.IsActive ? "Activated" : "Deactivated")} the account of {user.FullName} ({user.EmailAddress}).", "User", user.UserID);
            await _db.SaveChangesAsync();

            TempData["Success"] = user.IsActive ? $"{user.FullName}'s account was activated." : $"{user.FullName}'s account was deactivated and they were signed out.";
            return returnTo == "details" ? RedirectToAction(nameof(Details), new { id }) : RedirectToAction(nameof(Index));
        }
    }
}
