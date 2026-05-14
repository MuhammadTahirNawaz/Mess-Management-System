using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester_Project.Data;
using Semester_Project.Models;
using Semester_Project.Services;
using System.Security.Cryptography;
using System.Text;

namespace Semester_Project.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IQRCodeService _qrCodeService;
        private readonly ILogger<StudentController> _logger;
        private const string ALLOWED_EMAIL_DOMAIN = "@student.uet.edu.pk";

        public StudentController(ApplicationDbContext context, IEmailService emailService, IQRCodeService qrCodeService, ILogger<StudentController> logger)
        {
            _context = context;
            _emailService = emailService;
            _qrCodeService = qrCodeService;
            _logger = logger;
        }

        // Helper method to check if student is logged in and refresh session
        private bool IsStudentLoggedIn()
        {
            var email = HttpContext.Session.GetString("StudentEmail");
            if (!string.IsNullOrEmpty(email))
            {
                // Refresh session to keep it alive
                HttpContext.Session.SetString("StudentEmail", email);
                return true;
            }
            return false;
        }

        // GET: Student/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Student/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string name, string email, string membershipType = "FullMeal")
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                {
                    TempData["Error"] = "Name and email are required.";
                    return View();
                }

                // Check if email ends with allowed domain
                if (!email.EndsWith(ALLOWED_EMAIL_DOMAIN, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = $"Only {ALLOWED_EMAIL_DOMAIN} emails are allowed to register.";
                    return View();
                }

                // Check if email already exists
                var existingStudent = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
                if (existingStudent != null)
                {
                    TempData["Error"] = "This email is already registered. Please login instead.";
                    return RedirectToAction("Login");
                }

                // Generate random password
                var randomPassword = GenerateRandomPassword(10);
                var passwordHash = HashPassword(randomPassword);

                // Generate unique PermanentID (format: UET-YYYY-XXXX)
                var permanentId = await GenerateUniquePermanentID();

                // Generate unique QR code secret (non-guessable token)
                var qrCodeSecret = Guid.NewGuid().ToString("N"); // 32-character hex string

                // Create new student (unverified until first login)
                var student = new Student
                {
                    PermanentID = permanentId,
                    QRCodeSecret = qrCodeSecret,
                    Name = name,
                    Email = email,
                    PasswordHash = passwordHash,
                    MembershipType = membershipType,
                    IsVerified = false, // Set to true only after first successful login
                    IsFirstTimeLogin = true,
                    CreatedAt = DateTime.Now
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Send credentials via email with Permanent ID
                await _emailService.SendStudentCredentialsEmailAsync(email, name, randomPassword, permanentId);

                TempData["Success"] = "Registration successful! Your login credentials have been sent to your email.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during student registration");
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return View();
            }
        }

        // GET: Student/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: Student/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    TempData["Error"] = "Email and password are required.";
                    return View();
                }

                // Find student
                var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);

                if (student == null)
                {
                    TempData["Error"] = "Invalid email or password.";
                    return View();
                }

                // Verify password
                var passwordHash = HashPassword(password);
                if (student.PasswordHash != passwordHash)
                {
                    TempData["Error"] = "Invalid email or password.";
                    return View();
                }

                // Update last login and verify student on successful login
                student.LastLoginAt = DateTime.Now;
                if (!student.IsVerified)
                {
                    student.IsVerified = true;
                }
                await _context.SaveChangesAsync();

                // Remove any existing admin session keys only
                HttpContext.Session.Remove("AdminEmail");
                
                // Set student session
                HttpContext.Session.SetString("StudentEmail", student.Email);
                HttpContext.Session.SetString("StudentName", student.Name);
                HttpContext.Session.SetInt32("StudentId", student.Id);

                // Check if first time login
                if (student.IsFirstTimeLogin)
                {
                    TempData["Info"] = "Welcome! For security reasons, please change your password.";
                    return RedirectToAction("ChangePassword");
                }

                TempData["Success"] = $"Welcome back, {student.Name}!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during student login");
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return View();
            }
        }

        // GET: Student/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // POST: Student/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                var studentEmail = HttpContext.Session.GetString("StudentEmail");
                if (string.IsNullOrEmpty(studentEmail))
                {
                    return RedirectToAction("Login");
                }

                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    TempData["Error"] = "New password must be at least 6 characters long.";
                    return View();
                }

                if (newPassword != confirmPassword)
                {
                    TempData["Error"] = "Passwords do not match.";
                    return View();
                }

                var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
                if (student == null)
                {
                    return RedirectToAction("Login");
                }

                // Verify current password
                var currentPasswordHash = HashPassword(currentPassword);
                if (student.PasswordHash != currentPasswordHash)
                {
                    TempData["Error"] = "Current password is incorrect.";
                    return View();
                }

                // Update password
                student.PasswordHash = HashPassword(newPassword);
                student.IsFirstTimeLogin = false;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change");
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return View();
            }
        }

        // GET: Student/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return RedirectToAction("Login");
            }

            var studentName = HttpContext.Session.GetString("StudentName");
            ViewBag.StudentName = studentName;
            ViewBag.StudentId = student.PermanentID;
            ViewBag.StudentEmail = student.Email;
            
            // Get attendance count
            ViewBag.AttendanceCount = await _context.Attendances
                .Where(a => a.StudentId == student.Id)
                .CountAsync();
            
            // Get pending bills count
            ViewBag.PendingBills = await _context.Attendances
                .Where(a => a.StudentId == student.Id && a.IsPaid == false)
                .CountAsync();
            
            // Get total amount
            var totalAmount = await _context.Attendances
                .Where(a => a.StudentId == student.Id && a.IsPaid == false)
                .SumAsync(a => (decimal?)a.Amount) ?? 0;
            ViewBag.TotalAmount = totalAmount.ToString("N0");

            return View();
        }

        // GET: Student/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Logged out successfully.";
            return RedirectToAction("Login");
        }

        // GET: Student/ViewBill
        [HttpGet]
        public async Task<IActionResult> ViewBill()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToAction("Login");
            }

            var attendances = await _context.Attendances
                .Where(a => a.StudentId == student.Id)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            var unpaidAmount = attendances.Where(a => !a.IsPaid).Sum(a => a.Amount);
            var paidAmount = attendances.Where(a => a.IsPaid).Sum(a => a.Amount);

            var billDetails = new
            {
                StudentId = student.Id,
                StudentName = student.Name,
                StudentEmail = student.Email,
                PermanentID = student.PermanentID,
                Attendances = attendances,
                TotalMeals = attendances.Count,
                UnpaidMeals = attendances.Count(a => !a.IsPaid),
                PaidMeals = attendances.Count(a => a.IsPaid),
                TotalAmount = attendances.Sum(a => a.Amount),
                UnpaidAmount = unpaidAmount,
                PaidAmount = paidAmount
            };

            return View(billDetails);
        }

        // GET: Student/DigitalCard
        [HttpGet]
        public async Task<IActionResult> DigitalCard()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToAction("Login");
            }

            // Generate QR secret if not exists
            if (string.IsNullOrEmpty(student.QRCodeSecret))
            {
                student.QRCodeSecret = Guid.NewGuid().ToString("N");
                await _context.SaveChangesAsync();
            }

            // Generate QR Code with format: PermanentID|QRCodeSecret
            var qrData = $"{student.PermanentID}|{student.QRCodeSecret}";
            var qrCodeBase64 = _qrCodeService.GenerateQRCodeBase64(qrData);
            ViewBag.QRCode = qrCodeBase64;

            // Calculate current balance
            var unpaidAmount = await _context.Attendances
                .Where(a => a.StudentId == student.Id && !a.IsPaid)
                .SumAsync(a => a.Amount);
            ViewBag.CurrentBalance = unpaidAmount;

            return View(student);
        }

        // GET: Student/GetCurrentOTP (AJAX endpoint)
        [HttpGet]
        public async Task<IActionResult> GetCurrentOTP()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null || string.IsNullOrEmpty(student.CurrentCheckInOTP))
            {
                return Json(new { success = false, hasOTP = false });
            }

            // Check if OTP is still valid (5 minutes)
            if (student.OTPGeneratedAt.HasValue && 
                DateTime.Now.Subtract(student.OTPGeneratedAt.Value).TotalMinutes <= 5)
            {
                return Json(new
                {
                    success = true,
                    hasOTP = true,
                    otp = student.CurrentCheckInOTP,
                    generatedAt = student.OTPGeneratedAt.Value.ToString("hh:mm:ss tt")
                });
            }

            return Json(new { success = false, hasOTP = false });
        }

        // GET: Student/PaymentHistory
        [HttpGet]
        public async Task<IActionResult> PaymentHistory()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return RedirectToAction("Login");
            }

            var payments = await _context.Payments
                .Where(p => p.StudentId == student.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.StudentName = student.Name;
            ViewBag.TotalPaid = payments.Sum(p => p.Amount);

            return View(payments);
        }

        // GET: Student/MyAttendance
        [HttpGet]
        public async Task<IActionResult> MyAttendance()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return RedirectToAction("Login");
            }

            var attendances = await _context.Attendances
                .Where(a => a.StudentId == student.Id)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            ViewBag.StudentName = student.Name;
            ViewBag.TotalMeals = attendances.Count;
            ViewBag.TotalAmount = attendances.Sum(a => a.Amount);
            ViewBag.PaidMeals = attendances.Count(a => a.IsPaid);
            ViewBag.UnpaidMeals = attendances.Count(a => !a.IsPaid);

            return View(attendances);
        }

        // GET: Student/ViewMenu
        public async Task<IActionResult> ViewMenu()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return RedirectToAction("Login");
            }

            // Get current day of week
            var currentDay = DateTime.Now.DayOfWeek.ToString();

            // Filter menu items based on membership type
            // TeaWater members: Only see Breakfast (tea/water items)
            // FullMeal members: See all meal types
            var menuQuery = _context.MenuItems.Where(m => m.IsActive);
            
            if (student.MembershipType == "TeaWater")
            {
                // TeaWater members only see Breakfast items (tea, water, light snacks)
                menuQuery = menuQuery.Where(m => m.MealType == "Breakfast");
            }

            var menuItems = await menuQuery
                .OrderBy(m => m.DayOfWeek)
                .ThenBy(m => m.MealType)
                .ToListAsync();

            ViewBag.StudentName = student.Name;
            ViewBag.MembershipType = student.MembershipType;
            ViewBag.CurrentDay = currentDay;

            return View(menuItems);
        }

        // GET: Student/MyProfile
        public async Task<IActionResult> MyProfile()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return RedirectToAction("Login");
            }

            return View(student);
        }

        // POST: Student/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string name, string email)
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return Json(new { success = false, message = "Student not found" });
            }

            try
            {
                // Check if new email is different and already exists
                if (email != student.Email)
                {
                    if (await _context.Students.AnyAsync(s => s.Email == email && s.Id != student.Id))
                    {
                        return Json(new { success = false, message = "Email already exists" });
                    }
                    student.Email = email;
                    HttpContext.Session.SetString("StudentEmail", email); // Update session
                }

                student.Name = name;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Profile updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return Json(new { success = false, message = "Error updating profile" });
            }
        }

        // GET: Student/Complaints
        public async Task<IActionResult> Complaints()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return RedirectToAction("Login");
            }

            var complaints = await _context.Complaints
                .Where(c => c.StudentId == student.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.StudentName = student.Name;

            return View(complaints);
        }

        // POST: Student/SubmitComplaint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComplaint(string subject, string description)
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return Json(new { success = false, message = "Student not found" });
            }

            try
            {
                var complaint = new Complaint
                {
                    StudentId = student.Id,
                    Subject = subject,
                    Description = description,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.Complaints.Add(complaint);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Complaint submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting complaint");
                return Json(new { success = false, message = "Error submitting complaint" });
            }
        }

        // GET: Student/Announcements
        public async Task<IActionResult> Announcements()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return RedirectToAction("Login");
            }

            var announcements = await _context.Announcements
                .Where(a => a.IsActive && 
                           (a.ExpiresAt == null || a.ExpiresAt > DateTime.Now) &&
                           (a.VisibleFrom == null || a.VisibleFrom <= DateTime.Now) &&
                           (a.VisibleTo == null || a.VisibleTo >= DateTime.Now))
                .OrderByDescending(a => a.Priority == "Urgent")
                .ThenByDescending(a => a.Priority == "High")
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.StudentName = student.Name;

            return View(announcements);
        }

        // POST: Student/DeleteComplaint
        [HttpPost]
        public async Task<IActionResult> DeleteComplaint(int id)
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return Json(new { success = false, message = "Session expired" });
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return Json(new { success = false, message = "Student not found" });
            }

            try
            {
                var complaint = await _context.Complaints
                    .FirstOrDefaultAsync(c => c.Id == id && c.StudentId == student.Id);

                if (complaint == null)
                {
                    return Json(new { success = false, message = "Complaint not found" });
                }

                // Only allow deletion if not resolved or closed
                if (complaint.Status == "Resolved" || complaint.Status == "Closed")
                {
                    return Json(new { success = false, message = "Cannot delete resolved or closed complaints" });
                }

                _context.Complaints.Remove(complaint);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Complaint deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting complaint");
                return Json(new { success = false, message = "Error deleting complaint" });
            }
        }

        // GET: Student/BillRecheckRequests
        [HttpGet]
        public async Task<IActionResult> BillRecheckRequests()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return NotFound();
            }

            var requests = await _context.BillRecheckRequests
                .Where(r => r.StudentId == student.Id)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            ViewBag.StudentName = student.Name;
            return View(requests);
        }

        // GET: Student/SubmitBillRecheck
        [HttpGet]
        public async Task<IActionResult> SubmitBillRecheck()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return NotFound();
            }

            ViewBag.StudentName = student.Name;
            return View();
        }

        // POST: Student/SubmitBillRecheck
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitBillRecheck(int month, int year, string requestMessage)
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return NotFound();
            }

            // Validate inputs
            if (month < 1 || month > 12 || year < 2020 || year > DateTime.Now.Year + 1)
            {
                TempData["Error"] = "Invalid month or year selected.";
                return RedirectToAction("SubmitBillRecheck");
            }

            if (string.IsNullOrWhiteSpace(requestMessage))
            {
                TempData["Error"] = "Please provide a detailed message for your request.";
                return RedirectToAction("SubmitBillRecheck");
            }

            // Check if already exists for same month/year
            var existingRequest = await _context.BillRecheckRequests
                .FirstOrDefaultAsync(r => r.StudentId == student.Id && r.Month == month && r.Year == year && r.Status == "Pending");

            if (existingRequest != null)
            {
                TempData["Error"] = "You already have a pending recheck request for this month/year.";
                return RedirectToAction("BillRecheckRequests");
            }

            var request = new BillRecheckRequest
            {
                StudentId = student.Id,
                Month = month,
                Year = year,
                RequestMessage = requestMessage,
                Status = "Pending",
                RequestedAt = DateTime.Now
            };

            _context.BillRecheckRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your bill recheck request has been submitted successfully. Admin will review it soon.";
            return RedirectToAction("BillRecheckRequests");
        }

        // GET: Student/DeleteAccount
        [HttpGet]
        public IActionResult DeleteAccount()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // POST: Student/DeleteAccountConfirm
        [HttpPost]
        public async Task<IActionResult> DeleteAccountConfirm(string password)
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login");
            }

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
            if (student == null)
            {
                return NotFound();
            }

            // Verify password
            var hashedPassword = HashPassword(password);
            if (student.PasswordHash != hashedPassword)
            {
                TempData["Error"] = "Incorrect password. Account deletion failed.";
                return RedirectToAction("DeleteAccount");
            }

            // Check for unpaid bills
            var unpaidAttendances = await _context.Attendances
                .Where(a => a.StudentId == student.Id && !a.IsPaid)
                .ToListAsync();

            if (unpaidAttendances.Any())
            {
                var totalUnpaid = unpaidAttendances.Sum(a => a.Amount);
                TempData["Error"] = $"Cannot delete account. You have unpaid bills totaling PKR {totalUnpaid}. Please clear your dues first.";
                return RedirectToAction("DeleteAccount");
            }

            // Delete all related records
            var attendances = await _context.Attendances.Where(a => a.StudentId == student.Id).ToListAsync();
            _context.Attendances.RemoveRange(attendances);

            var payments = await _context.Payments.Where(p => p.StudentId == student.Id).ToListAsync();
            _context.Payments.RemoveRange(payments);

            var complaints = await _context.Complaints.Where(c => c.StudentId == student.Id).ToListAsync();
            _context.Complaints.RemoveRange(complaints);

            var recheckRequests = await _context.BillRecheckRequests.Where(r => r.StudentId == student.Id).ToListAsync();
            _context.BillRecheckRequests.RemoveRange(recheckRequests);

            // Delete student account
            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            // Clear session
            HttpContext.Session.Clear();

            TempData["Success"] = "Your account has been permanently deleted.";
            return RedirectToAction("Login");
        }

        // Helper method to generate unique PermanentID
        private async Task<string> GenerateUniquePermanentID()
        {
            var year = DateTime.Now.Year;
            var random = new Random();
            string permanentId;

            do
            {
                var randomNumber = random.Next(1000, 9999);
                permanentId = $"UET-{year}-{randomNumber}";
            }
            while (await _context.Students.AnyAsync(s => s.PermanentID == permanentId));

            return permanentId;
        }

        // Helper method to generate random password
        private string GenerateRandomPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
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
