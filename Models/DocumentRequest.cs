using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    public class DocumentRequest
    {
        [Key]
        public int RequestID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int DeceasedID { get; set; }

        [Required]
        public int TypeID { get; set; }

        [Required]
        public int StatusID { get; set; }

        [Required]
        public DateTime DateFiled { get; set; } = DateTime.UtcNow;

        public DateTime? DateReleased { get; set; }
    }
}