using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    public class UploadedDocument
    {
        [Key]
        public int DocumentID { get; set; }

        [Required]
        public int RequestID { get; set; }

        [Required]
        public int RequirementID { get; set; }

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>Random server-side file name; the original name is never used on disk.</summary>
        [Required]
        [MaxLength(80)]
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public int UploadedByUserID { get; set; }

        /// <summary>Pending, Verified or Incomplete.</summary>
        [Required]
        [MaxLength(20)]
        public string VerificationStatus { get; set; } = VerificationStates.Pending;

        [MaxLength(1000)]
        public string? VerificationRemarks { get; set; }

        public int? VerifiedByUserID { get; set; }

        public DateTime? VerifiedAt { get; set; }

        /// <summary>False once the file has been replaced by a newer upload (kept for the audit trail).</summary>
        public bool IsCurrent { get; set; } = true;

        public DocumentRequest? Request { get; set; }
        public DocumentRequirement? Requirement { get; set; }
        public User? UploadedBy { get; set; }
    }
}
