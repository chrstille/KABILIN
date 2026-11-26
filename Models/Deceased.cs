using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    public class Deceased
    {
        [Key]
        public int DeceasedID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required]
        public DateOnly DateOfDeath { get; set; }

        // Added for the document-processing workflow.
        // Nullable in the database so records created before this change remain valid.
        public DateOnly? DateOfBirth { get; set; }

        [MaxLength(255)]
        public string? PlaceOfDeath { get; set; }

        [MaxLength(255)]
        public string? CauseOfDeath { get; set; }

        [MaxLength(100)]
        public string? Relationship { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public ICollection<DocumentRequest> Requests { get; set; } = new List<DocumentRequest>();

        public string FullName =>
            string.Join(' ', new[] { FirstName, MiddleName, LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
