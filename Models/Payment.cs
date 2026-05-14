using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Semester_Project.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(100)]
        public string StripePaymentIntentId { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Succeeded, Failed, Canceled

        [MaxLength(20)]
        public string? CardBrand { get; set; } // Visa, Mastercard, etc.

        [MaxLength(4)]
        public string? CardLast4 { get; set; } // Last 4 digits of card

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? PaidAt { get; set; }

        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        // Related attendance IDs (comma-separated)
        [MaxLength(500)]
        public string? AttendanceIds { get; set; }
    }
}
