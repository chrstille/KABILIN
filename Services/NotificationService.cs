using KABILIN.Data;
using KABILIN.Models;

namespace KABILIN.Services
{
    public interface INotificationService
    {
        /// <summary>Queues a notification on the current DbContext; the caller saves.</summary>
        void Add(int userId, string title, string message, int? requestId = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _db;
        public NotificationService(AppDbContext db) => _db = db;

        public void Add(int userId, string title, string message, int? requestId = null)
        {
            _db.Notifications.Add(new Notification
            {
                UserID = userId,
                RequestID = requestId,
                Title = title.Length > 150 ? title[..150] : title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
