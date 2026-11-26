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
    [Authorize(Roles = AppRoles.Family)]
    public class RequestsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly RequestWorkflowService _workflow;
        private readonly IFileStorage _files;

        public RequestsController(AppDbContext db, RequestWorkflowService workflow, IFileStorage files)
        {
            _db = db; _workflow = workflow; _files = files;
        }

        private int UserId => User.GetUserId()!.Value;

        private IQueryable<DocumentRequest> Mine => _db.DocumentRequests.AsNoTracking().Where(r => r.UserID == UserId);

        // ---------- Document Requests (list) ----------
        public async Task<IActionResult> Index(string? q, int? status, int page = 1)
        {
            var query = Mine;
            if (status.HasValue) query = query.Where(r => r.StatusID == status);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(r => r.ReferenceNumber.ToLower().Contains(term)
                    || (r.Deceased!.FirstName + " " + r.Deceased.LastName).ToLower().Contains(term));
            }

            var paged = await PagedResult<RequestListItem>.CreateAsync(
                RequestQueries.ProjectList(_db, query.OrderByDescending(r => r.DateFiled).ThenByDescending(r => r.RequestID)), page, 10);

            ViewData["Statuses"] = await _db.Statuses.AsNoTracking().ToListAsync();
            ViewData["Query"] = q;
            ViewData["StatusFilter"] = status;
            ViewData["HasDeceased"] = await _db.Deceased.AnyAsync(d => d.UserID == UserId);
            return View(paged);
        }

        // ---------- Track Requests ----------
        public async Task<IActionResult> Track(string? filter)
        {
            filter = (filter ?? "all").ToLowerInvariant();
            var query = Mine;
            query = filter switch
            {
                "pending" => query.Where(r => r.StatusID == StatusIds.Submitted || r.StatusID == StatusIds.UnderReview || r.StatusID == StatusIds.Incomplete),
                "processing" => query.Where(r => r.StatusID == StatusIds.Processing || r.StatusID == StatusIds.Approved),
                "completed" => query.Where(r => r.StatusID == StatusIds.Completed),
                "rejected" => query.Where(r => r.StatusID == StatusIds.Rejected),
                _ => query
            };

            var items = await RequestQueries.ProjectList(_db, query.OrderByDescending(r => r.LastUpdatedAt)).ToListAsync();

            var rejectedIds = items.Where(i => i.StatusID == StatusIds.Rejected).Select(i => i.RequestID).ToList();
            var previous = rejectedIds.Count == 0 ? new Dictionary<int, int?>() :
                (await _db.StatusHistories.AsNoTracking()
                    .Where(h => rejectedIds.Contains(h.RequestID) && h.StatusID == StatusIds.Rejected)
                    .Select(h => new { h.RequestID, h.PreviousStatusID }).ToListAsync())
                .GroupBy(h => h.RequestID).ToDictionary(g => g.Key, g => g.First().PreviousStatusID);

            foreach (var i in items)
                i.Steps = RequestProgress.Build(i.StatusID, previous.GetValueOrDefault(i.RequestID));

            ViewData["Filter"] = filter;
            return View(items);
        }

        // ---------- New request ----------
        [HttpGet]
        public async Task<IActionResult> New(int? typeId, int? deceasedId)
        {
            var vm = await BuildNewAsync(new NewRequestViewModel { TypeID = typeId, DeceasedID = deceasedId });
            return View(vm);
        }

        [HttpPost]
        [RequestSizeLimit(62914560)]
        [RequestFormLimits(MultipartBodyLengthLimit = 62914560)]
        public async Task<IActionResult> New(NewRequestViewModel model)
        {
            if (!model.DeceasedID.HasValue) ModelState.AddModelError(nameof(model.DeceasedID), "Please choose a deceased-person record.");
            if (!model.TypeID.HasValue) ModelState.AddModelError(nameof(model.TypeID), "Please choose the document or service you need.");
            if (!model.Confirm) ModelState.AddModelError(nameof(model.Confirm), "Please confirm that the information and documents are accurate.");

            if (ModelState.IsValid)
            {
                var files = new Dictionary<int, IFormFile>();
                foreach (var f in Request.Form.Files.Where(f => f.Length > 0 && f.Name.StartsWith("file_")))
                    if (int.TryParse(f.Name["file_".Length..], out var reqId)) files[reqId] = f;

                var result = await _workflow.SubmitAsync(UserId, model.DeceasedID!.Value, model.TypeID!.Value, model.FamilyNotes, files);
                if (result.Ok)
                {
                    TempData["Success"] = "Your request was submitted. We will review your documents and keep you updated here.";
                    return RedirectToAction(nameof(Details), new { id = result.Id, submitted = true });
                }
                ModelState.AddModelError("", result.Error!);
            }

            return View(await BuildNewAsync(model));
        }

        private async Task<NewRequestViewModel> BuildNewAsync(NewRequestViewModel vm)
        {
            vm.Deceased = await _db.Deceased.AsNoTracking().Where(d => d.UserID == UserId)
                .OrderBy(d => d.FirstName)
                .Select(d => new DeceasedOption { DeceasedID = d.DeceasedID, Name = d.FirstName + " " + d.LastName })
                .ToListAsync();
            if (vm.Deceased.Count == 1 && !vm.DeceasedID.HasValue) vm.DeceasedID = vm.Deceased[0].DeceasedID;

            var types = await _db.RequestTypes.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync();
            var reqs = await _db.DocumentRequirements.AsNoTracking().Where(r => r.IsActive)
                .OrderBy(r => r.SortOrder).ToListAsync();
            vm.Types = types.Select(t => new RequestTypeOption
            {
                TypeID = t.TypeID, TypeName = t.TypeName, Description = t.Description,
                Requirements = reqs.Where(r => r.TypeID == t.TypeID).ToList()
            }).ToList();
            return vm;
        }

        // ---------- Details ----------
        public async Task<IActionResult> Details(int id, bool submitted = false)
        {
            var vm = await RequestQueries.LoadDetailsAsync(_db, id, UserId, forAdmin: false);
            if (vm == null) return NotFound();
            ViewData["JustSubmitted"] = submitted;
            return View(vm);
        }

        // ---------- Upload / replace a document ----------
        [HttpPost]
        [RequestSizeLimit(10485760)]
        public async Task<IActionResult> Upload(int id, int requirementId, IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please choose a file to upload.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _workflow.UploadDocumentAsync(UserId, id, requirementId, file);
            if (result.Ok) TempData["Success"] = "Your document was uploaded.";
            else TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Details), new { id });
        }

        // ---------- Secure file access (owner only) ----------
        public async Task<IActionResult> File(int id)
        {
            var doc = await _db.UploadedDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DocumentID == id && d.Request!.UserID == UserId);
            if (doc == null) return NotFound();

            var stream = _files.Open(doc.StoredFileName);
            if (stream == null) return NotFound();

            Response.Headers["Content-Disposition"] = $"inline; filename=\"{Uri.EscapeDataString(doc.OriginalFileName)}\"";
            return File(stream, doc.ContentType);
        }
    }
}
