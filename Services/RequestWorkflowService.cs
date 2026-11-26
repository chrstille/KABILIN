using System.Security.Cryptography;
using KABILIN.Data;
using KABILIN.Helpers;
using KABILIN.Models;
using Microsoft.EntityFrameworkCore;

namespace KABILIN.Services
{
    public record WorkflowResult(bool Ok, string? Error = null, int? Id = null)
    {
        public static WorkflowResult Success(int? id = null) => new(true, null, id);
        public static WorkflowResult Fail(string error) => new(false, error);
    }

    /// <summary>
    /// All request lifecycle rules live here so the family portal and the admin dashboard
    /// share one consistent behaviour: status changes, history, audit log and notifications.
    /// </summary>
    public class RequestWorkflowService
    {
        private const string RefAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        private readonly AppDbContext _db;
        private readonly IFileStorage _files;
        private readonly IActivityLogger _log;
        private readonly INotificationService _notify;

        public RequestWorkflowService(AppDbContext db, IFileStorage files,
            IActivityLogger log, INotificationService notify)
        {
            _db = db; _files = files; _log = log; _notify = notify;
        }

        // ---------------------------------------------------------------
        // Submission (family)
        // ---------------------------------------------------------------
        public async Task<WorkflowResult> SubmitAsync(
            int userId, int deceasedId, int typeId, string? notes,
            IReadOnlyDictionary<int, IFormFile> files)
        {
            var deceased = await _db.Deceased
                .FirstOrDefaultAsync(d => d.DeceasedID == deceasedId && d.UserID == userId);
            if (deceased == null) return WorkflowResult.Fail("Please choose one of your deceased-person records.");

            var type = await _db.RequestTypes.FirstOrDefaultAsync(t => t.TypeID == typeId);
            if (type == null) return WorkflowResult.Fail("Please choose the document or service you need.");

            var requirements = await _db.DocumentRequirements
                .Where(r => r.TypeID == typeId && r.IsActive)
                .OrderBy(r => r.SortOrder).ToListAsync();

            var missing = requirements
                .Where(r => r.IsRequired && !files.ContainsKey(r.RequirementID))
                .Select(r => r.Name).ToList();
            if (missing.Count > 0)
                return WorkflowResult.Fail("Please upload the required document(s): " + string.Join(", ", missing) + ".");

            // Save files first; roll them back if anything fails.
            var saved = new List<(DocumentRequirement Req, IFormFile File, string Stored)>();
            foreach (var req in requirements)
            {
                if (!files.TryGetValue(req.RequirementID, out var file)) continue;
                var result = await _files.SaveAsync(file);
                if (!result.Ok)
                {
                    foreach (var s in saved) _files.Delete(s.Stored);
                    return WorkflowResult.Fail(result.Error!);
                }
                saved.Add((req, file, result.StoredName!));
            }

            try
            {
                var now = DateTime.UtcNow;
                var request = new DocumentRequest
                {
                    ReferenceNumber = await NewReferenceAsync(),
                    UserID = userId,
                    DeceasedID = deceasedId,
                    TypeID = typeId,
                    StatusID = StatusIds.Submitted,
                    DateFiled = now,
                    LastUpdatedAt = now,
                    FamilyNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
                };

                foreach (var s in saved)
                {
                    request.Documents.Add(new UploadedDocument
                    {
                        RequirementID = s.Req.RequirementID,
                        OriginalFileName = Path.GetFileName(s.File.FileName),
                        StoredFileName = s.Stored,
                        ContentType = FileStorage.ContentTypeFor(s.Stored),
                        FileSize = s.File.Length,
                        UploadedAt = now,
                        UploadedByUserID = userId
                    });
                }

                request.Histories.Add(new StatusHistory
                {
                    StatusID = StatusIds.Submitted,
                    DateChanged = now,
                    ChangedByUserID = userId,
                    Remarks = "Request submitted by the family representative."
                });

                _db.DocumentRequests.Add(request);
                await _db.SaveChangesAsync(); // need RequestID for notification/log links

                _log.Add(userId, "Request submitted",
                    $"Submitted {type.TypeName} request {request.ReferenceNumber} for {deceased.FullName}.",
                    "Request", request.RequestID, request.RequestID);
                foreach (var s in saved)
                    _log.Add(userId, "Document uploaded",
                        $"Uploaded \"{s.Req.Name}\" ({Path.GetFileName(s.File.FileName)}).",
                        "Document", null, request.RequestID);

                _notify.Add(userId, "Request submitted",
                    $"Your {type.TypeName} request ({request.ReferenceNumber}) for {deceased.FullName} was received. " +
                    "We will review your documents and update you here.", request.RequestID);

                await _db.SaveChangesAsync();
                return WorkflowResult.Success(request.RequestID);
            }
            catch
            {
                foreach (var s in saved) _files.Delete(s.Stored);
                throw;
            }
        }

