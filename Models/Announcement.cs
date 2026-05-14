using System.ComponentModel.DataAnnotations;

namespace Semester_Project.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        [StringLength(20)]
        public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ExpiresAt { get; set; }

        public DateTime? VisibleFrom { get; set; }

        public DateTime? VisibleTo { get; set; }

        public int CreatedByUserId { get; set; }
    }
}
