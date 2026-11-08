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
    }
}