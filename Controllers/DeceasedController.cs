using KABILIN.Data;
using KABILIN.Models;
using KABILIN.Services;
using KABILIN.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KABILIN.Controllers
{
    [Authorize(Roles = AppRoles.Family)]
    public class DeceasedController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IActivityLogger _log;

        public DeceasedController(AppDbContext db, IActivityLogger log) { _db = db; _log = log; }

        private int UserId => User.GetUserId()!.Value;

        public async Task<IActionResult> Index(string? q)
        {
            var query = _db.Deceased.AsNoTracking().Where(d => d.UserID == UserId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(d => (d.FirstName + " " + d.LastName).ToLower().Contains(term));
            }
            var items = await query.OrderByDescending(d => d.CreatedAt)
                .Select(d => new DeceasedListItem
                {
                    DeceasedID = d.DeceasedID,
                    FullName = d.FirstName + (d.MiddleName != null && d.MiddleName != "" ? " " + d.MiddleName : "") + " " + d.LastName,
                    DateOfBirth = d.DateOfBirth,
                    DateOfDeath = d.DateOfDeath,
                    PlaceOfDeath = d.PlaceOfDeath,
                    RequestCount = d.Requests.Count
                }).ToListAsync();
            ViewData["Query"] = q;
            return View(items);
        }

        [HttpGet]
        public IActionResult Create() => View("Form", new DeceasedFormViewModel());

        [HttpPost]
        public async Task<IActionResult> Create(DeceasedFormViewModel model)
        {
            if (!ModelState.IsValid) return View("Form", model);

            var record = new Deceased { UserID = UserId };
            Apply(record, model);
            _db.Deceased.Add(record);
            await _db.SaveChangesAsync();

            _log.Add(UserId, "Deceased record added", $"Added deceased record for {record.FullName}.", "Deceased", record.DeceasedID);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"The record for {record.FullName} was saved. You can now request documents for it.";
            return RedirectToAction(nameof(Details), new { id = record.DeceasedID });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var d = await _db.Deceased.AsNoTracking().FirstOrDefaultAsync(x => x.DeceasedID == id && x.UserID == UserId);
            if (d == null) return NotFound();
            return View("Form", new DeceasedFormViewModel
            {
                DeceasedID = d.DeceasedID, FirstName = d.FirstName, MiddleName = d.MiddleName, LastName = d.LastName,
                DateOfBirth = d.DateOfBirth, DateOfDeath = d.DateOfDeath, PlaceOfDeath = d.PlaceOfDeath,
                CauseOfDeath = d.CauseOfDeath, Relationship = d.Relationship
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, DeceasedFormViewModel model)
        {
            if (!ModelState.IsValid) return View("Form", model);

            var d = await _db.Deceased.FirstOrDefaultAsync(x => x.DeceasedID == id && x.UserID == UserId);
            if (d == null) return NotFound();

            Apply(d, model);
            _log.Add(UserId, "Deceased record updated", $"Updated deceased record for {d.FullName}.", "Deceased", d.DeceasedID);
            await _db.SaveChangesAsync();

            TempData["Success"] = "The record was updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var d = await _db.Deceased.AsNoTracking().FirstOrDefaultAsync(x => x.DeceasedID == id && x.UserID == UserId);
            if (d == null) return NotFound();

            ViewData["Requests"] = await RequestQueries.ProjectList(_db,
                _db.DocumentRequests.AsNoTracking().Where(r => r.DeceasedID == id && r.UserID == UserId))
                .OrderByDescending(r => r.DateFiled).ToListAsync();
            return View(d);
        }

        private static void Apply(Deceased d, DeceasedFormViewModel m)
        {
            d.FirstName = m.FirstName.Trim();
            d.MiddleName = string.IsNullOrWhiteSpace(m.MiddleName) ? null : m.MiddleName.Trim();
            d.LastName = m.LastName.Trim();
            d.DateOfBirth = m.DateOfBirth;
            d.DateOfDeath = m.DateOfDeath!.Value;
            d.PlaceOfDeath = m.PlaceOfDeath?.Trim();
            d.CauseOfDeath = string.IsNullOrWhiteSpace(m.CauseOfDeath) ? null : m.CauseOfDeath.Trim();
            d.Relationship = m.Relationship;
        }
    }
}
