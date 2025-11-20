using Microsoft.AspNetCore.Mvc;
using ClaimManagementSystem.Services;

namespace ClaimManagementSystem.Controllers
{
    public class BaseController : Controller
    {
        protected readonly SessionService _sessionService;

        public BaseController(SessionService sessionService)
        {
            _sessionService = sessionService;
        }

        protected bool IsUserLoggedIn()
        {
            return _sessionService.GetUserSession() != null;
        }

        protected bool IsUserInRole(string role)
        {
            var user = _sessionService.GetUserSession();
            return user?.Role == role;
        }

        protected string? GetCurrentUserRole()
        {
            return _sessionService.GetUserRole();
        }

        protected int? GetCurrentUserId()
        {
            return _sessionService.GetUserId();
        }

        protected void SetErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }

        protected void SetSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        // Helper method to redirect to access denied with safety check
        protected IActionResult RedirectToAccessDenied()
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        // Helper method to check if user is authenticated and has required role
        protected bool IsAuthorized(string requiredRole)
        {
            return IsUserLoggedIn() && IsUserInRole(requiredRole);
        }
    }
}