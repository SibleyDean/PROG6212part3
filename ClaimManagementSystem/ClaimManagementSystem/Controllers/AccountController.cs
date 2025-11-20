using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClaimManagementSystem.Models;
using ClaimManagementSystem.Services;

namespace ClaimManagementSystem.Controllers
{
    public class AccountController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context, SessionService sessionService)
            : base(sessionService)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            if (IsUserLoggedIn())
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                SetErrorMessage("Email and password are required");
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Password == password && u.IsActive);

            if (user != null)
            {
                _sessionService.SetUserSession(user);

                return user.Role switch
                {
                    "HR" => RedirectToAction("Index", "HR"),
                    "Lecturer" => RedirectToAction("Index", "Lecturer"),
                    "ProgrammeCoordinator" => RedirectToAction("Index", "ProgrammeCoordinator"),
                    "AcademicManager" => RedirectToAction("Index", "AcademicManager"),
                    _ => RedirectToAction("Index", "Home")
                };
            }

            SetErrorMessage("Invalid email or password");
            return View();
        }

        public IActionResult Logout()
        {
            _sessionService.ClearSession();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}