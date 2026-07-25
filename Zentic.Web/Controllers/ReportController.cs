using Zentic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Zentic.Web.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class ReportController : BaseController
    {
        private readonly IExpenseService _expenseService;
        private readonly IDashboardService _dashboardService;

        public ReportController(IExpenseService expenseService, IDashboardService dashboardService)
            : base(dashboardService)
        {
            _expenseService = expenseService;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Auth");

            // BaseController.OnActionExecutionAsync already called GetDashboardAsync once to
            // populate ViewBag.PendingCount / ViewBag.FlaggedCount for the sidebar.
            // We need the full DashboardDto for the view model, so we call it once here.
            // A single call per request is acceptable; caching can be added later if needed.
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Staff";
            var dashboard = await _dashboardService.GetDashboardAsync(userId, role);
            return View(dashboard);
        }
    }
}
