using System.ComponentModel.DataAnnotations;

namespace ClaimManagementSystem.Models
{
    public class Claim
    {
        public int ClaimId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required(ErrorMessage = "Claim title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 180, ErrorMessage = "Hours worked must be between 0.01 and 180")]
        public decimal HoursWorked { get; set; }

        public decimal Amount { get; set; }
        public string Status { get; set; } = "Submitted";
        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        public string? Documentation { get; set; } // Now stores file path
        public string? OriginalFileName { get; set; } // Store original file name

        public bool? ApprovedByProgrammeCoordinator { get; set; }
        public bool? ApprovedByAcademicManager { get; set; }
        public string? RejectionReason { get; set; }
    }
}