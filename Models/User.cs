using System.ComponentModel.DataAnnotations;

namespace Semester_Project.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

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

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        public bool IsFirstTimeLogin { get; set; } = true;

        public string? CurrentOTP { get; set; }

        public DateTime? OTPGeneratedAt { get; set; }
    }
}
