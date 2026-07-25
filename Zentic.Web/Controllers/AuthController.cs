using Zentic.Application.Interfaces;
using Zentic.Application.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Zentic.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IAuditService _auditService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, IAuditService auditService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _auditService = auditService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [EnableRateLimiting("LoginPolicy")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.LoginAsync(model.Email, model.Password, ip);

            if (result == null || !result.Success || result.User == null)
            {
                ModelState.AddModelError("", result?.ErrorMessage ?? "Invalid email or password.");
                _logger.LogWarning("Failed login attempt for {Email} from {IP}", model.Email, ip);
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.User.Id.ToString()),
                new Claim(ClaimTypes.Name, result.User.FullName),
                new Claim(ClaimTypes.Email, result.User.Email),
                new Claim(ClaimTypes.Role, result.User.Role),
                new Claim("EmployeeId", result.User.EmployeeId),
                new Claim("DepartmentId", result.User.DepartmentId?.ToString() ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProps = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

            await _auditService.LogAsync(Domain.Enums.AuditEventType.Login, "Auth", result.User.Id.ToString(),
                result.User.Id, ip, null, null, "User logged in");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (User.Identity?.IsAuthenticated == true &&
                int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                await _auditService.LogAsync(Domain.Enums.AuditEventType.Logout, "Auth",
                    userId.ToString(), userId, ip, null, null, "User logged out");
            }
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();
    }
}
