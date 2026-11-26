using System.ComponentModel.DataAnnotations;

namespace KABILIN.ViewModels
{
    public class DeceasedFormViewModel : IValidatableObject
    {
        public int DeceasedID { get; set; }

        [Required(ErrorMessage = "Please enter the first name.")]
        [MaxLength(100)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Middle name")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Please enter the last name.")]
        [MaxLength(100)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the date of birth.")]
        [Display(Name = "Date of birth")]
        public DateOnly? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Please enter the date of death.")]
        [Display(Name = "Date of death")]
        public DateOnly? DateOfDeath { get; set; }

        [Required(ErrorMessage = "Please enter the place of death.")]
        [MaxLength(255)]
        [Display(Name = "Place of death")]
        public string? PlaceOfDeath { get; set; }

        [MaxLength(255)]
        [Display(Name = "Cause of death")]
        public string? CauseOfDeath { get; set; }

        [Required(ErrorMessage = "Please tell us your relationship to the deceased.")]
        [MaxLength(100)]
        [Display(Name = "Your relationship")]
        public string? Relationship { get; set; }

        public static readonly string[] RelationshipOptions =
            { "Spouse", "Child", "Parent", "Sibling", "Grandchild", "Other relative", "Legal representative" };

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(8));
            if (DateOfDeath.HasValue && DateOfDeath.Value > today)
                yield return new ValidationResult("The date of death cannot be in the future.", new[] { nameof(DateOfDeath) });
            if (DateOfBirth.HasValue && DateOfBirth.Value > today)
                yield return new ValidationResult("The date of birth cannot be in the future.", new[] { nameof(DateOfBirth) });
            if (DateOfBirth.HasValue && DateOfDeath.HasValue && DateOfBirth.Value > DateOfDeath.Value)
                yield return new ValidationResult("The date of birth must be before the date of death.", new[] { nameof(DateOfBirth) });
        }
    }

    public class DeceasedListItem
    {
        public int DeceasedID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateOnly? DateOfBirth { get; set; }
        public DateOnly DateOfDeath { get; set; }
        public string? PlaceOfDeath { get; set; }
        public int RequestCount { get; set; }
    }
}
