using System.ComponentModel.DataAnnotations;

namespace KABILIN.Models
{
    public class User
    {
        [Key]
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
        [MaxLength(20)]
        public string ContactNum { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string EmailAddress { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = AppRoles.Family;

        /// <summary>Deactivated accounts cannot sign in.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>True for seeded accounts that still use their initial password.</summary>
        public bool MustChangePassword { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public string FullName =>
            string.Join(' ', new[] { FirstName, MiddleName, LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