        private async Task<string> NewReferenceAsync()
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var chars = new char[5];
                for (var i = 0; i < chars.Length; i++)
                    chars[i] = RefAlphabet[RandomNumberGenerator.GetInt32(RefAlphabet.Length)];
                var reference = $"KBL-{Ph.Today:yyMMdd}-{new string(chars)}";
                if (!await _db.DocumentRequests.AnyAsync(r => r.ReferenceNumber == reference))
                    return reference;
            }
            throw new InvalidOperationException("Could not generate a unique reference number.");
        }

        // ---------------------------------------------------------------
        // Upload / replace a document (family)
        // ---------------------------------------------------------------
        public async Task<WorkflowResult> UploadDocumentAsync(int userId, int requestId, int requirementId, IFormFile file)
        {
            var request = await _db.DocumentRequests
                .Include(r => r.Type)
                .Include(r => r.Documents)
                .FirstOrDefaultAsync(r => r.RequestID == requestId && r.UserID == userId);
            if (request == null) return WorkflowResult.Fail("Request not found.");
            if (StatusIds.IsClosed(request.StatusID))
                return WorkflowResult.Fail("This request is closed, so documents can no longer be changed.");

            var requirement = await _db.DocumentRequirements
                .FirstOrDefaultAsync(r => r.RequirementID == requirementId && r.TypeID == request.TypeID && r.IsActive);
            if (requirement == null) return WorkflowResult.Fail("That requirement does not belong to this request.");

            var current = request.Documents.FirstOrDefault(d => d.RequirementID == requirementId && d.IsCurrent);
            if (current is { VerificationStatus: VerificationStates.Verified })
                return WorkflowResult.Fail("This document has already been verified and does not need to be replaced.");

            var saved = await _files.SaveAsync(file);
            if (!saved.Ok) return WorkflowResult.Fail(saved.Error!);

            try
            {
                var now = DateTime.UtcNow;
                var replacing = current != null;
                if (current != null) current.IsCurrent = false;

                _db.UploadedDocuments.Add(new UploadedDocument
                {
                    RequestID = requestId,
                    RequirementID = requirementId,
                    OriginalFileName = Path.GetFileName(file.FileName),
                    StoredFileName = saved.StoredName!,
                    ContentType = FileStorage.ContentTypeFor(saved.StoredName!),
                    FileSize = file.Length,
                    UploadedAt = now,
                    UploadedByUserID = userId
                });

                request.LastUpdatedAt = now;
                _log.Add(userId, replacing ? "Document replaced" : "Document uploaded",
                    $"{(replacing ? "Replaced" : "Uploaded")} \"{requirement.Name}\" ({Path.GetFileName(file.FileName)}).",
                    "Document", null, requestId);

                // When every flagged document has been resubmitted, go back to review.
                if (request.StatusID == StatusIds.Incomplete)
                {
                    await _db.SaveChangesAsync();
                    var stillFlagged = await _db.UploadedDocuments.AnyAsync(d =>
                        d.RequestID == requestId && d.IsCurrent && d.VerificationStatus == VerificationStates.Incomplete);
                    if (!stillFlagged)
                        ApplyStatus(request, StatusIds.UnderReview, null, null,
                            "Status changed from Incomplete Requirements to Under Review (all requested documents were resubmitted).",
                            notifyFamily: true);
                }

                _notify.Add(userId, "Document received",
                    $"We received \"{requirement.Name}\" for request {request.ReferenceNumber}.", requestId);

                await _db.SaveChangesAsync();
                return WorkflowResult.Success();
            }
            catch
            {
                _files.Delete(saved.StoredName!);
                throw;
            }
        }

        // ---------------------------------------------------------------
        // Status management (admin)
        // ---------------------------------------------------------------
        public async Task<WorkflowResult> ChangeStatusAsync(int requestId, int newStatusId, string? remarks, int adminId)
        {
            var request = await _db.DocumentRequests
                .Include(r => r.Documents)
                .FirstOrDefaultAsync(r => r.RequestID == requestId);
            if (request == null) return WorkflowResult.Fail("Request not found.");

            if (!StatusIds.Transitions.TryGetValue(request.StatusID, out var allowed) || !allowed.Contains(newStatusId))
                return WorkflowResult.Fail("That status change is not allowed from the current status.");

            remarks = remarks?.Trim();
            if ((newStatusId == StatusIds.Incomplete || newStatusId == StatusIds.Rejected) && string.IsNullOrWhiteSpace(remarks))
                return WorkflowResult.Fail(newStatusId == StatusIds.Rejected
                    ? "Please explain why the request is being rejected."
                    : "Please explain what the family needs to submit or correct.");

            if (newStatusId == StatusIds.Processing)
            {
                var required = await _db.DocumentRequirements
                    .Where(r => r.TypeID == request.TypeID && r.IsActive && r.IsRequired)
                    .Select(r => r.RequirementID).ToListAsync();
                var verified = request.Documents
                    .Where(d => d.IsCurrent && d.VerificationStatus == VerificationStates.Verified)
                    .Select(d => d.RequirementID).ToHashSet();
                if (required.Any(id => !verified.Contains(id)))
                    return WorkflowResult.Fail("Verify every required document before moving the request to Processing.");
            }

            var oldName = await _db.Statuses.Where(s => s.StatusID == request.StatusID).Select(s => s.StatusName).FirstAsync();
            var newName = await _db.Statuses.Where(s => s.StatusID == newStatusId).Select(s => s.StatusName).FirstAsync();

            ApplyStatus(request, newStatusId, adminId, remarks,
                $"Status changed from {oldName} to {newName}.", notifyFamily: true);

            await _db.SaveChangesAsync();
            return WorkflowResult.Success();
        }

        /// <summary>Applies a status change plus its history row, audit entry and notification (no save).</summary>
        private void ApplyStatus(DocumentRequest request, int newStatusId, int? actorId,
            string? remarks, string logText, bool notifyFamily)
        {
            var previous = request.StatusID;
            var now = DateTime.UtcNow;
            request.StatusID = newStatusId;
            request.LastUpdatedAt = now;
            if (!string.IsNullOrWhiteSpace(remarks)) request.LatestRemarks = remarks;
            if (newStatusId == StatusIds.Completed) request.DateReleased = now;

            _db.StatusHistories.Add(new StatusHistory
            {
                RequestID = request.RequestID,
                StatusID = newStatusId,
                PreviousStatusID = previous,
                ChangedByUserID = actorId,
                DateChanged = now,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks
            });

            _log.Add(actorId, "Status changed",
                logText + (string.IsNullOrWhiteSpace(remarks) ? "" : $" Remarks: {remarks}"),
                "Request", request.RequestID, request.RequestID);

            if (!notifyFamily) return;

            var (title, body) = newStatusId switch
            {
                StatusIds.UnderReview => ("Your request is under review",
                    "Our staff are now checking the documents you submitted."),
                StatusIds.Incomplete => ("Action needed: documents incomplete",
                    "Some requirements need to be corrected or submitted."),
                StatusIds.Processing => ("Your request is being processed",
                    "Your documents were verified and your request is now being processed."),
                StatusIds.Approved => ("Your request was approved",
                    "Your request passed review. It will be marked completed shortly."),
                StatusIds.Completed => ("Your request is completed",
                    "The workflow for this request is complete."),
                StatusIds.Rejected => ("Your request was rejected",
                    "We could not approve this request."),
                _ => ("Request status updated", "The status of your request changed.")
            };
            if (!string.IsNullOrWhiteSpace(remarks)) body += " Remarks: " + remarks;
            _notify.Add(request.UserID, title,
                $"Request {request.ReferenceNumber}: {body}", request.RequestID);
        }

        // ---------------------------------------------------------------
        // Timeline, remarks (admin)
        // ---------------------------------------------------------------
        public async Task<WorkflowResult> SetEstimateAsync(int requestId, DateOnly? date, string? remarks, int adminId)
        {
            var request = await _db.DocumentRequests.FirstOrDefaultAsync(r => r.RequestID == requestId);
            if (request == null) return WorkflowResult.Fail("Request not found.");
            if (StatusIds.IsClosed(request.StatusID))
                return WorkflowResult.Fail("This request is closed; its timeline can no longer be changed.");
            if (date.HasValue && date.Value < DateOnly.FromDateTime(request.DateFiled.AddHours(8)))
                return WorkflowResult.Fail("The estimated date cannot be earlier than the date the request was filed.");

            var old = request.EstimatedCompletionDate;
            if (old == date && string.IsNullOrWhiteSpace(remarks))
                return WorkflowResult.Fail("Nothing changed.");

            request.EstimatedCompletionDate = date;
            request.LastUpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(remarks)) request.LatestRemarks = remarks.Trim();

            var text = date.HasValue
                ? $"Estimated completion set to {Ph.Date(date.Value)} (was {(old.HasValue ? Ph.Date(old.Value) : "not set")})."
                : "Estimated completion date cleared.";
            _log.Add(adminId, "Timeline updated",
                text + (string.IsNullOrWhiteSpace(remarks) ? "" : $" Remarks: {remarks.Trim()}"),
                "Request", requestId, requestId);

            _notify.Add(request.UserID, "Estimated timeline updated",
                $"Request {request.ReferenceNumber}: " +
                (date.HasValue ? $"the estimated completion date is now {Ph.Date(date.Value)}. This is an estimate, not a guaranteed date."
                               : "the estimated completion date was removed.") +
                (string.IsNullOrWhiteSpace(remarks) ? "" : " Remarks: " + remarks.Trim()), requestId);

            await _db.SaveChangesAsync();
            return WorkflowResult.Success();
        }

        public async Task<WorkflowResult> AddRemarkAsync(int requestId, string remarks, int adminId)
        {
            remarks = remarks?.Trim() ?? "";
            if (remarks.Length == 0) return WorkflowResult.Fail("Please write a remark.");

            var request = await _db.DocumentRequests.FirstOrDefaultAsync(r => r.RequestID == requestId);
            if (request == null) return WorkflowResult.Fail("Request not found.");

            request.LatestRemarks = remarks;
            request.LastUpdatedAt = DateTime.UtcNow;
            _log.Add(adminId, "Remark added", $"Remark: {remarks}", "Request", requestId, requestId);
            _notify.Add(request.UserID, "New remark on your request",
                $"Request {request.ReferenceNumber}: {remarks}", requestId);

            await _db.SaveChangesAsync();
            return WorkflowResult.Success();
        }

        // ---------------------------------------------------------------
        // Document verification (admin)
        // ---------------------------------------------------------------
        public async Task<WorkflowResult> VerifyDocumentAsync(int documentId, bool verified, string? remarks, int adminId)
        {
            var doc = await _db.UploadedDocuments
                .Include(d => d.Requirement)
                .Include(d => d.Request)
                .FirstOrDefaultAsync(d => d.DocumentID == documentId);
            if (doc == null || doc.Request == null) return WorkflowResult.Fail("Document not found.");
            if (!doc.IsCurrent) return WorkflowResult.Fail("This file was replaced by a newer upload.");
            if (StatusIds.IsClosed(doc.Request.StatusID))
                return WorkflowResult.Fail("This request is closed; documents can no longer be reviewed.");

            remarks = remarks?.Trim();
            if (!verified && string.IsNullOrWhiteSpace(remarks))
                return WorkflowResult.Fail("Please explain what needs to be corrected so the family knows what to do.");

            var now = DateTime.UtcNow;
            var name = doc.Requirement?.Name ?? "Document";
            doc.VerificationStatus = verified ? VerificationStates.Verified : VerificationStates.Incomplete;
            doc.VerificationRemarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks;
            doc.VerifiedByUserID = adminId;
            doc.VerifiedAt = now;
            doc.Request.LastUpdatedAt = now;

            _db.VerificationRecords.Add(new VerificationRecord
            {
                DocumentID = doc.DocumentID,
                RequestID = doc.RequestID,
                RequirementID = doc.RequirementID,
                Result = doc.VerificationStatus,
                Remarks = doc.VerificationRemarks,
                VerifiedByUserID = adminId,
                VerifiedAt = now
            });

            _log.Add(adminId, verified ? "Document verified" : "Document marked incomplete",
                $"\"{name}\" {(verified ? "verified" : "marked incomplete")}." +
                (doc.VerificationRemarks != null ? $" Remarks: {doc.VerificationRemarks}" : ""),
                "Document", doc.DocumentID, doc.RequestID);

            if (!verified)
            {
                _notify.Add(doc.Request.UserID, "Document needs correction",
                    $"Request {doc.Request.ReferenceNumber}: \"{name}\" needs to be corrected. {doc.VerificationRemarks}",
                    doc.RequestID);
            }

            // Reviewing a document means the request is under review.
            if (doc.Request.StatusID == StatusIds.Submitted)
                ApplyStatus(doc.Request, StatusIds.UnderReview, adminId, null,
                    "Status changed from Submitted to Under Review (document review started).", notifyFamily: true);

            await _db.SaveChangesAsync();
            return WorkflowResult.Success();
        }

        // ---------------------------------------------------------------
        // Helpers used by views
        // ---------------------------------------------------------------
        public static bool CanUpload(DocumentRequest r) => !StatusIds.IsClosed(r.StatusID);

        public static bool IsOverdue(DocumentRequest r) =>
            r.EstimatedCompletionDate.HasValue
            && !StatusIds.IsClosed(r.StatusID)
            && r.EstimatedCompletionDate.Value < Ph.Today;
    }
}
