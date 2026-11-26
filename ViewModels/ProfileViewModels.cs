using System.ComponentModel.DataAnnotations;

namespace KABILIN.ViewModels
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Please enter your first name.")]
        [MaxLength(100)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Middle name")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Please enter your last name.")]
        [MaxLength(100)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your contact number.")]
        [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid contact number.")]
        [Display(Name = "Contact number")]
        public string ContactNum { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your address.")]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your city.")]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        // Display only
        public string EmailAddress { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Please enter your current password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a new password.")]
        [MinLength(8, ErrorMessage = "Use at least 8 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm new password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class ProfilePageViewModel
    {
        public ProfileViewModel Profile { get; set; } = new();
        public ChangePasswordViewModel Password { get; set; } = new();
    }
}
