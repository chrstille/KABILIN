namespace KABILIN.Helpers
{
    /// <summary>Philippine time (UTC+8, no daylight saving) formatting. The database stores UTC.</summary>
    public static class Ph
    {
        private static readonly TimeSpan Offset = TimeSpan.FromHours(8);

        public static DateTime ToLocal(DateTime utc) =>
            DateTime.SpecifyKind(utc, DateTimeKind.Utc).Add(Offset);

        public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.Add(Offset));

        public static string Date(DateTime utc) => ToLocal(utc).ToString("MMM d, yyyy");
        public static string DateTimeText(DateTime utc) => ToLocal(utc).ToString("MMM d, yyyy h:mm tt");
        public static string Date(DateOnly d) => d.ToString("MMM d, yyyy");
        public static string Date(DateOnly? d) => d.HasValue ? Date(d.Value) : "-";

        public static string Ago(DateTime utc)
        {
            var span = DateTime.UtcNow - DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hr ago";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} day{((int)span.TotalDays == 1 ? "" : "s")} ago";
            return Date(utc);
        }

        public static string FileSize(long bytes) =>
            bytes >= 1024 * 1024 ? $"{bytes / 1024d / 1024d:0.0} MB" :
            bytes >= 1024 ? $"{bytes / 1024d:0} KB" : $"{bytes} B";
    }
}
