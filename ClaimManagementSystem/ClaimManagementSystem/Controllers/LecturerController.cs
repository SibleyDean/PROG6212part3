using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClaimManagementSystem.Models;
using ClaimManagementSystem.Services;

namespace ClaimManagementSystem.Controllers
{
    public class LecturerController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public LecturerController(ApplicationDbContext context, SessionService sessionService, IWebHostEnvironment environment)
            : base(sessionService)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsUserLoggedIn() || !IsUserInRole("Lecturer"))
                return RedirectToAction("AccessDenied", "Account");

            var user = _sessionService.GetUserSession();
            var claims = await _context.Claims
                .Where(c => c.UserId == user.UserId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            return View(claims);
        }

        public IActionResult CreateClaim()
        {
            if (!IsUserLoggedIn() || !IsUserInRole("Lecturer"))
                return RedirectToAction("AccessDenied", "Account");

            var user = _sessionService.GetUserSession();
            ViewBag.HourlyRate = user.HourlyRate;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClaim(Claim claim, IFormFile documentationFile)
        {
            if (!IsUserLoggedIn() || !IsUserInRole("Lecturer"))
                return RedirectToAccessDenied();

            var user = _sessionService.GetUserSession();
            if (user == null)
            {
                SetErrorMessage("User session not found. Please login again.");
                return RedirectToAction("Login", "Account");
            }

            if (claim.HoursWorked > 180)
            {
                ModelState.AddModelError("HoursWorked", "Hours worked cannot exceed 180 hours per month");
            }

            // Validate file upload
            if (documentationFile == null || documentationFile.Length == 0)
            {
                ModelState.AddModelError("documentationFile", "Please upload a supporting document");
            }
            else
            {
                // Validate file type and size
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(documentationFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("documentationFile", "Please upload a PDF, Word document, or image file (PDF, DOC, DOCX, JPG, JPEG, PNG)");
                }

                if (documentationFile.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    ModelState.AddModelError("documentationFile", "File size cannot exceed 5MB");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle file upload
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "documentation");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(documentationFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await documentationFile.CopyToAsync(fileStream);
                    }

                    claim.UserId = user.UserId;
                    claim.Amount = claim.HoursWorked * (user.HourlyRate ?? 0);
                    claim.Status = "Submitted";
                    claim.Documentation = $"/uploads/documentation/{uniqueFileName}";
                    claim.OriginalFileName = documentationFile.FileName;

                    _context.Claims.Add(claim);
                    await _context.SaveChangesAsync();

                    SetSuccessMessage("Claim submitted successfully with supporting documentation");
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    SetErrorMessage("Error uploading file. Please try again.");
                    // Log the exception
                }
            }

            ViewBag.HourlyRate = user.HourlyRate ?? 0;
            return View(claim);
        }

        public async Task<IActionResult> ViewClaim(int id)
        {
            if (!IsUserLoggedIn() || !IsUserInRole("Lecturer"))
                return RedirectToAccessDenied();

            var claim = await _context.Claims
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ClaimId == id && c.UserId == GetCurrentUserId());

            if (claim == null)
            {
                SetErrorMessage("Claim not found");
                return RedirectToAction("Index");
            }

            return View(claim);
        }

        // GET: Edit Claim
        public async Task<IActionResult> EditClaim(int id)
        {
            if (!IsUserLoggedIn() || !IsUserInRole("Lecturer"))
                return RedirectToAccessDenied();

            var claim = await _context.Claims
                .FirstOrDefaultAsync(c => c.ClaimId == id && c.UserId == GetCurrentUserId());

            if (claim == null)
            {
                SetErrorMessage("Claim not found");
                return RedirectToAction("Index");
            }

            // Only allow editing of submitted claims
            if (claim.Status != "Submitted")
            {
                SetErrorMessage("Only submitted claims can be edited");
                return RedirectToAction("Index");
            }

            var user = _sessionService.GetUserSession();
            ViewBag.HourlyRate = user?.HourlyRate ?? 0;
            return View(claim);
        }

        // POST: Edit Claim (ONLY ONE VERSION)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditClaim(Claim claim, IFormFile? documentationFile)
        {
            if (!IsUserLoggedIn() || !IsUserInRole("Lecturer"))
                return RedirectToAccessDenied();

            try
            {
                var existingClaim = await _context.Claims
                    .FirstOrDefaultAsync(c => c.ClaimId == claim.ClaimId && c.UserId == GetCurrentUserId());

                if (existingClaim == null)
                {
                    SetErrorMessage("Claim not found or you don't have permission to edit this claim");
                    return RedirectToAction("Index");
                }

                // Only allow editing of submitted claims
                if (existingClaim.Status != "Submitted")
                {
                    SetErrorMessage("Only submitted claims can be edited. Current status: " + existingClaim.Status);
                    return RedirectToAction("Index");
                }

                // Validate hours
                if (claim.HoursWorked > 180)
                {
                    ModelState.AddModelError("HoursWorked", "Hours worked cannot exceed 180 hours per month");
                }

                // File validation if a new file is uploaded
                if (documentationFile != null && documentationFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(documentationFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("documentationFile", "Please upload a PDF, Word document, or image file (PDF, DOC, DOCX, JPG, JPEG, PNG)");
                    }

                    if (documentationFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("documentationFile", "File size cannot exceed 5MB");
                    }
                }

                if (ModelState.IsValid)
                {
                    var user = _sessionService.GetUserSession();
                    if (user != null)
                    {
                        // Update claim properties
                        existingClaim.Title = claim.Title ?? string.Empty;
                        existingClaim.Description = claim.Description ?? string.Empty;
                        existingClaim.HoursWorked = claim.HoursWorked;
                        existingClaim.Amount = claim.HoursWorked * (user.HourlyRate ?? 0);

                        // Handle file upload if a new file is provided
                        if (documentationFile != null && documentationFile.Length > 0)
                        {
                            // Delete old file if exists
                            if (!string.IsNullOrEmpty(existingClaim.Documentation))
                            {
                                try
                                {
                                    var oldFilePath = Path.Combine(_environment.WebRootPath, existingClaim.Documentation.TrimStart('/'));
                                    if (System.IO.File.Exists(oldFilePath))
                                    {
                                        System.IO.File.Delete(oldFilePath);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Log but continue - old file couldn't be deleted
                                    Console.WriteLine($"Could not delete old file: {ex.Message}");
                                }
                            }

                            // Upload new file
                            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "documentation");
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(documentationFile.FileName)}";
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await documentationFile.CopyToAsync(fileStream);
                            }

                            existingClaim.Documentation = $"/uploads/documentation/{uniqueFileName}";
                            existingClaim.OriginalFileName = documentationFile.FileName;
                        }

                        _context.Claims.Update(existingClaim);
                        await _context.SaveChangesAsync();

                        SetSuccessMessage("Claim updated successfully!");
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        SetErrorMessage("User session expired. Please login again.");
                        return RedirectToAction("Login", "Account");
                    }
                }
                else
                {
                    // If model state is invalid, gather error messages
                    var errorMessages = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    SetErrorMessage("Please fix the following errors: " + string.Join(", ", errorMessages));
                }
            }
            catch (DbUpdateException dbEx)
            {
                SetErrorMessage("Database error occurred while updating the claim. Please try again.");
                // Log the exception details
                Console.WriteLine($"Database update error: {dbEx.Message}");
            }
            catch (Exception ex)
            {
                SetErrorMessage("An unexpected error occurred. Please try again.");
                // Log the exception details
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }

            // If we get here, something went wrong - redisplay the form
            var currentUser = _sessionService.GetUserSession();
            ViewBag.HourlyRate = currentUser?.HourlyRate ?? 0;
            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClaim(int id)
        {
            if (!IsUserLoggedIn() || !IsUserInRole("Lecturer"))
                return RedirectToAccessDenied();

            var claim = await _context.Claims
                .FirstOrDefaultAsync(c => c.ClaimId == id && c.UserId == GetCurrentUserId());

            if (claim == null)
            {
                SetErrorMessage("Claim not found");
                return RedirectToAction("Index");
            }

            // Only allow deletion of submitted claims
            if (claim.Status != "Submitted")
            {
                SetErrorMessage("Only submitted claims can be deleted");
                return RedirectToAction("Index");
            }

            _context.Claims.Remove(claim);
            await _context.SaveChangesAsync();
            SetSuccessMessage("Claim deleted successfully");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult CalculateAmount(decimal hoursWorked)
        {
            var user = _sessionService.GetUserSession();
            if (user == null)
            {
                return Json(new { amount = "0.00" });
            }

            var amount = hoursWorked * (user.HourlyRate ?? 0);
            return Json(new { amount = amount.ToString("F2") });
        }
    }
}