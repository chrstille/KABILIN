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
    [Route("Admin/Settings")]
    public class AdminSettingsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IActivityLogger _log;
        public AdminSettingsController(AppDbContext db, IActivityLogger log) { _db = db; _log = log; }
        private int AdminId => User.GetUserId()!.Value;

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            ViewData["Admin"] = await _db.Users.AsNoTracking().FirstAsync(u => u.UserID == AdminId);
            return View();
        }

        // ---------- Document requirements ----------
        [HttpGet("Requirements")]
        public async Task<IActionResult> Requirements()
        {
            ViewData["Types"] = await _db.RequestTypes.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync();
            return View(await _db.DocumentRequirements.AsNoTracking().OrderBy(r => r.TypeID).ThenBy(r => r.SortOrder).ToListAsync());
        }

        [HttpGet("Requirements/Create")]
        public async Task<IActionResult> RequirementCreate(int? typeId)
        {
            await LoadTypesAsync();
            return View("RequirementForm", new RequirementFormViewModel { TypeID = typeId });
        }

        [HttpPost("Requirements/Create")]
        public async Task<IActionResult> RequirementCreate(RequirementFormViewModel model)
        {
            if (!ModelState.IsValid) { await LoadTypesAsync(); return View("RequirementForm", model); }
            var r = new DocumentRequirement { TypeID = model.TypeID!.Value };
            Apply(r, model);
            _db.DocumentRequirements.Add(r);
            _log.Add(AdminId, "Requirement added", $"Added requirement \"{r.Name}\".", "Requirement");
            await _db.SaveChangesAsync();
            TempData["Success"] = "The requirement was added. New requests will include it.";
            return RedirectToAction(nameof(Requirements));
        }

        [HttpGet("Requirements/Edit/{id:int}")]
        public async Task<IActionResult> RequirementEdit(int id)
        {
            var r = await _db.DocumentRequirements.AsNoTracking().FirstOrDefaultAsync(x => x.RequirementID == id);
            if (r == null) return NotFound();
            await LoadTypesAsync();
            return View("RequirementForm", new RequirementFormViewModel
            {
                RequirementID = r.RequirementID, TypeID = r.TypeID, Name = r.Name, Description = r.Description,
                IsRequired = r.IsRequired, SortOrder = r.SortOrder, IsActive = r.IsActive
            });
        }

        [HttpPost("Requirements/Edit/{id:int}")]
        public async Task<IActionResult> RequirementEdit(int id, RequirementFormViewModel model)
        {
            if (!ModelState.IsValid) { await LoadTypesAsync(); return View("RequirementForm", model); }
            var r = await _db.DocumentRequirements.FirstOrDefaultAsync(x => x.RequirementID == id);
            if (r == null) return NotFound();
            Apply(r, model);
            r.TypeID = model.TypeID!.Value;
            _log.Add(AdminId, "Requirement updated", $"Updated requirement \"{r.Name}\".", "Requirement", r.RequirementID);
            await _db.SaveChangesAsync();
            TempData["Success"] = "The requirement was updated.";
            return RedirectToAction(nameof(Requirements));
        }

        [HttpPost("Requirements/SetActive/{id:int}")]
        public async Task<IActionResult> RequirementSetActive(int id, bool active)
        {
            var r = await _db.DocumentRequirements.FirstOrDefaultAsync(x => x.RequirementID == id);
            if (r == null) return NotFound();
            r.IsActive = active;
            _log.Add(AdminId, active ? "Requirement enabled" : "Requirement disabled", $"{(active ? "Enabled" : "Disabled")} requirement \"{r.Name}\".", "Requirement", id);
            await _db.SaveChangesAsync();
            TempData["Success"] = active ? "The requirement is active again." : "The requirement was disabled for new requests.";
            return RedirectToAction(nameof(Requirements));
        }

        private async Task LoadTypesAsync() =>
            ViewData["Types"] = await _db.RequestTypes.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync();

        private static void Apply(DocumentRequirement r, RequirementFormViewModel m)
        {
            r.Name = m.Name.Trim();
            r.Description = string.IsNullOrWhiteSpace(m.Description) ? null : m.Description.Trim();
            r.IsRequired = m.IsRequired;
            r.SortOrder = m.SortOrder;
            r.IsActive = m.IsActive;
        }
    }
}
