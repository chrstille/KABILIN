using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    /// <summary>Immutable record of each verification decision made on an uploaded document.</summary>
    public class VerificationRecord
    {
        [Key]
        public int VerificationID { get; set; }

        [Required]
        public int DocumentID { get; set; }

        [Required]
        public int RequestID { get; set; }

        [Required]
        public int RequirementID { get; set; }

        /// <summary>Verified or Incomplete.</summary>
        [Required]
        [MaxLength(20)]
        public string Result { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        [Required]
        public int VerifiedByUserID { get; set; }

        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;

        public UploadedDocument? Document { get; set; }
        public User? VerifiedBy { get; set; }
    }
}
