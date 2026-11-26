using KABILIN.Data;
using KABILIN.Models;
using KABILIN.Services;
using KABILIN.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KABILIN.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    [Route("Admin/FuneralHomes")]
    public class AdminFuneralHomesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IActivityLogger _log;
        public AdminFuneralHomesController(AppDbContext db, IActivityLogger log) { _db = db; _log = log; }
        private int AdminId => User.GetUserId()!.Value;

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, string? status)
        {
            var query = _db.FuneralHomes.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(h => h.FuneralName.ToLower().Contains(term) || h.Location.ToLower().Contains(term));
            }
            if (status == "active") query = query.Where(h => h.IsActive);
            if (status == "archived") query = query.Where(h => !h.IsActive);
            ViewData["Query"] = q; ViewData["StatusFilter"] = status;
            return View(await query.OrderByDescending(h => h.IsActive).ThenBy(h => h.FuneralName).ToListAsync());
        }

        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var h = await _db.FuneralHomes.AsNoTracking().FirstOrDefaultAsync(x => x.FuneralID == id);
            return h == null ? NotFound() : View(h);
        }

        [HttpGet("Create")]
        public IActionResult Create() => View("Form", new FuneralHomeFormViewModel());

        [HttpPost("Create")]
        public async Task<IActionResult> Create(FuneralHomeFormViewModel model)
        {
            if (!ModelState.IsValid) return View("Form", model);
            var h = new FuneralHome();
            Apply(h, model);
            _db.FuneralHomes.Add(h);
            await _db.SaveChangesAsync();
            _log.Add(AdminId, "Funeral home added", $"Added funeral home \"{h.FuneralName}\".", "FuneralHome", h.FuneralID);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"\"{h.FuneralName}\" was added and is now visible to families.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var h = await _db.FuneralHomes.AsNoTracking().FirstOrDefaultAsync(x => x.FuneralID == id);
            if (h == null) return NotFound();
            return View("Form", new FuneralHomeFormViewModel
            {
                FuneralID = h.FuneralID, FuneralName = h.FuneralName, Location = h.Location, City = h.City,
                ContactNum = h.ContactNum, EmailAddress = h.EmailAddress, Services = h.Services, Description = h.Description,
                Latitude = h.Latitude, Longitude = h.Longitude, IsActive = h.IsActive
            });
        }

        [HttpPost("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id, FuneralHomeFormViewModel model)
        {
            if (!ModelState.IsValid) return View("Form", model);
            var h = await _db.FuneralHomes.FirstOrDefaultAsync(x => x.FuneralID == id);
            if (h == null) return NotFound();
            Apply(h, model);
            _log.Add(AdminId, "Funeral home updated", $"Updated funeral home \"{h.FuneralName}\".", "FuneralHome", h.FuneralID);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Changes saved. They are now live in the Family Portal.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("SetActive/{id:int}")]
        public async Task<IActionResult> SetActive(int id, bool active)
        {
            var h = await _db.FuneralHomes.FirstOrDefaultAsync(x => x.FuneralID == id);
            if (h == null) return NotFound();
            h.IsActive = active;
            _log.Add(AdminId, active ? "Funeral home restored" : "Funeral home archived",
                $"{(active ? "Restored" : "Archived")} funeral home \"{h.FuneralName}\".", "FuneralHome", h.FuneralID);
            await _db.SaveChangesAsync();
            TempData["Success"] = active ? $"\"{h.FuneralName}\" is visible to families again." : $"\"{h.FuneralName}\" was archived and hidden from families.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var h = await _db.FuneralHomes.FirstOrDefaultAsync(x => x.FuneralID == id);
            if (h == null) return NotFound();
            _db.FuneralHomes.Remove(h);
            _log.Add(AdminId, "Funeral home deleted", $"Permanently deleted funeral home \"{h.FuneralName}\".", "FuneralHome", id);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"\"{h.FuneralName}\" was deleted.";
            return RedirectToAction(nameof(Index));
        }

        private static void Apply(FuneralHome h, FuneralHomeFormViewModel m)
        {
            h.FuneralName = m.FuneralName.Trim();
            h.Location = m.Location.Trim();
            h.City = string.IsNullOrWhiteSpace(m.City) ? null : m.City.Trim();
            h.ContactNum = m.ContactNum.Trim();
            h.EmailAddress = string.IsNullOrWhiteSpace(m.EmailAddress) ? null : m.EmailAddress.Trim();
            h.Services = string.IsNullOrWhiteSpace(m.Services) ? null : string.Join("\n",
                m.Services.Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            h.Description = string.IsNullOrWhiteSpace(m.Description) ? null : m.Description.Trim();
            h.Latitude = m.Latitude;
            h.Longitude = m.Longitude;
            h.IsActive = m.IsActive;
        }
    }
}
