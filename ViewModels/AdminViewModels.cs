using System.ComponentModel.DataAnnotations;
using KABILIN.Helpers;
using KABILIN.Models;

namespace KABILIN.ViewModels
{
    // ---------- Dashboard ----------
    public class ChartSlice
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Color { get; set; } = "#3B6255";
    }

    public class AdminDashboardViewModel
    {
        public int FamilyRepresentatives { get; set; }
        public int DeceasedRecords { get; set; }
        public int TotalRequests { get; set; }
        public int Pending { get; set; }
        public int UnderReview { get; set; }
        public int Incomplete { get; set; }
        public int Processing { get; set; }
        public int Completed { get; set; }
        public int Rejected { get; set; }
        public int FuneralHomes { get; set; }
        public int Overdue { get; set; }

        public List<ChartSlice> StatusDistribution { get; set; } = new();
        public List<ChartSlice> CategoryDistribution { get; set; } = new();
        public List<AdminRequestRow> RecentRequests { get; set; } = new();
        public List<AdminActivityRow> RecentActivities { get; set; } = new();
    }

    public class AdminRequestRow
    {
        public int RequestID { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string DeceasedName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public DateTime DateFiled { get; set; }
        public int StatusID { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateOnly? EstimatedCompletionDate { get; set; }
        public bool IsOverdue =>
            EstimatedCompletionDate.HasValue && !StatusIds.IsClosed(StatusID) && EstimatedCompletionDate.Value < Ph.Today;
    }

    public class AdminActivityRow
    {
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserName { get; set; } = "System";
        public string? Reference { get; set; }
        public int? RequestID { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ---------- Users ----------
    public class AdminUserRow
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string ContactNum { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int DeceasedCount { get; set; }
        public int RequestCount { get; set; }
    }

    public class AdminUserDetailsViewModel
    {
        public User User { get; set; } = null!;
        public List<Deceased> Deceased { get; set; } = new();
        public List<AdminRequestRow> Requests { get; set; } = new();
    }

    // ---------- Deceased ----------
    public class AdminDeceasedRow
    {
        public int DeceasedID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateOnly DateOfDeath { get; set; }
        public string? PlaceOfDeath { get; set; }
        public int UserID { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public int RequestCount { get; set; }
    }

    public class AdminDeceasedDetailsViewModel
    {
        public Deceased Record { get; set; } = null!;
        public List<AdminRequestRow> Requests { get; set; } = new();
    }

    // ---------- Funeral homes ----------
    public class FuneralHomeFormViewModel : IValidatableObject
    {
        public int FuneralID { get; set; }

        [Required(ErrorMessage = "Please enter the funeral home name.")]
        [MaxLength(150)]
        [Display(Name = "Funeral home name")]
        public string FuneralName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the complete address.")]
        [MaxLength(255)]
        [Display(Name = "Complete address")]
        public string Location { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "City or municipality")]
        public string? City { get; set; }

        [Required(ErrorMessage = "Please enter a contact number.")]
        [RegularExpression(@"^[0-9+\-\s()/,]{6,20}$", ErrorMessage = "Enter a valid contact number.")]
        [Display(Name = "Contact number")]
        public string ContactNum { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(255)]
        [Display(Name = "Email address")]
        public string? EmailAddress { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Services offered")]
        public string? Services { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double? Longitude { get; set; }

        public bool IsActive { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Latitude.HasValue != Longitude.HasValue)
                yield return new ValidationResult("Enter both latitude and longitude, or leave both empty.",
                    new[] { Latitude.HasValue ? nameof(Longitude) : nameof(Latitude) });
        }
    }

    // ---------- Requests: admin actions ----------
    public class AdminRequestFilter
    {
        public string? Q { get; set; }
        public int? Status { get; set; }
        public int? Type { get; set; }
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
        public bool Overdue { get; set; }
    }

    // ---------- Notifications ----------
    public class SendNotificationViewModel
    {
        [Required(ErrorMessage = "Please choose who should receive this.")]
        [Display(Name = "Recipient")]
        public int? UserID { get; set; }

        [Display(Name = "Request reference")]
        [MaxLength(30)]
        public string? ReferenceNumber { get; set; }

        [Required(ErrorMessage = "Please enter a title.")]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please write the message.")]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;
    }

    public class AdminNotificationRow
    {
        public int NotificationID { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public int? RequestID { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ---------- Requirements ----------
    public class RequirementFormViewModel
    {
        public int RequirementID { get; set; }

        [Required(ErrorMessage = "Choose the request type.")]
        [Display(Name = "Request type")]
        public int? TypeID { get; set; }

        [Required(ErrorMessage = "Please enter a name.")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(400)]
        public string? Description { get; set; }

        [Display(Name = "This document is required")]
        public bool IsRequired { get; set; } = true;

        [Range(0, 999)]
        [Display(Name = "Display order")]
        public int SortOrder { get; set; } = 10;

        public bool IsActive { get; set; } = true;
    }
}
