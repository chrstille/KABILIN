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
    [Route("Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _db;
        public AdminDashboardController(AppDbContext db) => _db = db;

        private static readonly Dictionary<int, string> StatusColors = new()
        {
            [StatusIds.Submitted] = "#96A797",
            [StatusIds.UnderReview] = "#6F8FA6",
            [StatusIds.Incomplete] = "#D1A441",
            [StatusIds.Processing] = "#3F5F78",
            [StatusIds.Approved] = "#7DAA8E",
            [StatusIds.Completed] = "#3B6255",
            [StatusIds.Rejected] = "#B5544B"
        };

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var byStatus = await _db.DocumentRequests.AsNoTracking()
                .GroupBy(r => r.StatusID).Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);
            int C(int id) => byStatus.GetValueOrDefault(id);

            var statusNames = await _db.Statuses.AsNoTracking().ToDictionaryAsync(s => s.StatusID, s => s.StatusName);
            var today = Ph.Today;

            var vm = new AdminDashboardViewModel
            {
                FamilyRepresentatives = await _db.Users.CountAsync(u => u.Role == AppRoles.Family),
                DeceasedRecords = await _db.Deceased.CountAsync(),
                TotalRequests = byStatus.Values.Sum(),
                Pending = C(StatusIds.Submitted),
                UnderReview = C(StatusIds.UnderReview),
                Incomplete = C(StatusIds.Incomplete),
                Processing = C(StatusIds.Processing) + C(StatusIds.Approved),
                Completed = C(StatusIds.Completed),
                Rejected = C(StatusIds.Rejected),
                FuneralHomes = await _db.FuneralHomes.CountAsync(h => h.IsActive),
                Overdue = await _db.DocumentRequests.CountAsync(r =>
                    r.EstimatedCompletionDate != null && r.EstimatedCompletionDate < today
                    && r.StatusID != StatusIds.Completed && r.StatusID != StatusIds.Rejected)
            };

            vm.StatusDistribution = StatusIds.DisplayOrder
                .Select(id => new ChartSlice { Label = statusNames.GetValueOrDefault(id, "?"), Count = C(id), Color = StatusColors[id] })
                .ToList();

            var byType = await _db.DocumentRequests.AsNoTracking()
                .GroupBy(r => r.TypeID).Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Key, g => g.Count);
            vm.CategoryDistribution = (await _db.RequestTypes.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync())
                .Select(t => new ChartSlice { Label = t.TypeName, Count = byType.GetValueOrDefault(t.TypeID) }).ToList();

            vm.RecentRequests = await AdminRequestsController.ProjectRows(
                _db.DocumentRequests.AsNoTracking().OrderByDescending(r => r.DateFiled).ThenByDescending(r => r.RequestID).Take(6)).ToListAsync();

            vm.RecentActivities = await _db.ActivityLogs.AsNoTracking()
                .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.LogID).Take(8)
                .Select(a => new AdminActivityRow
                {
                    Action = a.Action,
                    Description = a.Description,
                    UserName = a.User == null ? "System" : a.User.FirstName + " " + a.User.LastName,
                    Reference = a.Request == null ? null : a.Request.ReferenceNumber,
                    RequestID = a.RequestID,
                    CreatedAt = a.CreatedAt
                }).ToListAsync();

            return View(vm);
        }
    }
}
