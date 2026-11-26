using KABILIN.Data;
using KABILIN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KABILIN.Controllers
{
    [Authorize(Roles = AppRoles.Family)]
    public class FuneralHomesController : Controller
    {
        private readonly AppDbContext _db;
        public FuneralHomesController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? q, string? city)
        {
            var query = _db.FuneralHomes.AsNoTracking().Where(h => h.IsActive);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(h => h.FuneralName.ToLower().Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(city))
                query = query.Where(h => h.City == city);

            ViewData["Cities"] = await _db.FuneralHomes.AsNoTracking()
                .Where(h => h.IsActive && h.City != null && h.City != "")
                .Select(h => h.City!).Distinct().OrderBy(c => c).ToListAsync();
            ViewData["Query"] = q;
            ViewData["City"] = city;
            ViewData["TotalActive"] = await _db.FuneralHomes.CountAsync(h => h.IsActive);

            return View(await query.OrderBy(h => h.FuneralName).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var home = await _db.FuneralHomes.AsNoTracking().FirstOrDefaultAsync(h => h.FuneralID == id && h.IsActive);
            return home == null ? NotFound() : View(home);
        }
    }
}
