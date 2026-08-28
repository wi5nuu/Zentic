using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Zentic.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentController : BaseController
    {
        private readonly IRepository<Department> _deptRepo;

        public DepartmentController(
            IRepository<Department> deptRepo,
            IAuditService auditService,
            IDashboardService dashboardService)
            : base(dashboardService, auditService)
        {
            _deptRepo = deptRepo;
        }

        public async Task<IActionResult> Index()
        {
            var departments = await _deptRepo.GetAllAsync();
            return View(departments);
        }

        public IActionResult Create() => View(new Department());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Code,Description")] Department model)
        {
            var all = await _deptRepo.GetAllAsync();
            if (all.Any(d => d.Code == model.Code))
                ModelState.AddModelError("Code", "Department code already in use.");
            if (all.Any(d => d.Name == model.Name))
                ModelState.AddModelError("Name", "Department name already in use.");

            if (!ModelState.IsValid) return View(model);

            model.IsActive = true;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            var created = await _deptRepo.AddAsync(model);
            await LogAuditAsync(AuditEventType.Create, "Department", created.Id.ToString(), $"Created department {created.Name}");

            TempData["Success"] = $"Department {created.Name} created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var dept = await _deptRepo.GetByIdAsync(id);
            if (dept == null) return NotFound();
            return View(dept);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Code,Description,IsActive")] Department model)
        {
            var dept = await _deptRepo.GetByIdAsync(id);
            if (dept == null) return NotFound();

            var all = await _deptRepo.GetAllAsync();
            if (all.Any(d => d.Code == model.Code && d.Id != id))
                ModelState.AddModelError("Code", "Department code already in use.");
            if (all.Any(d => d.Name == model.Name && d.Id != id))
                ModelState.AddModelError("Name", "Department name already in use.");

            if (!ModelState.IsValid) return View(model);

            dept.Name = model.Name;
            dept.Code = model.Code;
            dept.Description = model.Description;
            dept.IsActive = model.IsActive;

            await _deptRepo.UpdateAsync(dept);
            await LogAuditAsync(AuditEventType.Update, "Department", id.ToString(), $"Updated department {dept.Name}");

            TempData["Success"] = $"Department {dept.Name} updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var dept = await _deptRepo.GetByIdAsync(id);
            if (dept == null) return NotFound();

            await _deptRepo.SoftDeleteAsync(id);
            await LogAuditAsync(AuditEventType.Delete, "Department", id.ToString(), $"Deleted department {dept.Name}");

            TempData["Success"] = $"Department {dept.Name} deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
