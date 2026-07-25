using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Zentic.Web.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class VendorController : BaseController
    {
        private readonly IVendorService _vendorService;
        private readonly IAuditService _auditService;
        private readonly IDashboardService _dashboardService;

        public VendorController(IVendorService vendorService, IAuditService auditService, IDashboardService dashboardService)
            : base(dashboardService)
        {
            _vendorService = vendorService;
            _auditService = auditService;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var vendors = await _vendorService.GetAllVendorsAsync();
            return View(vendors);
        }

        public async Task<IActionResult> Details(int id)
        {
            var vendor = await _vendorService.GetByIdAsync(id);
            if (vendor == null) return NotFound();

            await LogAudit(AuditEventType.View, "Vendor", id.ToString(), "Viewed vendor details");
            return View(vendor);
        }

        public IActionResult Create() => View(new CreateVendorDto());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateVendorDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            try
            {
                var userId = GetCurrentUserId();
                var vendor = await _vendorService.CreateAsync(dto, userId);
                await LogAudit(AuditEventType.Create, "Vendor", vendor.Id.ToString(), $"Created vendor {vendor.Name}");
                TempData["Success"] = $"Vendor {vendor.Name} created and pending verification.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var vendor = await _vendorService.GetByIdAsync(id);
            if (vendor == null) return NotFound();

            return View(new CreateVendorDto
            {
                Name = vendor.Name,
                Code = vendor.Code,
                Address = vendor.Address,
                PhoneNumber = vendor.PhoneNumber,
                Email = vendor.Email,
                TaxId = vendor.TaxId,
                BankName = vendor.BankName,
                BankAccountHolderName = vendor.BankAccountHolderName
                // BankAccountNumber intentionally blank — only update if user enters new value
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateVendorDto dto)
        {
            if (!ModelState.IsValid) return View(dto);
            try
            {
                var userId = GetCurrentUserId();
                var vendor = await _vendorService.UpdateAsync(id, dto, userId);
                await LogAudit(AuditEventType.Update, "Vendor", id.ToString(), $"Updated vendor {vendor.Name}");
                TempData["Success"] = $"Vendor {vendor.Name} updated.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(dto);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> Verify(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _vendorService.VerifyAsync(id, userId);
                await LogAudit(AuditEventType.Approve, "Vendor", id.ToString(), "Vendor verified and set to Active");
                TempData["Success"] = "Vendor verified and activated.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> Suspend(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _vendorService.SuspendAsync(id, userId);
                await LogAudit(AuditEventType.Update, "Vendor", id.ToString(), "Vendor suspended/unsuspended");
                TempData["Success"] = "Vendor status updated.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        private int GetCurrentUserId()
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
                return id;
            throw new InvalidOperationException("User not authenticated.");
        }

        private async Task LogAudit(AuditEventType eventType, string entity, string entityId, string notes)
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditService.LogAsync(eventType, entity, entityId, userId, ip, null, null, notes);
            }
        }
    }
}
