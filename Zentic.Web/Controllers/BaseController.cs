using Zentic.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Zentic.Web.Controllers
{
    public class BaseController : Controller
    {
        private readonly IDashboardService? _dashboardService;
        private static readonly TimeSpan DashboardCacheDuration = TimeSpan.FromMinutes(5);
        private static DateTime _lastDashboardFetch = DateTime.MinValue;
        private static int _cachedUserId = 0;
        private static string _cachedRole = "";
        private static int _cachedPendingCount = 0;
        private static int _cachedFlaggedCount = 0;
        private static readonly object _cacheLock = new();

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
                        int pendingCount = 0, flaggedCount = 0;
                        var now = DateTime.UtcNow;
                        bool useCache;
                        
                        lock (_cacheLock)
                        {
                            useCache = _cachedUserId == userId && 
                                       _cachedRole == role && 
                                       now - _lastDashboardFetch < DashboardCacheDuration;
                            if (useCache)
                            {
                                pendingCount = _cachedPendingCount;
                                flaggedCount = _cachedFlaggedCount;
                            }
                        }

                        if (!useCache)
                        {
                            try
                            {
                                var dashboard = await _dashboardService.GetDashboardAsync(userId, role);
                                pendingCount = dashboard.PendingApproval;
                                flaggedCount = dashboard.FlaggedForReview;
                            }
                            catch
                            {
                                // Use cached values on error
                                lock (_cacheLock)
                                {
                                    pendingCount = _cachedPendingCount;
                                    flaggedCount = _cachedFlaggedCount;
                                }
                            }
                            
                            lock (_cacheLock)
                            {
                                _cachedUserId = userId;
                                _cachedRole = role;
                                _cachedPendingCount = pendingCount;
                                _cachedFlaggedCount = flaggedCount;
                                _lastDashboardFetch = now;
                            }
                        }

                        ViewBag.PendingCount = pendingCount;
                        ViewBag.FlaggedCount = flaggedCount;
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
