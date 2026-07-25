using Zentic.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Zentic.Web.Controllers
{
    public class BaseController : Controller
    {
        private readonly IDashboardService? _dashboardService;

        public BaseController(IDashboardService? dashboardService = null)
        {
            _dashboardService = dashboardService;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Inject sidebar badge counts for all authenticated requests
            if (User.Identity?.IsAuthenticated == true && _dashboardService != null)
            {
                try
                {
                    var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var role = User.FindFirstValue(ClaimTypes.Role) ?? "Staff";
                    if (int.TryParse(userIdStr, out var userId))
                    {
                        var dashboard = await _dashboardService.GetDashboardAsync(userId, role);
                        ViewBag.PendingCount = dashboard.PendingApproval;
                        ViewBag.FlaggedCount = dashboard.FlaggedForReview;
                    }
                    else
                    {
                        ViewBag.PendingCount = 0;
                        ViewBag.FlaggedCount = 0;
                    }
                }
                catch
                {
                    // Fallback if dashboard service fails
                    ViewBag.PendingCount = 0;
                    ViewBag.FlaggedCount = 0;
                }
            }

            await next();
        }
    }
}
