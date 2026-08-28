using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Zentic.Web.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class VendorController : BaseController
    {
        private readonly IVendorService _vendorService;

        public VendorController(IVendorService vendorService, IAuditService auditService, IDashboardService dashboardService)
            : base(dashboardService, auditService)
        {
            _vendorService = vendorService;
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

            await LogAuditAsync(AuditEventType.View, "Vendor", id.ToString(), "Viewed vendor details");
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
                await LogAuditAsync(AuditEventType.Create, "Vendor", vendor.Id.ToString(), $"Created vendor {vendor.Name}");
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
                await LogAuditAsync(AuditEventType.Update, "Vendor", id.ToString(), $"Updated vendor {vendor.Name}");
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
        public async Task<IActionResult> Verify(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _vendorService.VerifyAsync(id, userId);
                await LogAuditAsync(AuditEventType.Approve, "Vendor", id.ToString(), "Vendor verified and set to Active");
                TempData["Success"] = "Vendor verified and activated.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _vendorService.SuspendAsync(id, userId);
                await LogAuditAsync(AuditEventType.Update, "Vendor", id.ToString(), "Vendor suspended/unsuspended");
                TempData["Success"] = "Vendor status updated.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
