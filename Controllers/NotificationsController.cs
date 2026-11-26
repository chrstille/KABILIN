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
    [Authorize(Roles = AppRoles.Family)]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _db;
        public NotificationsController(AppDbContext db) => _db = db;
        private int UserId => User.GetUserId()!.Value;

        public async Task<IActionResult> Index(string? filter, int page = 1)
        {
            filter = filter == "unread" ? "unread" : "all";
            var query = _db.Notifications.AsNoTracking().Where(n => n.UserID == UserId);
            if (filter == "unread") query = query.Where(n => !n.IsRead);

            return View(new NotificationListViewModel
            {
                Filter = filter,
                UnreadCount = await _db.Notifications.CountAsync(n => n.UserID == UserId && !n.IsRead),
                Items = await PagedResult<Notification>.CreateAsync(
                    query.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.NotificationID), page, 15)
            });
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(int id, string? filter)
        {
            var n = await _db.Notifications.FirstOrDefaultAsync(x => x.NotificationID == id && x.UserID == UserId);
            if (n != null && !n.IsRead) { n.IsRead = true; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Index), new { filter });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            await _db.Notifications.Where(n => n.UserID == UserId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            TempData["Success"] = "All notifications were marked as read.";
            return RedirectToAction(nameof(Index));
        }
    }
}
