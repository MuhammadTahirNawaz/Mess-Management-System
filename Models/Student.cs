using System.ComponentModel.DataAnnotations;

namespace Semester_Project.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string PermanentID { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsVerified { get; set; } = false;

        public bool IsFirstTimeLogin { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        // Membership Type: "TeaWater" or "FullMeal"
        [Required]
        [MaxLength(20)]
        public string MembershipType { get; set; } = "FullMeal";

        // Face Recognition Data
        public string? FaceEncoding { get; set; } // Store face embedding as JSON
        public bool IsFaceRegistered { get; set; } = false;
        public DateTime? FaceRegisteredAt { get; set; }

        // OTP fields for check-in verification
        [MaxLength(6)]
        public string? CurrentCheckInOTP { get; set; }

        public DateTime? OTPGeneratedAt { get; set; }

        // QR Code secret for verification (unique, non-guessable token)
        [MaxLength(100)]
        public string? QRCodeSecret { get; set; }

        // Navigation property for attendances
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

        // Navigation property for payments
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
