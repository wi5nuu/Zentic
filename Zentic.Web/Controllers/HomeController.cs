using Zentic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Zentic.Web.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        private readonly IDashboardService _dashboardService;

        public HomeController(IDashboardService dashboardService) : base(dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login", "Auth");
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "Staff";
            var vm = await _dashboardService.GetDashboardAsync(userId, role);
            return View(vm);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();
    }
}
