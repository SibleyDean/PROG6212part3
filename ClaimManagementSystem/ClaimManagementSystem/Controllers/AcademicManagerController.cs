using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClaimManagementSystem.Models;
using ClaimManagementSystem.Services;

namespace ClaimManagementSystem.Controllers
{
    public class AcademicManagerController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public AcademicManagerController(ApplicationDbContext context, SessionService sessionService)
            : base(sessionService)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsUserLoggedIn() || !IsUserInRole("AcademicManager"))
                return RedirectToAction("AccessDenied", "Account");

            var claims = await _context.Claims
                .Include(c => c.User)
                .Where(c => c.Status == "ApprovedByProgrammeCoordinator")
                .OrderBy(c => c.SubmissionDate)
                .ToListAsync();

            return View(claims);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveClaim(int claimId)
        {
            if (!IsUserLoggedIn() || !IsUserInRole("AcademicManager"))
                return RedirectToAction("AccessDenied", "Account");

            var claim = await _context.Claims.FindAsync(claimId);
            if (claim != null)
            {
                claim.Status = "Paid";
                claim.ApprovedByAcademicManager = true;
                await _context.SaveChangesAsync();
                SetSuccessMessage("Claim approved and marked as paid");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RejectClaim(int claimId, string rejectionReason)
        {
            if (!IsUserLoggedIn() || !IsUserInRole("AcademicManager"))
                return RedirectToAction("AccessDenied", "Account");

            var claim = await _context.Claims.FindAsync(claimId);
            if (claim != null)
            {
                claim.Status = "Rejected";
                claim.ApprovedByAcademicManager = false;
                claim.RejectionReason = rejectionReason;
                await _context.SaveChangesAsync();
                SetSuccessMessage("Claim rejected successfully");
            }

            return RedirectToAction("Index");
        }
    }
}