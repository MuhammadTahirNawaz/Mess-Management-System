using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester_Project.Data;
using Semester_Project.Models;
using Semester_Project.Services;
using System.Security.Cryptography;
using System.Text;

namespace Semester_Project.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminController> _logger;
        private const string ADMIN_EMAIL = "tahirnawaz12194@gmail.com";

        public AdminController(ApplicationDbContext context, IEmailService emailService, ILogger<AdminController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: Admin/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Admin/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                // Check if the email is the authorized admin email
                if (email != ADMIN_EMAIL)
                {
                    TempData["Error"] = "Unauthorized email address.";
                    return View();
                }

                // Check if user exists in database
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    // First time login - create user and send OTP
                    user = new User
                    {
                        Name = "Admin",
                        Email = email,
                        PasswordHash = string.Empty,
                        IsVerified = false,
                        CreatedAt = DateTime.Now,
                        IsFirstTimeLogin = true
                    };

                    // Generate 6-digit OTP
                    var otp = GenerateOTP();
                    user.CurrentOTP = otp;
                    user.OTPGeneratedAt = DateTime.Now;

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Send OTP email
                    await _emailService.SendOTPEmailAsync(email, otp);

                    TempData["Success"] = "OTP has been sent to your email.";
                    return RedirectToAction("VerifyOTP", new { email = email });
                }
                else if (user.IsFirstTimeLogin)
                {
                    // User exists but hasn't set password yet - send OTP again
                    var otp = GenerateOTP();
                    user.CurrentOTP = otp;
                    user.OTPGeneratedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    await _emailService.SendOTPEmailAsync(email, otp);

                    TempData["Success"] = "OTP has been sent to your email.";
                    return RedirectToAction("VerifyOTP", new { email = email });
                }
                else
                {
                    // User has already set password - verify password
                    if (string.IsNullOrEmpty(password))
                    {
                        TempData["Error"] = "Please enter your password.";
                        return View();
                    }

                    var passwordHash = HashPassword(password);
                    if (user.PasswordHash == passwordHash)
                    {
                        // Successful login - remove any existing student session keys only
                        HttpContext.Session.Remove("StudentEmail");
                        HttpContext.Session.Remove("StudentName");
                        HttpContext.Session.Remove("StudentId");
                        
                        // Set admin session
                        HttpContext.Session.SetString("AdminEmail", user.Email);
                        TempData["Success"] = "Login successful!";
                        return RedirectToAction("Dashboard");
                    }
                    else
                    {
                        TempData["Error"] = "Invalid password.";
                        return View();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process");
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return View();
            }
        }

        // GET: Admin/VerifyOTP
        [HttpGet]
        public IActionResult VerifyOTP(string email)
        {
            if (string.IsNullOrEmpty(email) || email != ADMIN_EMAIL)
            {
                return RedirectToAction("Login");
            }

            ViewBag.Email = email;
            return View();
        }

        // POST: Admin/VerifyOTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(string email, string otp)
        {
            try
            {
                if (email != ADMIN_EMAIL)
                {
                    TempData["Error"] = "Unauthorized email address.";
                    return RedirectToAction("Login");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    TempData["Error"] = "User not found. Please login again.";
                    return RedirectToAction("Login");
                }

                // Check if OTP is valid
                if (user.CurrentOTP != otp)
                {
                    TempData["Error"] = "Invalid OTP. Please try again.";
                    ViewBag.Email = email;
                    return View();
                }

                // Check if OTP has expired (10 minutes validity)
                if (user.OTPGeneratedAt == null || (DateTime.Now - user.OTPGeneratedAt.Value).TotalMinutes > 10)
                {
                    TempData["Error"] = "OTP has expired. Please login again to receive a new OTP.";
                    return RedirectToAction("Login");
                }

                // OTP verified successfully - clear OTP and redirect to set password
                user.CurrentOTP = null;
                user.OTPGeneratedAt = null;
                await _context.SaveChangesAsync();

                TempData["Success"] = "OTP verified successfully. Please set your password.";
                return RedirectToAction("SetPassword", new { email = email });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OTP verification");
                TempData["Error"] = "An error occurred. Please try again.";
                ViewBag.Email = email;
                return View();
            }
        }

        // GET: Admin/SetPassword
        [HttpGet]
        public async Task<IActionResult> SetPassword(string email)
        {
            if (string.IsNullOrEmpty(email) || email != ADMIN_EMAIL)
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !user.IsFirstTimeLogin)
            {
                return RedirectToAction("Login");
            }

            ViewBag.Email = email;
            return View();
        }

        // POST: Admin/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(string email, string password, string confirmPassword)
        {
            try
            {
                if (email != ADMIN_EMAIL)
                {
                    TempData["Error"] = "Unauthorized email address.";
                    return RedirectToAction("Login");
                }

                if (string.IsNullOrEmpty(password) || password.Length < 6)
                {
                    TempData["Error"] = "Password must be at least 6 characters long.";
                    ViewBag.Email = email;
                    return View();
                }

                if (password != confirmPassword)
                {
                    TempData["Error"] = "Passwords do not match.";
                    ViewBag.Email = email;
                    return View();
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    TempData["Error"] = "User not found. Please login again.";
                    return RedirectToAction("Login");
                }

                // Set password and mark first time login as false
                user.PasswordHash = HashPassword(password);
                user.IsFirstTimeLogin = false;
                await _context.SaveChangesAsync();

                HttpContext.Session.SetString("AdminEmail", user.Email);
                TempData["Success"] = "Password set successfully!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password setup");
                TempData["Error"] = "An error occurred. Please try again.";
                ViewBag.Email = email;
                return View();
            }
        }

        // GET: Admin/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Logged out successfully.";
            return RedirectToAction("Login");
        }

        // Helper method to generate 6-digit OTP
        private string GenerateOTP()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // GET: Admin/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail))
            {
                return RedirectToAction("Login", "Admin");
            }

            // Clear any student session data to ensure admin-only access
            HttpContext.Session.Remove("StudentEmail");
            HttpContext.Session.Remove("StudentName");
            HttpContext.Session.Remove("StudentId");

            // Get statistics for dashboard
            ViewBag.TotalStudents = await _context.Students.CountAsync();
            ViewBag.ActiveStudents = await _context.Students.Where(s => s.IsVerified).CountAsync();
            ViewBag.TodayCheckIns = await _context.Attendances
                .Where(a => a.Date.Date == DateTime.Today)
                .Select(a => a.StudentId)
                .Distinct()
                .CountAsync();
            
            // Count students with unpaid bills (students who have attendance but no corresponding payment)
            var studentsWithBills = await _context.Attendances
                .Where(a => a.IsPaid == false)
                .Select(a => a.StudentId)
                .Distinct()
                .CountAsync();
            ViewBag.PendingBills = studentsWithBills;

            // Ensure we return Admin Dashboard view, not Student
            return View("~/Views/Admin/Dashboard.cshtml");
        }

        // POST: Admin/ClearTestData - Clear all face registrations and attendance
        [HttpPost]
        public async Task<IActionResult> ClearTestData()
        {
            try
            {
                // Check if admin is logged in
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("AdminEmail")))
                {
                    return Json(new { success = false, message = "Unauthorized. Please login as admin." });
                }

                // Clear all face registrations
                var studentsWithFaces = await _context.Students
                    .Where(s => s.IsFaceRegistered)
                    .ToListAsync();

                foreach (var student in studentsWithFaces)
                {
                    student.FaceEncoding = null;
                    student.IsFaceRegistered = false;
                    student.FaceRegisteredAt = null;
                }

                // Clear all attendance records
                var allAttendance = await _context.Attendances.ToListAsync();
                _context.Attendances.RemoveRange(allAttendance);

                // Save changes
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin cleared {studentsWithFaces.Count} face registrations and {allAttendance.Count} attendance records");

                return Json(new 
                { 
                    success = true, 
                    message = $"Successfully cleared {studentsWithFaces.Count} face registrations and {allAttendance.Count} attendance records." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing test data");
                return Json(new { success = false, message = "An error occurred while clearing data." });
            }
        }

        // Helper method to hash password using SHA256
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
