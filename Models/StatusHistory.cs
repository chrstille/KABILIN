using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    public class StatusHistory
    {
        [Key]
        public int HistoryID { get; set; }

        [Required]
        public int RequestID { get; set; }

        /// <summary>The status the request moved INTO.</summary>
        [Required]
        public int StatusID { get; set; }

        [Required]
        public DateTime DateChanged { get; set; } = DateTime.UtcNow;

        /// <summary>The status the request moved FROM (null for the first entry).</summary>
        public int? PreviousStatusID { get; set; }

        /// <summary>Personnel (or the family representative, for submission) responsible.</summary>
        public int? ChangedByUserID { get; set; }

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        public DocumentRequest? Request { get; set; }
        public Status? Status { get; set; }
        public User? ChangedBy { get; set; }
    }
}
