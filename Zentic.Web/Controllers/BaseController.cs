using Zentic.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Zentic.Web.Controllers
{
    public class BaseController : Controller
    {
        private readonly IDashboardService? _dashboardService;
        private readonly IAuditService? _auditService;
        private static readonly TimeSpan DashboardCacheDuration = TimeSpan.FromMinutes(5);
        private static readonly ConcurrentDictionary<int, DashboardCacheEntry> _userCache = new();
        private static DateTime _lastCleanup = DateTime.MinValue;

        private class DashboardCacheEntry
        {
            public int PendingCount { get; set; }
            public int FlaggedCount { get; set; }
            public DateTime LastFetch { get; set; }
            public string Role { get; set; } = "";
        }

        public BaseController(IDashboardService? dashboardService = null, IAuditService? auditService = null)
        {
            _dashboardService = dashboardService;
            _auditService = auditService;
        }

        protected int GetCurrentUserId()
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
                return id;
            throw new InvalidOperationException("User not authenticated.");
        }

        protected int? TryGetCurrentUserId()
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
                return id;
            return null;
        }

        protected async Task LogAuditAsync(Domain.Enums.AuditEventType eventType, string entity, string entityId, string notes)
        {
            if (_auditService == null) return;
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditService.LogAsync(eventType, entity, entityId, userId, ip, null, null, notes);
            }
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated == true && _dashboardService != null)
            {
                try
                {
                    var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var role = User.FindFirstValue(ClaimTypes.Role) ?? "Staff";
                    if (int.TryParse(userIdStr, out var userId))
                    {
                        CleanupStaleCacheEntries();

                        bool useCache = _userCache.TryGetValue(userId, out var cached)
                                        && cached.Role == role
                                        && DateTime.UtcNow - cached.LastFetch < DashboardCacheDuration;

                        int pendingCount, flaggedCount;

                        if (useCache)
                        {
                            pendingCount = cached.PendingCount;
                            flaggedCount = cached.FlaggedCount;
                        }
                        else
                        {
                            try
                            {
                                var dashboard = await _dashboardService.GetDashboardAsync(userId, role);
                                pendingCount = dashboard.PendingApproval;
                                flaggedCount = dashboard.FlaggedForReview;
                            }
                            catch (Exception ex)
                            {
                                if (_auditService != null)
                                    _ = _auditService.LogAsync(Domain.Enums.AuditEventType.Update, "BaseController", "dashboard", userId, null, null, null, $"Dashboard fetch error: {ex.Message}");
                                if (useCache)
                                {
                                    pendingCount = cached!.PendingCount;
                                    flaggedCount = cached.FlaggedCount;
                                }
                                else
                                {
                                    pendingCount = 0;
                                    flaggedCount = 0;
                                }
                            }

                            _userCache[userId] = new DashboardCacheEntry
                            {
                                PendingCount = pendingCount,
                                FlaggedCount = flaggedCount,
                                LastFetch = DateTime.UtcNow,
                                Role = role
                            };
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
                catch (Exception ex)
                {
                    if (_auditService != null)
                        _ = _auditService.LogAsync(Domain.Enums.AuditEventType.Update, "BaseController", "dashboard", 0, null, null, null, $"Dashboard middleware error: {ex.Message}");
                    ViewBag.PendingCount = 0;
                    ViewBag.FlaggedCount = 0;
                }
            }

            await next();
        }

        private static void CleanupStaleCacheEntries()
        {
            var now = DateTime.UtcNow;
            if (now - _lastCleanup < TimeSpan.FromMinutes(10)) return;
            _lastCleanup = now;
            var staleKeys = _userCache.Where(kvp => now - kvp.Value.LastFetch > DashboardCacheDuration.Add(TimeSpan.FromMinutes(5)))
                                      .Select(kvp => kvp.Key).ToList();
            foreach (var key in staleKeys)
                _userCache.TryRemove(key, out _);
        }
    }
}
