using Microsoft.EntityFrameworkCore;
using Semester_Project.Models;

namespace Semester_Project.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<MonthlyCharge> MonthlyCharges { get; set; }
        public DbSet<BillRecheckRequest> BillRecheckRequests { get; set; }
        public DbSet<PaymentDeadline> PaymentDeadlines { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ensure email is unique for Users
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Ensure email is unique for Students
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Email)
                .IsUnique();

            // Configure decimal precision for MenuItem
            modelBuilder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasPrecision(18, 2);

            // Configure decimal precision for Attendance
            modelBuilder.Entity<Attendance>()
                .Property(a => a.Amount)
                .HasPrecision(18, 2);
        }
    }
}
