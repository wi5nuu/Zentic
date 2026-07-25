using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace Zentic.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : BaseController
    {
        private readonly IRepository<AppUser> _userRepo;
        private readonly IRepository<Department> _deptRepo;
        private readonly IAuditService _auditService;
        private readonly IDashboardService _dashboardService;

        public UserController(
            IRepository<AppUser> userRepo,
            IRepository<Department> deptRepo,
            IAuditService auditService,
            IDashboardService dashboardService)
            : base(dashboardService)
        {
            _userRepo = userRepo;
            _deptRepo = deptRepo;
            _auditService = auditService;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userRepo.GetAllAsync();
            return View(users);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDepartments();
            ViewBag.Roles = Enum.GetValues<UserRole>()
                .Select(r => new SelectListItem(r.ToString(), r.ToString()));
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Email,EmployeeId,Position,Role,DepartmentId")] AppUser model, string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                ModelState.AddModelError("password", "Password must be at least 8 characters.");

            // Check unique email / employee ID
            var all = await _userRepo.GetAllAsync();
            if (all.Any(u => u.Email == model.Email))
                ModelState.AddModelError("Email", "Email already in use.");
            if (all.Any(u => u.EmployeeId == model.EmployeeId))
                ModelState.AddModelError("EmployeeId", "Employee ID already in use.");

            if (!ModelState.IsValid)
            {
                await PopulateDepartments(model.DepartmentId);
                ViewBag.Roles = Enum.GetValues<UserRole>()
                    .Select(r => new SelectListItem(r.ToString(), r.ToString(), r == model.Role));
                return View(model);
            }

            model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            model.IsActive = true;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            var created = await _userRepo.AddAsync(model);
            await LogAudit(AuditEventType.Create, "AppUser", created.Id.ToString(), $"Created user {created.Email}");

            TempData["Success"] = $"User {created.FullName} created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            await PopulateDepartments(user.DepartmentId);
            ViewBag.Roles = Enum.GetValues<UserRole>()
                .Select(r => new SelectListItem(r.ToString(), r.ToString(), r == user.Role));
            return View(user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FullName,Email,EmployeeId,Position,Role,DepartmentId,IsActive")] AppUser model, string? newPassword)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            var all = await _userRepo.GetAllAsync();
            if (all.Any(u => u.Email == model.Email && u.Id != id))
                ModelState.AddModelError("Email", "Email already in use.");
            if (all.Any(u => u.EmployeeId == model.EmployeeId && u.Id != id))
                ModelState.AddModelError("EmployeeId", "Employee ID already in use.");

            if (!ModelState.IsValid)
            {
                await PopulateDepartments(model.DepartmentId);
                ViewBag.Roles = Enum.GetValues<UserRole>()
                    .Select(r => new SelectListItem(r.ToString(), r.ToString(), r == model.Role));
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.EmployeeId = model.EmployeeId;
            user.Position = model.Position;
            user.Role = model.Role;
            user.DepartmentId = model.DepartmentId;
            user.IsActive = model.IsActive;

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword.Length < 8)
                {
                    ModelState.AddModelError("newPassword", "Password must be at least 8 characters.");
                    await PopulateDepartments(model.DepartmentId);
                    ViewBag.Roles = Enum.GetValues<UserRole>()
                        .Select(r => new SelectListItem(r.ToString(), r.ToString(), r == model.Role));
                    return View(model);
                }
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }

            await _userRepo.UpdateAsync(user);
            await LogAudit(AuditEventType.Update, "AppUser", id.ToString(), $"Updated user {user.Email}");

            TempData["Success"] = $"User {user.FullName} updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            // Prevent admin from deactivating themselves
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId) && currentUserId == id)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _userRepo.UpdateAsync(user);
            await LogAudit(AuditEventType.Update, "AppUser", id.ToString(),
                $"User {user.Email} {(user.IsActive ? "activated" : "deactivated")}");

            TempData["Success"] = $"User {user.FullName} {(user.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId) && currentUserId == id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Index));
            }

            await _userRepo.SoftDeleteAsync(id);
            await LogAudit(AuditEventType.Delete, "AppUser", id.ToString(), $"Deleted user {user.Email}");

            TempData["Success"] = $"User {user.FullName} deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDepartments(int? selectedId = null)
        {
            var depts = await _deptRepo.GetAllAsync();
            ViewBag.Departments = depts
                .Where(d => d.IsActive)
                .Select(d => new SelectListItem(d.Name, d.Id.ToString(), d.Id == selectedId));
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
