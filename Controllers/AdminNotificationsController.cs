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
    [Route("Admin/Notifications")]
    public class AdminNotificationsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly INotificationService _notify;
        private readonly IActivityLogger _log;
        public AdminNotificationsController(AppDbContext db, INotificationService notify, IActivityLogger log)
        { _db = db; _notify = notify; _log = log; }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q, string? state, int page = 1)
        {
            var query = _db.Notifications.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(n => n.Title.ToLower().Contains(term) || n.Message.ToLower().Contains(term)
                    || _db.Users.Any(u => u.UserID == n.UserID && (u.FirstName + " " + u.LastName).ToLower().Contains(term)));
            }
            if (state == "unread") query = query.Where(n => !n.IsRead);
            if (state == "read") query = query.Where(n => n.IsRead);

            var rows = query.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.NotificationID)
                .Select(n => new AdminNotificationRow
                {
                    NotificationID = n.NotificationID,
                    RecipientName = _db.Users.Where(u => u.UserID == n.UserID).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault() ?? "",
                    Title = n.Title, Message = n.Message,
                    RequestID = n.RequestID,
                    Reference = _db.DocumentRequests.Where(r => r.RequestID == n.RequestID).Select(r => r.ReferenceNumber).FirstOrDefault(),
                    IsRead = n.IsRead, CreatedAt = n.CreatedAt
                });

            ViewData["Query"] = q; ViewData["State"] = state;
            return View(await PagedResult<AdminNotificationRow>.CreateAsync(rows, page, 12));
        }

        [HttpGet("Send")]
        public async Task<IActionResult> Send(int? userId, string? reference)
        {
            await LoadRecipientsAsync();
            return View(new SendNotificationViewModel { UserID = userId, ReferenceNumber = reference });
        }

        [HttpPost("Send")]
        public async Task<IActionResult> Send(SendNotificationViewModel model)
        {
            int? requestId = null;
            if (ModelState.IsValid)
            {
                var recipientOk = await _db.Users.AnyAsync(u => u.UserID == model.UserID && u.Role == AppRoles.Family && u.IsActive);
                if (!recipientOk) ModelState.AddModelError(nameof(model.UserID), "Choose an active family representative.");

                if (!string.IsNullOrWhiteSpace(model.ReferenceNumber))
                {
                    var reference = model.ReferenceNumber.Trim().ToUpper();
                    var req = await _db.DocumentRequests.AsNoTracking()
                        .Where(r => r.ReferenceNumber == reference && r.UserID == model.UserID)
                        .Select(r => new { r.RequestID }).FirstOrDefaultAsync();
                    if (req == null) ModelState.AddModelError(nameof(model.ReferenceNumber), "That reference number was not found for this recipient.");
                    else requestId = req.RequestID;
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadRecipientsAsync();
                return View(model);
            }

            _notify.Add(model.UserID!.Value, model.Title.Trim(), model.Message.Trim(), requestId);
            _log.Add(User.GetUserId(), "Notification sent", $"Sent notification \"{model.Title.Trim()}\" to a family representative.",
                "User", model.UserID, requestId);
            await _db.SaveChangesAsync();

            TempData["Success"] = "The notification was sent.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadRecipientsAsync() =>
            ViewData["Recipients"] = await _db.Users.AsNoTracking()
                .Where(u => u.Role == AppRoles.Family && u.IsActive)
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                .Select(u => new { u.UserID, Name = u.FirstName + " " + u.LastName + " (" + u.EmailAddress + ")" })
                .ToDictionaryAsync(u => u.UserID, u => u.Name);
    }
}
