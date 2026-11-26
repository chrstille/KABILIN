using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    public class RequestType
    {
        [Key]
        public int TypeID { get; set; }

        [Required]
        [MaxLength(100)]
        public string TypeName { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        public int SortOrder { get; set; }

        public ICollection<DocumentRequirement> Requirements { get; set; } = new List<DocumentRequirement>();
    }
}
