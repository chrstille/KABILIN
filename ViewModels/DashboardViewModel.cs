namespace KABILIN.ViewModels
{
    public class DashboardViewModel
    {
        public string FirstName { get; set; } = string.Empty;

        public int TotalDeceased { get; set; }
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ProcessingRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int NeedAttention { get; set; }
        public int UnreadNotifications { get; set; }

        public List<DashboardRequestItem> RecentRequests { get; set; } = new();
        public List<DashboardNotificationItem> RecentNotifications { get; set; } = new();
        public List<DashboardActivityItem> RecentActivities { get; set; } = new();
        public List<DashboardTypeItem> RequestTypes { get; set; } = new();
    }

    public class DashboardRequestItem
    {
        public int RequestID { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string DeceasedName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public DateTime DateFiled { get; set; }
    }

    public class DashboardNotificationItem
    {
        public int NotificationID { get; set; }
        public int? RequestID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DashboardActivityItem
    {
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public int? RequestID { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DashboardTypeItem
    {
        public int TypeID { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
