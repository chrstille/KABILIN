using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    public class DocumentRequest
    {
        [Key]
        public int RequestID { get; set; }

        /// <summary>Human-friendly unique reference, e.g. KBL-261125-4F7Q2.</summary>
        [Required]
        [MaxLength(30)]
        public string ReferenceNumber { get; set; } = string.Empty;

        [Required]
        public int UserID { get; set; }

        [Required]
        public int DeceasedID { get; set; }

        [Required]
        public int TypeID { get; set; }

        [Required]
        public int StatusID { get; set; }

        [Required]
        public DateTime DateFiled { get; set; } = DateTime.UtcNow;

        public DateTime? DateReleased { get; set; }

        /// <summary>An estimate only; never a guaranteed completion date.</summary>
        public DateOnly? EstimatedCompletionDate { get; set; }

        [MaxLength(1000)]
        public string? LatestRemarks { get; set; }

        [MaxLength(1000)]
        public string? FamilyNotes { get; set; }

        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Deceased? Deceased { get; set; }
        public RequestType? Type { get; set; }
        public Status? Status { get; set; }

        public ICollection<UploadedDocument> Documents { get; set; } = new List<UploadedDocument>();
        public ICollection<StatusHistory> Histories { get; set; } = new List<StatusHistory>();
    }
}
