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
    [Route("Admin/Deceased")]
    public class AdminDeceasedController : Controller
    {
        private readonly AppDbContext _db;
        public AdminDeceasedController(AppDbContext db) => _db = db;

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, int? year, bool? hasRequests, int page = 1)
        {
            var query = _db.Deceased.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(d => (d.FirstName + " " + d.LastName).ToLower().Contains(term)
                    || (d.User!.FirstName + " " + d.User.LastName).ToLower().Contains(term));
            }
            if (year.HasValue) query = query.Where(d => d.DateOfDeath.Year == year);
            if (hasRequests == true) query = query.Where(d => d.Requests.Any());
            if (hasRequests == false) query = query.Where(d => !d.Requests.Any());

            var rows = query.OrderByDescending(d => d.CreatedAt).Select(d => new AdminDeceasedRow
            {
                DeceasedID = d.DeceasedID,
                FullName = d.FirstName + " " + d.LastName,
                DateOfDeath = d.DateOfDeath,
                PlaceOfDeath = d.PlaceOfDeath,
                UserID = d.UserID,
                FamilyName = d.User!.FirstName + " " + d.User.LastName,
                RequestCount = d.Requests.Count
            });

            ViewData["Query"] = q; ViewData["Year"] = year; ViewData["HasRequests"] = hasRequests;
            ViewData["Years"] = await _db.Deceased.AsNoTracking().Select(d => d.DateOfDeath.Year).Distinct().OrderByDescending(y => y).ToListAsync();
            return View(await PagedResult<AdminDeceasedRow>.CreateAsync(rows, page, 10));
        }

        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var record = await _db.Deceased.AsNoTracking().Include(d => d.User).FirstOrDefaultAsync(d => d.DeceasedID == id);
            if (record == null) return NotFound();
            return View(new AdminDeceasedDetailsViewModel
            {
                Record = record,
                Requests = await AdminRequestsController.ProjectRows(
                    _db.DocumentRequests.AsNoTracking().Where(r => r.DeceasedID == id).OrderByDescending(r => r.DateFiled)).ToListAsync()
            });
        }
    }
}
