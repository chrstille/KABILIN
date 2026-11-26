using KABILIN.Helpers;
using KABILIN.Models;

namespace KABILIN.ViewModels
{
    public class RequestTypeOption
    {
        public int TypeID { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<DocumentRequirement> Requirements { get; set; } = new();
    }

    public class DeceasedOption
    {
        public int DeceasedID { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class NewRequestViewModel
    {
        public int? DeceasedID { get; set; }
        public int? TypeID { get; set; }
        public string? FamilyNotes { get; set; }
        public bool Confirm { get; set; }

        public List<DeceasedOption> Deceased { get; set; } = new();
        public List<RequestTypeOption> Types { get; set; } = new();
    }

    public class RequestListItem
    {
        public int RequestID { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public int TypeID { get; set; }
        public string DeceasedName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public int StatusID { get; set; }
        public DateTime DateFiled { get; set; }
        public DateOnly? EstimatedCompletionDate { get; set; }
        public string? LatestRemarks { get; set; }
        public int RequiredCount { get; set; }
        public int VerifiedCount { get; set; }
        public int FlaggedCount { get; set; }
        public List<ProgressStep> Steps { get; set; } = new();
        public bool IsOverdue =>
            EstimatedCompletionDate.HasValue && !StatusIds.IsClosed(StatusID) && EstimatedCompletionDate.Value < Ph.Today;
    }

    public class RequirementRow
    {
        public DocumentRequirement Requirement { get; set; } = null!;
        public UploadedDocument? Current { get; set; }
        public List<UploadedDocument> Previous { get; set; } = new();
    }

    public class HistoryItem
    {
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public DateTime When { get; set; }
    }

    /// <summary>Everything needed to render a request, for both the family and admin views.</summary>
    public class RequestDetailsViewModel
    {
        public DocumentRequest Request { get; set; } = null!;
        public List<RequirementRow> Requirements { get; set; } = new();
        public List<HistoryItem> History { get; set; } = new();
        public List<ProgressStep> Steps { get; set; } = new();
        public bool CanUpload => !StatusIds.IsClosed(Request.StatusID);
        public bool IsOverdue =>
            Request.EstimatedCompletionDate.HasValue && !StatusIds.IsClosed(Request.StatusID)
            && Request.EstimatedCompletionDate.Value < Ph.Today;

        public int MissingRequired => Requirements.Count(r => r.Requirement.IsRequired && r.Current == null);
        public int NeedsCorrection => Requirements.Count(r => r.Current?.VerificationStatus == VerificationStates.Incomplete);

        // Admin only
        public List<Status> AllowedNextStatuses { get; set; } = new();
        public List<VerificationRecord> Verifications { get; set; } = new();
    }

    public class NotificationListViewModel
    {
        public PagedResult<Notification> Items { get; set; } = new();
        public string Filter { get; set; } = "all";
        public int UnreadCount { get; set; }
    }
}
