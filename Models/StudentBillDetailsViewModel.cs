namespace Semester_Project.Models
{
    public class StudentBillDetailsViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string PermanentID { get; set; } = string.Empty;
        public List<Attendance> Attendances { get; set; } = new List<Attendance>();
        public int TotalMeals { get; set; }
        public int UnpaidMeals { get; set; }
        public int PaidMeals { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal UnpaidAmount { get; set; }
        public decimal PaidAmount { get; set; }
    }
}
