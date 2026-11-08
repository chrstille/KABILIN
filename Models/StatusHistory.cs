using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    public class StatusHistory
    {
        [Key]
        public int HistoryID { get; set; }

        [Required]
        public int RequestID { get; set; }

        [Required]
        public int StatusID { get; set; }

        [Required]
        public DateTime DateChanged { get; set; } = DateTime.UtcNow;
    }
}