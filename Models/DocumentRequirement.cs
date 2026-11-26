using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    /// <summary>A document that must (or may) be supplied for a given request type.</summary>
    public class DocumentRequirement
    {
        [Key]
        public int RequirementID { get; set; }

        [Required]
        public int TypeID { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? Description { get; set; }

        public bool IsRequired { get; set; } = true;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public RequestType? Type { get; set; }
    }
}
