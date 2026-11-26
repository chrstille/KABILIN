using KABILIN.Data;
using KABILIN.Helpers;
using KABILIN.Models;
using KABILIN.Services;
using KABILIN.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KABILIN.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    [Route("Admin/Requests")]
    public class AdminRequestsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly RequestWorkflowService _workflow;
        private readonly IFileStorage _files;

        public AdminRequestsController(AppDbContext db, RequestWorkflowService workflow, IFileStorage files)
        {
            _db = db; _workflow = workflow; _files = files;
        }

        private int AdminId => User.GetUserId()!.Value;

        public static IQueryable<AdminRequestRow> ProjectRows(IQueryable<DocumentRequest> source) =>
            source.Select(r => new AdminRequestRow
            {
                RequestID = r.RequestID,
                ReferenceNumber = r.ReferenceNumber,
                FamilyName = r.User!.FirstName + " " + r.User.LastName,
                DeceasedName = r.Deceased!.FirstName + " " + r.Deceased.LastName,
                TypeName = r.Type!.TypeName,
                DateFiled = r.DateFiled,
                StatusID = r.StatusID,
                StatusName = r.Status!.StatusName,
                EstimatedCompletionDate = r.EstimatedCompletionDate
            });

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] AdminRequestFilter f, int page = 1)
        {
            var query = _db.DocumentRequests.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(f.Q))
            {
                var term = f.Q.Trim().ToLower();
                query = query.Where(r => r.ReferenceNumber.ToLower().Contains(term)
                    || (r.User!.FirstName + " " + r.User.LastName).ToLower().Contains(term)
                    || r.User.EmailAddress.ToLower().Contains(term)
                    || (r.Deceased!.FirstName + " " + r.Deceased.LastName).ToLower().Contains(term));
            }
            if (f.Status.HasValue) query = query.Where(r => r.StatusID == f.Status);
            if (f.Type.HasValue) query = query.Where(r => r.TypeID == f.Type);
            if (f.From.HasValue)
            {
                var from = DateTime.SpecifyKind(f.From.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc).AddHours(-8);
                query = query.Where(r => r.DateFiled >= from);
            }
            if (f.To.HasValue)
            {
                var to = DateTime.SpecifyKind(f.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc).AddHours(-8);
                query = query.Where(r => r.DateFiled < to);
            }
            if (f.Overdue)
            {
                var today = Ph.Today;
                query = query.Where(r => r.EstimatedCompletionDate != null && r.EstimatedCompletionDate < today
                    && r.StatusID != StatusIds.Completed && r.StatusID != StatusIds.Rejected);
            }

            ViewData["Filter"] = f;
            ViewData["Statuses"] = await _db.Statuses.AsNoTracking().ToListAsync();
            ViewData["Types"] = await _db.RequestTypes.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync();
            return View(await PagedResult<AdminRequestRow>.CreateAsync(
                ProjectRows(query.OrderByDescending(r => r.DateFiled).ThenByDescending(r => r.RequestID)), page, 10));
        }

        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var vm = await RequestQueries.LoadDetailsAsync(_db, id, null, forAdmin: true);
            if (vm == null) return NotFound();
            vm.Verifications = await _db.VerificationRecords.AsNoTracking()
                .Where(v => v.RequestID == id).Include(v => v.VerifiedBy).Include(v => v.Document)
                .OrderByDescending(v => v.VerifiedAt).ToListAsync();
            return View(vm);
        }

        [HttpPost("UpdateStatus/{id:int}")]
        public async Task<IActionResult> UpdateStatus(int id, int statusId, string? remarks)
        {
            var result = await _workflow.ChangeStatusAsync(id, statusId, remarks, AdminId);
            if (result.Ok) TempData["Success"] = "The status was updated and the family was notified.";
            else TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost("Verify/{id:int}")]
        public async Task<IActionResult> Verify(int id, int documentId, string result, string? remarks)
        {
            var verified = result == "verify";
            var outcome = await _workflow.VerifyDocumentAsync(documentId, verified, remarks, AdminId);
            if (outcome.Ok) TempData["Success"] = verified ? "The document was marked as verified." : "The document was marked incomplete and the family was notified.";
            else TempData["Error"] = outcome.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost("Timeline/{id:int}")]
        public async Task<IActionResult> Timeline(int id, DateOnly? estimatedDate, string? remarks)
        {
            var result = await _workflow.SetEstimateAsync(id, estimatedDate, remarks, AdminId);
            if (result.Ok) TempData["Success"] = "The estimated timeline was saved and the family was notified.";
            else TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost("Remark/{id:int}")]
        public async Task<IActionResult> Remark(int id, string remarks)
        {
            var result = await _workflow.AddRemarkAsync(id, remarks, AdminId);
            if (result.Ok) TempData["Success"] = "Your remark was added and the family was notified.";
            else TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet("File/{id:int}")]
        public async Task<IActionResult> File(int id)
        {
            var doc = await _db.UploadedDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.DocumentID == id);
            if (doc == null) return NotFound();
            var stream = _files.Open(doc.StoredFileName);
            if (stream == null) return NotFound();
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{Uri.EscapeDataString(doc.OriginalFileName)}\"";
            return File(stream, doc.ContentType);
        }
    }
}
