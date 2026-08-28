using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Application.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace Zentic.Web.Controllers
{
    [Authorize]
    public class ExpenseController : BaseController
    {
        private readonly IExpenseService _expenseService;
        private readonly IVendorService _vendorService;
        private readonly IAuditService _auditService;

        public ExpenseController(IExpenseService expenseService, IVendorService vendorService, IAuditService auditService, IDashboardService dashboardService) 
            : base(dashboardService, auditService)
        {
            _expenseService = expenseService;
            _vendorService = vendorService;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(string? status, string? search, string? category, bool? flagged, int page = 1)
        {
            var userId = TryGetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");
            var role = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) ?? "Staff";

            var filter = new ExpenseFilterDto
            {
                Status = status,
                Search = search,
                Category = category,
                Flagged = flagged,
                Page = page,
                PageSize = 10
            };

            var result = await _expenseService.GetPagedAsync(filter, userId.Value, role);
            ViewBag.Filter = filter;
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = TryGetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");
            var role = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) ?? "Staff";

            var dto = await _expenseService.GetByIdAsync(id, userId.Value, role);
            if (dto == null) return NotFound();

            await LogAuditAsync(Domain.Enums.AuditEventType.View,
                "ExpenseRequisition", id.ToString(), "Viewed requisition details");

            await PopulateDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new CreateExpenseDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateExpenseDto model)
        {
            var validator = new CreateExpenseValidator();
            var validation = await validator.ValidateAsync(model);

            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

                await PopulateDropdowns();
                return View(model);
            }

            var userId = TryGetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            try
            {
                var result = await _expenseService.CreateAsync(model, userId.Value);
                TempData["Success"] = $"Requisition {result.RequisitionNumber} created.{(result.IsFlaggedForReview ? " ⚠ Flagged for review: " + result.FlagReason : "")}";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                await PopulateDropdowns();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            return await ExecuteAndRedirect(async userId =>
            {
                await _expenseService.SubmitAsync(id, userId);
                TempData["Success"] = "Requisition submitted for approval.";
            }, nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanApproveRequisition")]
        public async Task<IActionResult> Approve(int id, string? notes)
        {
            return await ExecuteAndRedirect(async userId =>
            {
                await _expenseService.ApproveAsync(id, userId, notes);
                TempData["Success"] = "Requisition approved.";
            }, nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanApproveRequisition")]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Rejection reason is required.";
                return RedirectToAction(nameof(Details), new { id });
            }
            return await ExecuteAndRedirect(async userId =>
            {
                await _expenseService.RejectAsync(id, userId, reason);
                TempData["Success"] = "Requisition rejected.";
            }, nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            return await ExecuteAndRedirect(async userId =>
            {
                await _expenseService.CancelAsync(id, userId);
                TempData["Success"] = "Requisition cancelled.";
            }, nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanApproveRequisition")]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            return await ExecuteAndRedirect(async userId =>
            {
                await _expenseService.MarkAsPaidAsync(id, userId);
                TempData["Success"] = "Requisition marked as paid.";
            }, nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanApproveRequisition")]
        public async Task<IActionResult> AddQuotation(AddQuotationDto dto)
        {
            return await ExecuteAndRedirect(async userId =>
            {
                await _expenseService.AddQuotationAsync(dto, userId);
                TempData["Success"] = "Quotation added.";
            }, nameof(Details), new { id = dto.RequisitionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanApproveRequisition")]
        public async Task<IActionResult> RemoveQuotation(int requisitionId, int quotationId)
        {
            return await ExecuteAndRedirect(async userId =>
            {
                await _expenseService.RemoveQuotationAsync(quotationId, userId);
                TempData["Success"] = "Quotation removed.";
            }, nameof(Details), new { id = requisitionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanApproveRequisition")]
        public async Task<IActionResult> SelectQuotation(int requisitionId, int quotationId, string? reason)
        {
            return await ExecuteAndRedirect(async userId =>
            {
                await _expenseService.SelectQuotationAsync(requisitionId, quotationId, reason, userId);
                TempData["Success"] = "Quotation selected.";
            }, nameof(Details), new { id = requisitionId });
        }

        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> Export(string? status, string? search, string? category, bool? flagged)
        {
            var userId = TryGetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");
            var role = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) ?? "Staff";

            var filter = new ExpenseFilterDto
            {
                Status = status,
                Search = search,
                Category = category,
                Flagged = flagged,
                Page = 1,
                PageSize = 2000
            };

            var result = await _expenseService.GetPagedAsync(filter, userId.Value, role);
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("\uFEFFRequisitionNumber,Title,Amount,Category,Status,Requester,Department,Vendor,CreatedAt,SubmittedAt,ApprovedAt,PaidAt,Flagged");
            foreach (var r in result.Items)
            {
                csv.AppendLine(string.Join(",",
                    Csv(r.RequisitionNumber), Csv(r.Title), r.Amount.ToString("0.00"),
                    Csv(r.Category), Csv(r.Status), Csv(r.RequesterName), Csv(r.DepartmentName),
                    Csv(r.VendorName), r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    r.SubmittedAt?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    r.ApprovedAt?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    r.PaidAt?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    r.IsFlaggedForReview ? "Yes" : "No"));
            }

            await LogAuditAsync(Domain.Enums.AuditEventType.Export, nameof(Domain.Entities.ExpenseRequisition),
                "CSV", $"Exported {result.Items.Count} requisition records");

            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv",
                $"requisitions_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        private async Task<IActionResult> ExecuteAndRedirect(Func<int, Task> action, string actionName, object? routeValues = null)
        {
            var userId = TryGetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");
            try
            {
                await action(userId.Value);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(actionName, routeValues);
        }

        private static string Csv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            var sanitized = value.Replace("\"", "\"\"");
            // Replace newlines with spaces to avoid breaking CSV rows
            sanitized = sanitized.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
            // Prevent CSV formula injection: if value starts with =, +, -, @, tab, prepend '
            if (sanitized.Length > 0 && sanitized[0] is '=' or '+' or '-' or '@' or '\t')
                sanitized = "'" + sanitized;
            return "\"" + sanitized + "\"";
        }

        private async Task PopulateDropdowns()
        {
            var vendors = await _vendorService.GetActiveVendorsAsync();
            ViewBag.Vendors = vendors.Select(v => new SelectListItem(v.Name, v.Id.ToString()));
            ViewBag.Categories = Enum.GetNames<Domain.Enums.ExpenseCategory>()
                .Select(c => new SelectListItem(c, c));
        }
    }
}
