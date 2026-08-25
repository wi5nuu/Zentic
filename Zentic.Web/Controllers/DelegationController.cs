using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace Zentic.Web.Controllers
{
    [Authorize]
    public class DelegationController : BaseController
    {
        private readonly IApprovalWorkflowService _workflowService;
        private readonly IRepository<AppUser> _userRepo;
        private readonly IRepository<Department> _deptRepo;
        private readonly IDashboardService _dashboardService;

        public DelegationController(
            IApprovalWorkflowService workflowService,
            IRepository<AppUser> userRepo,
            IRepository<Department> deptRepo,
            IDashboardService dashboardService)
            : base(dashboardService)
        {
            _workflowService = workflowService;
            _userRepo = userRepo;
            _deptRepo = deptRepo;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId();
            var model = (
                Incoming: await _workflowService.GetActiveDelegationsAsync(userId),
                Outgoing: await _workflowService.GetDelegationsByDelegatorAsync(userId)
            );
            return View(model);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new CreateApprovalDelegationDto
            {
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(7)
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateApprovalDelegationDto dto)
        {
            if (dto.DelegateId == 0)
                ModelState.AddModelError("DelegateId", "Select a delegate.");
            if (dto.EndDate < dto.StartDate)
                ModelState.AddModelError("EndDate", "End date must be on or after start date.");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(dto);
            }

            try
            {
                var userId = CurrentUserId();
                if (dto.DelegateId == userId)
                    throw new InvalidOperationException("You cannot delegate approvals to yourself.");

                await _workflowService.CreateDelegationAsync(dto, userId);

                TempData["Success"] = "Delegation created.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Create));
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(int id)
        {
            var revoked = await _workflowService.RevokeDelegationAsync(id, CurrentUserId());
            TempData[revoked ? "Success" : "Error"] = revoked ? "Delegation revoked." : "Delegation not found.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdowns()
        {
            var users = (await _userRepo.GetAllAsync())
                .Where(u => u.IsActive && u.Id != CurrentUserId())
                .OrderBy(u => u.FullName)
                .ToList();
            ViewBag.Users = new SelectList(users, "Id", "FullName");
            ViewBag.Departments = new SelectList(await _deptRepo.GetAllAsync(), "Id", "Name");
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
