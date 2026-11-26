using System.Security.Claims;
using KABILIN.Data;
using KABILIN.Models;
using KABILIN.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KABILIN.Components
{
    public class SidebarModel
    {
        public bool IsAdmin { get; set; }
        public string Active { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public int UnreadNotifications { get; set; }
    }

    public class SidebarViewComponent : ViewComponent
    {
        private readonly AppDbContext _db;
        public SidebarViewComponent(AppDbContext db) => _db = db;

        public async Task<IViewComponentResult> InvokeAsync(string? active)
        {
            var user = HttpContext.User;
            var model = new SidebarModel
            {
                IsAdmin = user.IsInRole(AppRoles.Admin),
                Active = active ?? "",
                FullName = user.FindFirstValue(ClaimTypes.Name) ?? "",
                Email = user.FindFirstValue(ClaimTypes.Email) ?? ""
            };

            if (!model.IsAdmin && user.GetUserId() is int id)
                model.UnreadNotifications = await _db.Notifications.AsNoTracking()
                    .CountAsync(n => n.UserID == id && !n.IsRead);

            return View(model);
        }
    }
}
