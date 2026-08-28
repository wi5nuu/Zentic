using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Zentic.Domain.Entities;
using Zentic.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Zentic.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ApprovalWorkflowController : BaseController
    {
        private readonly IApprovalWorkflowService _workflowService;
        private readonly IRepository<AppUser> _userRepo;
        private readonly IRepository<Department> _deptRepo;

        public ApprovalWorkflowController(
            IApprovalWorkflowService workflowService,
            IRepository<AppUser> userRepo,
            IRepository<Department> deptRepo,
            IDashboardService dashboardService)
            : base(dashboardService)
        {
            _workflowService = workflowService;
            _userRepo = userRepo;
            _deptRepo = deptRepo;
        }

        public async Task<IActionResult> Index()
        {
            var workflows = await _workflowService.GetActiveWorkflowsAsync();
            return View(workflows);
        }

        public async Task<IActionResult> Details(int id)
        {
            var workflow = await _workflowService.GetWorkflowByIdAsync(id);
            if (workflow == null) return NotFound();
            return View(workflow);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new CreateApprovalWorkflowDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateApprovalWorkflowDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                ModelState.AddModelError("Name", "Workflow name is required.");
            if (!dto.Steps.Any())
                ModelState.AddModelError("Steps", "At least one approval step is required.");

            ValidateSteps(dto.Steps);

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(dto);
            }

            var userId = GetCurrentUserId();
            var created = await _workflowService.CreateWorkflowAsync(dto, userId);

            TempData["Success"] = $"Workflow '{created.Name}' created.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var workflow = await _workflowService.GetWorkflowByIdAsync(id);
            if (workflow == null) return NotFound();

            var dto = new CreateApprovalWorkflowDto
            {
                Name = workflow.Name,
                Description = workflow.Description,
                DepartmentId = workflow.DepartmentId,
                Category = Enum.TryParse<ExpenseCategory>(workflow.Category, out var cat) ? cat : null,
                MinAmount = workflow.MinAmount,
                MaxAmount = workflow.MaxAmount,
                Priority = workflow.Priority,
                Steps = workflow.Steps.OrderBy(s => s.StepOrder).Select(s => new CreateApprovalStepDto
                {
                    Name = s.Name,
                    Description = s.Description,
                    ApproverType = Enum.TryParse<ApproverType>(s.ApproverType, out var at) ? at : ApproverType.SpecificUser,
                    SpecificUserId = s.SpecificUserId,
                    RequiredRole = Enum.TryParse<UserRole>(s.RequiredRole, out var ur) ? ur : null,
                    DepartmentId = s.DepartmentId,
                    AllowDelegation = s.AllowDelegation,
                    RequireAllApprovers = s.RequireAllApprovers,
                    EscalationRuleId = s.EscalationRuleId,
                    IsParallel = s.IsParallel
                }).ToList()
            };

            await PopulateDropdowns();
            return View(dto);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateApprovalWorkflowDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                ModelState.AddModelError("Name", "Workflow name is required.");
            if (!dto.Steps.Any())
                ModelState.AddModelError("Steps", "At least one approval step is required.");

            ValidateSteps(dto.Steps);

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(dto);
            }

            var userId = GetCurrentUserId();
            var updated = await _workflowService.UpdateWorkflowAsync(id, dto, userId);

            TempData["Success"] = $"Workflow '{updated.Name}' updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _workflowService.DeleteWorkflowAsync(id, GetCurrentUserId());

            TempData[deleted ? "Success" : "Error"] = deleted ? "Workflow deleted." : "Workflow not found.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EscalationRules()
        {
            var rules = await _workflowService.GetActiveEscalationRulesAsync();
            return View(rules);
        }

        public async Task<IActionResult> CreateEscalationRule()
        {
            await PopulateDropdowns();
            return View(new CreateEscalationRuleDto());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEscalationRule(CreateEscalationRuleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                ModelState.AddModelError("Name", "Rule name is required.");
            if (dto.EscalateToUserId == null && dto.EscalateToRole == null)
                ModelState.AddModelError("EscalateToUserId", "Specify an escalation target user or role.");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(dto);
            }

            var created = await _workflowService.CreateEscalationRuleAsync(dto, GetCurrentUserId());

            TempData["Success"] = $"Escalation rule '{created.Name}' created.";
            return RedirectToAction(nameof(EscalationRules));
        }

        private void ValidateSteps(List<CreateApprovalStepDto> steps)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                if (string.IsNullOrWhiteSpace(step.Name))
                    ModelState.AddModelError($"Steps[{i}].Name", $"Step {i + 1}: name is required.");

                switch (step.ApproverType)
                {
                    case ApproverType.SpecificUser when step.SpecificUserId == null:
                        ModelState.AddModelError($"Steps[{i}].SpecificUserId", $"Step {i + 1}: select a specific approver.");
                        break;
                    case ApproverType.Role when step.RequiredRole == null:
                        ModelState.AddModelError($"Steps[{i}].RequiredRole", $"Step {i + 1}: select a required role.");
                        break;
                    case ApproverType.DepartmentHead when step.DepartmentId == null:
                        ModelState.AddModelError($"Steps[{i}].DepartmentId", $"Step {i + 1}: select a department.");
                        break;
                }
            }
        }

        private async Task PopulateDropdowns()
        {
            var users = (await _userRepo.GetAllAsync())
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .ToList();
            ViewBag.Users = new SelectList(users, "Id", "FullName");
            ViewBag.Departments = new SelectList(await _deptRepo.GetAllAsync(), "Id", "Name");
            ViewBag.Roles = Enum.GetValues<UserRole>()
                .Select(r => new SelectListItem(r.ToString(), ((int)r).ToString()));
            ViewBag.Categories = Enum.GetValues<ExpenseCategory>()
                .Select(c => new SelectListItem(c.ToString(), ((int)c).ToString()));
            ViewBag.EscalationRules = (await _workflowService.GetActiveEscalationRulesAsync())
                .Select(r => new SelectListItem(r.Name, r.Id.ToString()))
                .ToList();
        }
    }
}
