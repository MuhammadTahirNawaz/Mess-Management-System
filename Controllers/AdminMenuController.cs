using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester_Project.Data;
using Semester_Project.Models;

namespace Semester_Project.Controllers
{
    public class AdminMenuController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminMenuController> _logger;

        public AdminMenuController(ApplicationDbContext context, ILogger<AdminMenuController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Check if admin is logged in
        private bool IsAdminLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("AdminEmail"));
        }

        // GET: AdminMenu/MenuManagement
        public async Task<IActionResult> MenuManagement()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var menuItems = await _context.MenuItems
                .OrderBy(m => m.DayOfWeek)
                .ThenBy(m => m.MealType)
                .ToListAsync();

            return View(menuItems);
        }

        // GET: AdminMenu/AddMenuItem
        public IActionResult AddMenuItem()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            return View();
        }

        // POST: AdminMenu/AddMenuItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMenuItem(MenuItem menuItem)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    menuItem.CreatedAt = DateTime.Now;
                    _context.MenuItems.Add(menuItem);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Menu item added successfully!";
                    return RedirectToAction("MenuManagement");
                }

                return View(menuItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding menu item");
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return View(menuItem);
            }
        }

        // GET: AdminMenu/EditMenuItem/5
        public async Task<IActionResult> EditMenuItem(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null)
            {
                TempData["Error"] = "Menu item not found.";
                return RedirectToAction("MenuManagement");
            }

            return View(menuItem);
        }

        // POST: AdminMenu/EditMenuItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMenuItem(MenuItem menuItem)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    menuItem.UpdatedAt = DateTime.Now;
                    _context.Update(menuItem);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Menu item updated successfully!";
                    return RedirectToAction("MenuManagement");
                }

                return View(menuItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating menu item");
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return View(menuItem);
            }
        }

        // POST: AdminMenu/DeleteMenuItem/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            try
            {
                var menuItem = await _context.MenuItems.FindAsync(id);
                if (menuItem != null)
                {
                    _context.MenuItems.Remove(menuItem);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Menu item deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Menu item not found.";
                }

                return RedirectToAction("MenuManagement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting menu item");
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("MenuManagement");
            }
        }

        // GET: AdminMenu/ViewStudents
        public async Task<IActionResult> ViewStudents()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var students = await _context.Students
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(students);
        }

        // POST: AdminMenu/DeleteStudent
        [HttpPost]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            try
            {
                var student = await _context.Students.FindAsync(id);

                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found." });
                }

                // Remove all attendance records for this student first
                var attendances = await _context.Attendances
                    .Where(a => a.StudentId == id)
                    .ToListAsync();
                
                if (attendances.Any())
                {
                    _context.Attendances.RemoveRange(attendances);
                }

                // Remove the student
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin deleted student: {student.Name} (ID: {student.PermanentID})");
                return Json(new { success = true, message = $"Student {student.Name} has been successfully removed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting student with ID: {id}");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // GET: AdminMenu/MarkAttendance
        public async Task<IActionResult> MarkAttendance()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var students = await _context.Students
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.TodayDate = DateTime.Today.ToString("yyyy-MM-dd");
            ViewBag.DayOfWeek = DateTime.Today.DayOfWeek.ToString();

            return View(students);
        }

        // POST: AdminMenu/SaveAttendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAttendance(string date, string mealType, List<int> studentIds)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            try
            {
                if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(mealType))
                {
                    TempData["Error"] = "Date and meal type are required.";
                    return RedirectToAction("MarkAttendance");
                }

                var attendanceDate = DateTime.Parse(date);
                var dayOfWeek = attendanceDate.DayOfWeek.ToString();

                // Get menu price for this meal
                var menuItem = await _context.MenuItems
                    .Where(m => m.DayOfWeek == dayOfWeek && m.MealType == mealType && m.IsActive)
                    .FirstOrDefaultAsync();

                if (menuItem == null)
                {
                    TempData["Error"] = $"No menu item found for {mealType} on {dayOfWeek}.";
                    return RedirectToAction("MarkAttendance");
                }

                // Check if attendance already exists for this date and meal
                var existingAttendance = await _context.Attendances
                    .Where(a => a.Date.Date == attendanceDate.Date && a.MealType == mealType)
                    .ToListAsync();

                if (existingAttendance.Any())
                {
                    // Remove existing attendance for this date and meal
                    _context.Attendances.RemoveRange(existingAttendance);
                }

                // Add new attendance records
                if (studentIds != null && studentIds.Any())
                {
                    foreach (var studentId in studentIds)
                    {
                        var attendance = new Attendance
                        {
                            StudentId = studentId,
                            Date = attendanceDate,
                            MealType = mealType,
                            Amount = menuItem.Price,
                            CreatedAt = DateTime.Now
                        };

                        _context.Attendances.Add(attendance);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Attendance marked successfully for {studentIds?.Count ?? 0} student(s)!";
                return RedirectToAction("MarkAttendance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving attendance");
                TempData["Error"] = $"An error occurred: {ex.Message}";
                return RedirectToAction("MarkAttendance");
            }
        }

        // GET: AdminMenu/ViewBills
        public async Task<IActionResult> ViewBills()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var billData = await _context.Students
                .Select(s => new
                {
                    StudentId = s.Id,
                    StudentName = s.Name,
                    StudentEmail = s.Email,
                    PermanentID = s.PermanentID,
                    TotalAmount = _context.Attendances
                        .Where(a => a.StudentId == s.Id && !a.IsPaid)
                        .Sum(a => (decimal?)a.Amount) ?? 0,
                    TotalMeals = _context.Attendances
                        .Where(a => a.StudentId == s.Id && !a.IsPaid)
                        .Count()
                })
                .Where(b => b.TotalAmount > 0)
                .OrderByDescending(b => b.TotalAmount)
                .ToListAsync();

            return View(billData);
        }

        // GET: AdminMenu/StudentBillDetails/5
        public async Task<IActionResult> StudentBillDetails(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToAction("ViewBills");
            }

            var attendances = await _context.Attendances
                .Where(a => a.StudentId == id)
                .OrderByDescending(a => a.Date)
                .ToListAsync();

            var unpaidAmount = attendances.Where(a => !a.IsPaid).Sum(a => a.Amount);
            var paidAmount = attendances.Where(a => a.IsPaid).Sum(a => a.Amount);

            var billDetails = new StudentBillDetailsViewModel
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

        // POST: AdminMenu/MarkAsPaid
        [HttpPost]
        public async Task<IActionResult> MarkAsPaid(int studentId)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            try
            {
                var unpaidAttendances = await _context.Attendances
                    .Where(a => a.StudentId == studentId && !a.IsPaid)
                    .ToListAsync();

                if (!unpaidAttendances.Any())
                {
                    return Json(new { success = false, message = "No unpaid bills found for this student." });
                }

                var totalAmount = unpaidAttendances.Sum(a => a.Amount);

                foreach (var attendance in unpaidAttendances)
                {
                    attendance.IsPaid = true;
                    attendance.PaidAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Admin marked bills as paid for student ID {studentId}. Total: PKR {totalAmount}");

                return Json(new
                {
                    success = true,
                    message = $"Payment of PKR {totalAmount:N2} marked as received successfully!",
                    totalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking bills as paid for student ID {studentId}");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // GET: AdminMenu/CheckIn
        public IActionResult CheckIn()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            return View();
        }

        // POST: AdminMenu/VerifyStudent (Step 1: Enter Permanent ID)
        [HttpPost]
        public async Task<IActionResult> VerifyStudent(string permanentId)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            if (string.IsNullOrWhiteSpace(permanentId))
            {
                return Json(new { success = false, message = "Please enter a Permanent ID." });
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.PermanentID == permanentId.Trim());

            if (student == null)
            {
                return Json(new { success = false, message = "Student not found with this Permanent ID." });
            }

            _logger.LogInformation($"Step 1 completed: Student {student.Name} ({student.PermanentID}) verified by ID");

            // Get unpaid balance
            var unpaidBalance = await _context.Attendances
                .Where(a => a.StudentId == student.Id && !a.IsPaid)
                .SumAsync(a => a.Amount);

            // Get today's attendance count
            var todayAttendance = await _context.Attendances
                .CountAsync(a => a.StudentId == student.Id && a.Date.Date == DateTime.Today);

            return Json(new
            {
                success = true,
                requiresQRScan = true,
                student = new
                {
                    id = student.Id,
                    name = student.Name,
                    permanentId = student.PermanentID,
                    email = student.Email,
                    balance = unpaidBalance,
                    todayMeals = todayAttendance,
                    isVerified = student.IsVerified
                }
            });
        }

        // POST: AdminMenu/VerifyQRCode (Step 2: Scan QR Code)
        [HttpPost]
        public async Task<IActionResult> VerifyQRCode(int studentId, string qrSecret)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            if (string.IsNullOrWhiteSpace(qrSecret))
            {
                return Json(new { success = false, message = "Invalid QR code data." });
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                return Json(new { success = false, message = "Student not found." });
            }

            // Verify QR secret matches
            if (student.QRCodeSecret != qrSecret.Trim())
            {
                _logger.LogWarning($"QR verification failed for student {student.PermanentID}. Possible counterfeit card.");
                return Json(new { success = false, message = "QR code verification failed. This may be a counterfeit card." });
            }

            // Generate 4-digit OTP for final verification
            var otp = GenerateCheckInOTP();
            student.CurrentCheckInOTP = otp;
            student.OTPGeneratedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Step 2 completed: QR verified. OTP {otp} generated for {student.Name} ({student.PermanentID})");

            return Json(new
            {
                success = true,
                message = "QR code verified! OTP sent to student's device.",
                requiresOTP = true
            });
        }

        // POST: AdminMenu/ProcessCheckIn (Step 3: Verify OTP and Complete Check-in)
        [HttpPost]
        public async Task<IActionResult> ProcessCheckIn(int studentId, string mealType, string otp)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            try
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found." });
                }

                // Verify OTP (Step 3)
                if (string.IsNullOrWhiteSpace(otp) || student.CurrentCheckInOTP != otp.Trim())
                {
                    return Json(new { success = false, message = "Invalid OTP. Please ask student for correct code." });
                }

                // Check if OTP is expired (5 minutes)
                if (!student.OTPGeneratedAt.HasValue || 
                    DateTime.Now.Subtract(student.OTPGeneratedAt.Value).TotalMinutes > 5)
                {
                    return Json(new { success = false, message = "OTP expired. Please start check-in process again." });
                }

                // Check if already checked in for this meal today
                var existingAttendance = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.StudentId == studentId &&
                                            a.Date.Date == DateTime.Today &&
                                            a.MealType == mealType);

                if (existingAttendance != null)
                {
                    return Json(new { success = false, message = $"Student already checked in for {mealType} today." });
                }

                // Get menu item for today and meal type
                var dayOfWeek = DateTime.Today.DayOfWeek.ToString();
                var menuItem = await _context.MenuItems
                    .FirstOrDefaultAsync(m => m.DayOfWeek == dayOfWeek &&
                                            m.MealType == mealType &&
                                            m.IsActive);

                if (menuItem == null)
                {
                    return Json(new { success = false, message = $"No menu item found for {mealType} today." });
                }

                // Create attendance record
                var attendance = new Attendance
                {
                    StudentId = studentId,
                    Date = DateTime.Today,
                    MealType = mealType,
                    Amount = menuItem.Price,
                    IsPaid = false,
                    CreatedAt = DateTime.Now
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();

                // Clear the OTP after successful check-in
                student.CurrentCheckInOTP = null;
                student.OTPGeneratedAt = null;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Check-in successful for {mealType}!",
                    amount = menuItem.Price,
                    menuItem = menuItem.ItemName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing check-in");
                return Json(new { success = false, message = "An error occurred during check-in." });
            }
        }

        // GET: AdminMenu/ManageComplaints
        public async Task<IActionResult> ManageComplaints()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var complaints = await _context.Complaints
                .Include(c => c.Student)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(complaints);
        }

        // POST: AdminMenu/UpdateComplaintStatus
        [HttpPost]
        public async Task<IActionResult> UpdateComplaintStatus(int id, string status, string? response)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var complaint = await _context.Complaints.FindAsync(id);
                if (complaint == null)
                {
                    return Json(new { success = false, message = "Complaint not found" });
                }

                complaint.Status = status;
                complaint.AdminResponse = response;
                
                if (status == "Resolved" || status == "Closed")
                {
                    complaint.ResolvedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Complaint status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating complaint status");
                return Json(new { success = false, message = "Error updating complaint status" });
            }
        }

        // GET: AdminMenu/ManageAnnouncements
        public async Task<IActionResult> ManageAnnouncements()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var announcements = await _context.Announcements
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(announcements);
        }

        // POST: AdminMenu/CreateAnnouncement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(string title, string content, string priority, string expiresAt, string visibleFrom, string visibleTo)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var adminEmail = HttpContext.Session.GetString("AdminEmail");
                var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

                DateTime? expiryDate = null;
                if (!string.IsNullOrEmpty(expiresAt))
                {
                    if (DateTime.TryParse(expiresAt, out DateTime parsedDate))
                    {
                        expiryDate = parsedDate;
                    }
                }

                DateTime? visibleFromDate = null;
                if (!string.IsNullOrEmpty(visibleFrom))
                {
                    if (DateTime.TryParse(visibleFrom, out DateTime parsedDate))
                    {
                        visibleFromDate = parsedDate;
                    }
                }

                DateTime? visibleToDate = null;
                if (!string.IsNullOrEmpty(visibleTo))
                {
                    if (DateTime.TryParse(visibleTo, out DateTime parsedDate))
                    {
                        visibleToDate = parsedDate;
                    }
                }

                var announcement = new Announcement
                {
                    Title = title,
                    Content = content,
                    Priority = priority,
                    ExpiresAt = expiryDate,
                    VisibleFrom = visibleFromDate,
                    VisibleTo = visibleToDate,
                    IsActive = true,
                    CreatedByUserId = admin?.Id ?? 0,
                    CreatedAt = DateTime.Now
                };

                _context.Announcements.Add(announcement);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Announcement created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating announcement: {Message}", ex.Message);
                return Json(new { success = false, message = $"Error creating announcement: {ex.Message}" });
            }
        }

        // POST: AdminMenu/ToggleAnnouncementStatus
        [HttpPost]
        public async Task<IActionResult> ToggleAnnouncementStatus(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var announcement = await _context.Announcements.FindAsync(id);
                if (announcement == null)
                {
                    return Json(new { success = false, message = "Announcement not found" });
                }

                announcement.IsActive = !announcement.IsActive;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Announcement status updated", isActive = announcement.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating announcement status");
                return Json(new { success = false, message = "Error updating announcement status" });
            }
        }

        // POST: AdminMenu/UpdateAnnouncement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAnnouncement(int id, string title, string content, string priority, string expiresAt, string visibleFrom, string visibleTo)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var announcement = await _context.Announcements.FindAsync(id);
                if (announcement == null)
                {
                    return Json(new { success = false, message = "Announcement not found" });
                }

                DateTime? expiryDate = null;
                if (!string.IsNullOrEmpty(expiresAt))
                {
                    if (DateTime.TryParse(expiresAt, out DateTime parsedDate))
                    {
                        expiryDate = parsedDate;
                    }
                }

                DateTime? visibleFromDate = null;
                if (!string.IsNullOrEmpty(visibleFrom))
                {
                    if (DateTime.TryParse(visibleFrom, out DateTime parsedDate))
                    {
                        visibleFromDate = parsedDate;
                    }
                }

                DateTime? visibleToDate = null;
                if (!string.IsNullOrEmpty(visibleTo))
                {
                    if (DateTime.TryParse(visibleTo, out DateTime parsedDate))
                    {
                        visibleToDate = parsedDate;
                    }
                }

                announcement.Title = title;
                announcement.Content = content;
                announcement.Priority = priority;
                announcement.ExpiresAt = expiryDate;
                announcement.VisibleFrom = visibleFromDate;
                announcement.VisibleTo = visibleToDate;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Announcement updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating announcement: {Message}", ex.Message);
                return Json(new { success = false, message = $"Error updating announcement: {ex.Message}" });
            }
        }

        // POST: AdminMenu/DeleteAnnouncement
        [HttpPost]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                var announcement = await _context.Announcements.FindAsync(id);
                if (announcement == null)
                {
                    return Json(new { success = false, message = "Announcement not found" });
                }

                _context.Announcements.Remove(announcement);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Announcement deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting announcement");
                return Json(new { success = false, message = "Error deleting announcement" });
            }
        }

        // Helper method to generate 4-digit OTP
        private string GenerateCheckInOTP()
        {
            Random random = new Random();
            return random.Next(1000, 9999).ToString();
        }
    }
}
