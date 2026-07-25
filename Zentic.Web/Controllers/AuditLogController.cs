using Zentic.Application.DTOs;
using Zentic.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Zentic.Web.Controllers
{
    [Authorize(Roles = "Admin,Finance,Auditor")]
    public class AuditLogController : BaseController
    {
        private readonly IAuditService _auditService;
        private readonly IDashboardService _dashboardService;

        public AuditLogController(IAuditService auditService, IDashboardService dashboardService)
            : base(dashboardService)
        {
            _auditService = auditService;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index(string? entityName, int? userId, 
            DateTime? from, DateTime? to, int page = 1)
        {
            var filter = new AuditFilterDto
            {
                EntityName = entityName,
                UserId = userId,
                From = from,
                To = to,
                Page = page,
                PageSize = 20
            };

            var result = await _auditService.GetPagedAsync(filter);
            ViewBag.Filter = filter;
            return View(result);
        }
    }
}
