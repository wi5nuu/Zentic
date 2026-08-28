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
            var userId = GetCurrentUserId();
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Staff";
            var dashboard = await _dashboardService.GetDashboardAsync(userId, role);
            return View(dashboard);
        }
    }
}
