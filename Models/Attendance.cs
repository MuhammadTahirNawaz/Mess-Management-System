using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Semester_Project.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string MealType { get; set; } = string.Empty; // Breakfast, Lunch, Dinner

        [Required]
        public decimal Amount { get; set; }

        public bool IsPaid { get; set; } = false;

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
