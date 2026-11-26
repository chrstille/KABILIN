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

        /// <summary>Complete address.</summary>
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

        [MaxLength(100)]
        public string? City { get; set; }

        /// <summary>Services offered, one per line.</summary>
        [MaxLength(1000)]
        public string? Services { get; set; }

        [Range(-90, 90)]
        public double? Latitude { get; set; }

        [Range(-180, 180)]
        public double? Longitude { get; set; }

        /// <summary>False = archived (hidden from the Family Portal).</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public IEnumerable<string> ServiceList =>
            (Services ?? string.Empty)
                .Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
    }
}
