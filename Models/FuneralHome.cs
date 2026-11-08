using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    public class FuneralHome
    {
        [Key]
        public int FuneralID { get; set; }

        [Required]
        [MaxLength(150)]
        public string FuneralName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string ContactNum { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(255)]
        public string? EmailAddress { get; set; }

        public string? Description { get; set; }
    }
}