using Microsoft.AspNetCore.Http;
using ClaimManagementSystem.Models;

namespace ClaimManagementSystem.Services
{
    public class SessionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void SetUserSession(User user)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                session.SetString("UserId", user.UserId.ToString());
                session.SetString("UserName", user.Name ?? string.Empty);
                session.SetString("UserSurname", user.Surname ?? string.Empty);
                session.SetString("UserEmail", user.Email ?? string.Empty);
                session.SetString("UserRole", user.Role ?? string.Empty);
                session.SetString("UserHourlyRate", user.HourlyRate?.ToString() ?? "0");
            }
        }

        public User? GetUserSession()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) return null;

            var userIdString = session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString)) return null;

            // Safe parsing with null checks
            if (!int.TryParse(userIdString, out int userId)) return null;

            var hourlyRateString = session.GetString("UserHourlyRate");
            decimal? hourlyRate = null;
            if (decimal.TryParse(hourlyRateString, out decimal rate))
            {
                hourlyRate = rate;
            }

            return new User
            {
                UserId = userId,
                Name = session.GetString("UserName") ?? string.Empty,
                Surname = session.GetString("UserSurname") ?? string.Empty,
                Email = session.GetString("UserEmail") ?? string.Empty,
                Role = session.GetString("UserRole") ?? string.Empty,
                HourlyRate = hourlyRate
            };
        }

        public void ClearSession()
        {
            _httpContextAccessor.HttpContext?.Session.Clear();
        }

        // Helper method to safely get user ID
        public int? GetUserId()
        {
            var user = GetUserSession();
            return user?.UserId;
        }

        // Helper method to safely get user role
        public string? GetUserRole()
        {
            var user = GetUserSession();
            return user?.Role;
        }
    }
}