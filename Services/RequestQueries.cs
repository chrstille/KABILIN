using KABILIN.Data;
using KABILIN.Helpers;
using KABILIN.Models;
using KABILIN.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KABILIN.Services
{
    /// <summary>Shared read-side queries used by both the family portal and the admin dashboard.</summary>
    public static class RequestQueries
    {
        public static async Task<RequestDetailsViewModel?> LoadDetailsAsync(
            AppDbContext db, int requestId, int? onlyForUserId, bool forAdmin)
        {
            var query = db.DocumentRequests.AsNoTracking()
                .Include(r => r.Type).Include(r => r.Status)
                .Include(r => r.Deceased).Include(r => r.User)
                .Where(r => r.RequestID == requestId);
            if (onlyForUserId.HasValue) query = query.Where(r => r.UserID == onlyForUserId.Value);

            var request = await query.FirstOrDefaultAsync();
            if (request == null) return null;

            var requirements = await db.DocumentRequirements.AsNoTracking()
                .Where(r => r.TypeID == request.TypeID && r.IsActive)
                .OrderBy(r => r.SortOrder).ToListAsync();

            var docs = await db.UploadedDocuments.AsNoTracking()
                .Where(d => d.RequestID == requestId)
                .OrderByDescending(d => d.UploadedAt).ToListAsync();

            // Show requirements that were later disabled only if the family already uploaded to them.
            var extraIds = docs.Select(d => d.RequirementID).Except(requirements.Select(r => r.RequirementID)).ToList();
            if (extraIds.Count > 0)
                requirements.AddRange(await db.DocumentRequirements.AsNoTracking()
                    .Where(r => extraIds.Contains(r.RequirementID)).ToListAsync());

            var rows = requirements.Select(req => new RequirementRow
            {
                Requirement = req,
                Current = docs.FirstOrDefault(d => d.RequirementID == req.RequirementID && d.IsCurrent),
                Previous = docs.Where(d => d.RequirementID == req.RequirementID && !d.IsCurrent).ToList()
            }).ToList();

            var previousStatus = await db.StatusHistories.AsNoTracking()
                .Where(h => h.RequestID == requestId && h.StatusID == request.StatusID)
                .OrderByDescending(h => h.DateChanged).Select(h => h.PreviousStatusID).FirstOrDefaultAsync();

            var vm = new RequestDetailsViewModel
            {
                Request = request,
                Requirements = rows,
                Steps = RequestProgress.Build(request.StatusID, previousStatus),
                History = await LoadHistoryAsync(db, request, forAdmin)
            };

            if (forAdmin && StatusIds.Transitions.TryGetValue(request.StatusID, out var next) && next.Length > 0)
            {
                vm.AllowedNextStatuses = await db.Statuses.AsNoTracking()
                    .Where(s => next.Contains(s.StatusID)).ToListAsync();
                vm.AllowedNextStatuses = vm.AllowedNextStatuses
                    .OrderBy(s => Array.IndexOf(StatusIds.DisplayOrder, s.StatusID)).ToList();
            }
            return vm;
        }

        private static async Task<List<HistoryItem>> LoadHistoryAsync(AppDbContext db, DocumentRequest request, bool forAdmin)
        {
            var logs = await db.ActivityLogs.AsNoTracking()
                .Where(a => a.RequestID == request.RequestID && (forAdmin || a.Action != "Notification sent"))
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.LogID)
                .ToListAsync();

            string Actor(ActivityLog a)
            {
                if (a.User == null) return "System";
                if (forAdmin) return a.User.FullName + (a.User.Role == AppRoles.Admin ? " (staff)" : " (family)");
                return a.UserID == request.UserID ? "You" : "KABILIN staff";
            }

            if (logs.Count > 0)
                return logs.Select(a => new HistoryItem
                {
                    Title = a.Action, Text = a.Description, Actor = Actor(a), When = a.CreatedAt
                }).ToList();

            // Requests created before activity logging existed fall back to their status history.
            var names = await db.Statuses.AsNoTracking().ToDictionaryAsync(s => s.StatusID, s => s.StatusName);
            var hist = await db.StatusHistories.AsNoTracking()
                .Where(h => h.RequestID == request.RequestID)
                .OrderByDescending(h => h.DateChanged).ToListAsync();
            return hist.Select(h => new HistoryItem
            {
                Title = "Status: " + names.GetValueOrDefault(h.StatusID, "Updated"),
                Text = h.Remarks ?? "",
                Actor = "System",
                When = h.DateChanged
            }).ToList();
        }

        public static IQueryable<RequestListItem> ProjectList(AppDbContext db, IQueryable<DocumentRequest> source) =>
            source.Select(r => new RequestListItem
            {
                RequestID = r.RequestID,
                ReferenceNumber = r.ReferenceNumber,
                TypeID = r.TypeID,
                TypeName = r.Type!.TypeName,
                DeceasedName = r.Deceased!.FirstName + " " + r.Deceased.LastName,
                StatusID = r.StatusID,
                StatusName = r.Status!.StatusName,
                DateFiled = r.DateFiled,
                EstimatedCompletionDate = r.EstimatedCompletionDate,
                LatestRemarks = r.LatestRemarks,
                RequiredCount = db.DocumentRequirements.Count(x => x.TypeID == r.TypeID && x.IsActive && x.IsRequired),
                VerifiedCount = r.Documents.Count(d => d.IsCurrent && d.VerificationStatus == VerificationStates.Verified
                    && db.DocumentRequirements.Any(x => x.RequirementID == d.RequirementID && x.IsRequired)),
                FlaggedCount = r.Documents.Count(d => d.IsCurrent && d.VerificationStatus == VerificationStates.Incomplete)
            });
    }
}
