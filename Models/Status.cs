using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    public class Status
    {
        [Key]
        public int StatusID { get; set; }

        [Required]
        [MaxLength(50)]
        public string StatusName { get; set; } = string.Empty;
    }
}