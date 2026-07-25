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
        private readonly IDashboardService _dashboardService;

        public ExpenseController(IExpenseService expenseService, IVendorService vendorService, IAuditService auditService, IDashboardService dashboardService) 
            : base(dashboardService)
        {
            _expenseService = expenseService;
            _vendorService = vendorService;
            _auditService = auditService;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(string? status, string? search, string? category, int page = 1)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Auth");
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Staff";

            var filter = new ExpenseFilterDto
            {
                Status = status,
                Search = search,
                Category = category,
                Page = page,
                PageSize = 10
            };

            var result = await _expenseService.GetPagedAsync(filter, userId, role);
            ViewBag.Filter = filter;
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Auth");
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Staff";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var dto = await _expenseService.GetByIdAsync(id, userId, role);
            if (dto == null) return NotFound();

            // Audit: log view of requisition details
            await _auditService.LogAsync(Domain.Enums.AuditEventType.View,
                "ExpenseRequisition", id.ToString(), userId, ip, null, null, "Viewed requisition details", id);

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

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Auth");

            try
            {
                var result = await _expenseService.CreateAsync(model, userId);
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
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Auth");
            try
            {
                await _expenseService.SubmitAsync(id, userId);
                TempData["Success"] = "Requisition submitted for approval.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanApproveRequisition")]
        public async Task<IActionResult> Approve(int id, string? notes)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var approverId))
                return RedirectToAction("Login", "Auth");
            try
            {
                await _expenseService.ApproveAsync(id, approverId, notes);
                TempData["Success"] = "Requisition approved.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
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
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var approverId))
                return RedirectToAction("Login", "Auth");
            try
            {
                await _expenseService.RejectAsync(id, approverId, reason);
                TempData["Success"] = "Requisition rejected.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Auth");
            try
            {
                await _expenseService.CancelAsync(id, userId);
                TempData["Success"] = "Requisition cancelled.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
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
