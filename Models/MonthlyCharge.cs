using System.ComponentModel.DataAnnotations;

namespace Semester_Project.Models
{
    public class MonthlyCharge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ChargeName { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}
