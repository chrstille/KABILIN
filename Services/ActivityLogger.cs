using KABILIN.Data;
using KABILIN.Models;

namespace KABILIN.Services
{
    public interface IActivityLogger
    {
        /// <summary>Queues a log entry on the current DbContext; the caller saves.</summary>
        void Add(int? userId, string action, string description,
                 string? entityType = null, int? entityId = null, int? requestId = null);
    }

    public class ActivityLogger : IActivityLogger
    {
        private readonly AppDbContext _db;
        public ActivityLogger(AppDbContext db) => _db = db;

        public void Add(int? userId, string action, string description,
                        string? entityType = null, int? entityId = null, int? requestId = null)
        {
            _db.ActivityLogs.Add(new ActivityLog
            {
                UserID = userId,
                Action = action,
                Description = description.Length > 600 ? description[..600] : description,
                EntityType = entityType,
                EntityID = entityId,
                RequestID = requestId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
