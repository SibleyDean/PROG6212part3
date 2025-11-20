using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClaimManagementSystem.Models;
using ClaimManagementSystem.Services;

namespace ClaimManagementSystem.Controllers
{
    public class ProgrammeCoordinatorController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public ProgrammeCoordinatorController(ApplicationDbContext context, SessionService sessionService)
            : base(sessionService)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsUserLoggedIn() || !IsUserInRole("ProgrammeCoordinator"))
                return RedirectToAction("AccessDenied", "Account");

            var claims = await _context.Claims
                .Include(c => c.User)
                .Where(c => c.Status == "Submitted")
                .OrderBy(c => c.SubmissionDate)
                .ToListAsync();

            return View(claims);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveClaim(int claimId)
        {
            if (!IsAuthorized("ProgrammeCoordinator"))
                return RedirectToAccessDenied();

            var claim = await _context.Claims.FindAsync(claimId);
            if (claim != null)
            {
                claim.Status = "ApprovedByProgrammeCoordinator";
                claim.ApprovedByProgrammeCoordinator = true;
                await _context.SaveChangesAsync();
                SetSuccessMessage("Claim approved successfully");
            }
            else
            {
                SetErrorMessage("Claim not found");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RejectClaim(int claimId, string rejectionReason)
        {
            if (!IsAuthorized("ProgrammeCoordinator"))
                return RedirectToAccessDenied();

            var claim = await _context.Claims.FindAsync(claimId);
            if (claim != null)
            {
                claim.Status = "Rejected";
                claim.ApprovedByProgrammeCoordinator = false;
                claim.RejectionReason = rejectionReason ?? "No reason provided";
                await _context.SaveChangesAsync();
                SetSuccessMessage("Claim rejected successfully");
            }
            else
            {
                SetErrorMessage("Claim not found");
            }

            return RedirectToAction("Index");
        }
    }
}