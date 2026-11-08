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
    }
}