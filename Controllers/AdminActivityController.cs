using KABILIN.Data;
using KABILIN.Helpers;
using KABILIN.Models;
using KABILIN.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KABILIN.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    [Route("Admin/Activity")]
    public class AdminActivityController : Controller
    {
        private readonly AppDbContext _db;
        public AdminActivityController(AppDbContext db) => _db = db;

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, string? action, int page = 1)
        {
            var query = _db.ActivityLogs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(a => a.Description.ToLower().Contains(term)
                    || (a.User != null && (a.User.FirstName + " " + a.User.LastName).ToLower().Contains(term))
                    || (a.Request != null && a.Request.ReferenceNumber.ToLower().Contains(term)));
            }
            if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);

            var rows = query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.LogID)
                .Select(a => new AdminActivityRow
                {
                    Action = a.Action,
                    Description = a.Description,
                    UserName = a.User == null ? "System" : a.User.FirstName + " " + a.User.LastName,
                    Reference = a.Request == null ? null : a.Request.ReferenceNumber,
                    RequestID = a.RequestID,
                    CreatedAt = a.CreatedAt
                });

            ViewData["Query"] = q; ViewData["Action"] = action;
            ViewData["Actions"] = await _db.ActivityLogs.AsNoTracking().Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();
            return View(await PagedResult<AdminActivityRow>.CreateAsync(rows, page, 15));
        }
    }
}
