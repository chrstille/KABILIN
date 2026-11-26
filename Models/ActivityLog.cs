using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    /// <summary>System-wide audit trail. Entries tied to a request also form the request history.</summary>
    public class ActivityLog
    {
        [Key]
        public int LogID { get; set; }

        public int? UserID { get; set; }

        [Required]
        [MaxLength(60)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(600)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? EntityType { get; set; }

        public int? EntityID { get; set; }

        public int? RequestID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public DocumentRequest? Request { get; set; }
    }
}
