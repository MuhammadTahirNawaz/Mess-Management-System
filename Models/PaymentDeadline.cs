using System.ComponentModel.DataAnnotations;

namespace Semester_Project.Models
{
    public class PaymentDeadline
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Month { get; set; } // 1-12

        [Required]
        public int Year { get; set; }

        [Required]
        public DateTime DeadlineDate { get; set; }

        [Required]
        public decimal FineAmount { get; set; } = 1000;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
