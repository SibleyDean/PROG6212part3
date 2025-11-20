using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClaimManagementSystem.Models;
using ClaimManagementSystem.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClaimManagementSystem.Controllers
{
    public class HRController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public HRController(ApplicationDbContext context, SessionService sessionService, IWebHostEnvironment environment)
            : base(sessionService)
        {
            _context = context;
            _environment = environment;

            // Set QuestPDF license (free for open source)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsUserLoggedIn() || !IsUserInRole("HR"))
                return RedirectToAction("AccessDenied", "Account");

            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            return View(users);
        }

        public IActionResult CreateUser()
        {
            if (!IsUserLoggedIn() || !IsUserInRole("HR"))
                return RedirectToAction("AccessDenied", "Account");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(User user)
        {
            if (!IsUserLoggedIn() || !IsUserInRole("HR"))
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    SetErrorMessage("Email already exists");
                    return View(user);
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                SetSuccessMessage("User created successfully");
                return RedirectToAction("Index");
            }

            return View(user);
        }

        public async Task<IActionResult> EditUser(int id)
        {
            if (!IsUserLoggedIn() || !IsUserInRole("HR"))
                return RedirectToAction("AccessDenied", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                SetErrorMessage("User not found");
                return RedirectToAction("Index");
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(User user)
        {
            if (!IsUserLoggedIn() || !IsUserInRole("HR"))
                return RedirectToAction("AccessDenied", "Account");

            if (ModelState.IsValid)
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                SetSuccessMessage("User updated successfully");
                return RedirectToAction("Index");
            }

            return View(user);
        }

        public async Task<IActionResult> GenerateReport()
        {
            if (!IsUserLoggedIn() || !IsUserInRole("HR"))
                return RedirectToAction("AccessDenied", "Account");

            try
            {
                var claims = await _context.Claims
                    .Include(c => c.User)
                    .Where(c => c.Status == "Paid")
                    .OrderByDescending(c => c.SubmissionDate)
                    .ToListAsync();

                var users = await _context.Users
                    .Where(u => u.IsActive && u.Role == "Lecturer")
                    .ToListAsync();

                // Generate PDF using QuestPDF
                var pdfBytes = GeneratePdfReport(claims, users);

                return File(pdfBytes, "application/pdf", $"ClaimsReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Error generating report: {ex.Message}");
                return RedirectToAction("Index");
            }
        }

        private byte[] GeneratePdfReport(List<Claim> claims, List<User> users)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .AlignCenter()
                        .Text("CLAIMS MANAGEMENT SYSTEM")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken3)
                        .ParagraphSpacing(0.5f, Unit.Centimetre);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            // Report Title
                            x.Item().AlignCenter().Text($"MONTHLY REPORT - {DateTime.Now:MMMM yyyy}")
                                .SemiBold().FontSize(14);

                            // Summary Section
                            x.Item().Component(new SummaryComponent(claims, users));

                            // Claims Details Section
                            if (claims.Any())
                            {
                                x.Item().Component(new ClaimsTableComponent(claims));
                            }

                            // Lecturers Summary
                            x.Item().Component(new LecturersComponent(users, claims));
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                            x.Span($" - Generated on {DateTime.Now:dd MMMM yyyy HH:mm}");
                        });
                });
            });

            return document.GeneratePdf();
        }

        // CSV Report Alternative
        public async Task<IActionResult> GenerateCSVReport()
        {
            if (!IsUserLoggedIn() || !IsUserInRole("HR"))
                return RedirectToAction("AccessDenied", "Account");

            var claims = await _context.Claims
                .Include(c => c.User)
                .Where(c => c.Status == "Paid")
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();

            // Create CSV content
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Lecturer,Title,SubmissionDate,HoursWorked,Amount,Status,Description");

            foreach (var claim in claims)
            {
                var description = claim.Description?.Replace("\"", "\"\"") ?? "";
                csv.AppendLine($"\"{claim.User?.Name} {claim.User?.Surname}\",\"{claim.Title}\",\"{claim.SubmissionDate:yyyy-MM-dd}\",{claim.HoursWorked},{claim.Amount},\"{claim.Status}\",\"{description}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"ClaimsReport_{DateTime.Now:yyyyMMdd}.csv");
        }
    }

    // QuestPDF Components
    public class SummaryComponent : IComponent
    {
        private readonly List<Claim> _claims;
        private readonly List<User> _users;

        public SummaryComponent(List<Claim> claims, List<User> users)
        {
            _claims = claims;
            _users = users;
        }

        public void Compose(IContainer container)
        {
            var totalAmount = _claims.Sum(c => c.Amount);
            var totalHours = _claims.Sum(c => c.HoursWorked);

            container.Background(Colors.Grey.Lighten3)
                .Padding(15)
                .Column(column =>
                {
                    column.Item().Text("SUMMARY STATISTICS").SemiBold().FontSize(12);
                    column.Spacing(10);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Blue.Medium).Padding(10).AlignCenter()
                            .Text($"Total Claims\n{_claims.Count}").FontColor(Colors.White).Bold();

                        row.RelativeItem().Background(Colors.Green.Medium).Padding(10).AlignCenter()
                            .Text($"Total Amount\nR {totalAmount:F2}").FontColor(Colors.White).Bold();

                        row.RelativeItem().Background(Colors.Orange.Medium).Padding(10).AlignCenter()
                            .Text($"Total Hours\n{totalHours:F1}").FontColor(Colors.White).Bold();

                        row.RelativeItem().Background(Colors.Purple.Medium).Padding(10).AlignCenter()
                            .Text($"Active Lecturers\n{_users.Count}").FontColor(Colors.White).Bold();
                    });
                });
        }
    }

    public class ClaimsTableComponent : IComponent
    {
        private readonly List<Claim> _claims;

        public ClaimsTableComponent(List<Claim> claims)
        {
            _claims = claims;
        }

        public void Compose(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("PAID CLAIMS DETAILS").SemiBold().FontSize(12);
                column.Spacing(10);

                column.Item().Table(table =>
                {
                    // Define columns
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // Lecturer
                        columns.RelativeColumn(3); // Title
                        columns.RelativeColumn(1.5f); // Date
                        columns.RelativeColumn(1); // Hours
                        columns.RelativeColumn(1.5f); // Amount
                        columns.RelativeColumn(1.5f); // Status
                    });

                    // Table header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Lecturer").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Title").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Submission Date").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Hours").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Amount").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Blue.Darken3).Padding(5).Text("Status").FontColor(Colors.White).Bold();
                    });

                    // Table content
                    foreach (var claim in _claims)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{claim.User?.Name} {claim.User?.Surname}");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(claim.Title);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(claim.SubmissionDate.ToString("dd/MM/yyyy"));
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(claim.HoursWorked.ToString("F1"));
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"R {claim.Amount:F2}");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(claim.Status);
                    }
                });
            });
        }
    }

    public class LecturersComponent : IComponent
    {
        private readonly List<User> _users;
        private readonly List<Claim> _claims;

        public LecturersComponent(List<User> users, List<Claim> claims)
        {
            _users = users;
            _claims = claims;
        }

        public void Compose(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("LECTURER SUMMARY").SemiBold().FontSize(12);
                column.Spacing(10);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2); // Name
                        columns.RelativeColumn(3); // Email
                        columns.RelativeColumn(1.5f); // Hourly Rate
                        columns.RelativeColumn(1.5f); // Active Claims
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Green.Darken3).Padding(5).Text("Name").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Green.Darken3).Padding(5).Text("Email").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Green.Darken3).Padding(5).Text("Hourly Rate").FontColor(Colors.White).Bold();
                        header.Cell().Background(Colors.Green.Darken3).Padding(5).Text("Paid Claims").FontColor(Colors.White).Bold();
                    });

                    // Content
                    foreach (var user in _users)
                    {
                        var userClaims = _claims.Count(c => c.UserId == user.UserId);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{user.Name} {user.Surname}");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(user.Email);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(user.HourlyRate.HasValue ? $"R {user.HourlyRate:F2}" : "N/A");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(userClaims.ToString());
                    }
                });
            });
        }
    }
}