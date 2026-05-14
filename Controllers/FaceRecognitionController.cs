using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester_Project.Data;
using Semester_Project.Services;

namespace Semester_Project.Controllers
{
    public class FaceRecognitionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFaceRecognitionService _faceService;
        private readonly ILogger<FaceRecognitionController> _logger;

        public FaceRecognitionController(
            ApplicationDbContext context,
            IFaceRecognitionService faceService,
            ILogger<FaceRecognitionController> logger)
        {
            _context = context;
            _faceService = faceService;
            _logger = logger;
        }

        // GET: FaceRecognition/RegisterFace
        [HttpGet]
        public IActionResult RegisterFace()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login", "Student");
            }

            return View();
        }

        // POST: FaceRecognition/RegisterFace
        [HttpPost]
        public async Task<IActionResult> RegisterFaceData([FromBody] FaceDataModel model)
        {
            try
            {
                var studentEmail = HttpContext.Session.GetString("StudentEmail");
                if (string.IsNullOrEmpty(studentEmail))
                {
                    return Json(new { success = false, message = "Not logged in" });
                }

                var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == studentEmail);
                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found" });
                }

                // Encode face from image
                var encoding = await _faceService.EncodeFaceFromBase64(model.ImageData);
                if (encoding == null)
                {
                    return Json(new { success = false, message = "No face detected. Please ensure your face is clearly visible." });
                }

                // Check if this face already belongs to another student
                var allStudents = await _context.Students
                    .Where(s => s.IsFaceRegistered && s.FaceEncoding != null && s.Id != student.Id)
                    .ToListAsync();

                foreach (var otherStudent in allStudents)
                {
                    var isSameFace = await _faceService.CompareFaces(encoding, otherStudent.FaceEncoding!, 0.85);
                    if (isSameFace)
                    {
                        return Json(new 
                        { 
                            success = false, 
                            message = $"This face is already registered to another student ({otherStudent.Name}). Each face must be unique." 
                        });
                    }
                }

                // Save face encoding
                student.FaceEncoding = encoding;
                student.IsFaceRegistered = true;
                student.FaceRegisteredAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Face registered successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering face");
                return Json(new { success = false, message = "Error registering face. Please try again." });
            }
        }

        // GET: FaceRecognition/ScanAttendance
        [HttpGet]
        public IActionResult ScanAttendance()
        {
            return View();
        }

        // POST: FaceRecognition/VerifyFace
        [HttpPost]
        public async Task<IActionResult> VerifyFace([FromBody] FaceDataModel model)
        {
            try
            {
                // Recognize student from face
                var studentId = await _faceService.RecognizeStudentFromImage(model.ImageData);
                
                if (studentId == null)
                {
                    return Json(new { success = false, message = "Face not recognized. Please try again or register your face first." });
                }

                var student = await _context.Students
                    .Include(s => s.Attendances)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found" });
                }

                // Check if already marked attendance today
                var today = DateTime.Today;
                var existingAttendance = student.Attendances
                    .FirstOrDefault(a => a.Date.Date == today);

                if (existingAttendance != null)
                {
                    return Json(new 
                    { 
                        success = false, 
                        message = $"Attendance already marked for {student.Name} today at {existingAttendance.Date:hh:mm tt}" 
                    });
                }

                // Calculate amount based on membership type
                decimal amount = student.MembershipType == "TeaWater" ? 50 : 150;

                // Mark attendance
                var attendance = new Models.Attendance
                {
                    StudentId = student.Id,
                    Date = DateTime.Now,
                    Amount = amount,
                    IsPaid = false
                };

                _context.Attendances.Add(attendance);
                await _context.SaveChangesAsync();

                return Json(new 
                { 
                    success = true, 
                    message = $"Welcome {student.Name}! Attendance marked successfully.",
                    studentName = student.Name,
                    permanentId = student.PermanentID,
                    amount = amount,
                    membershipType = student.MembershipType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying face");
                return Json(new { success = false, message = "Error verifying face. Please try again." });
            }
        }

        // GET: FaceRecognition/AdminScan
        [HttpGet]
        public IActionResult AdminScan()
        {
            var adminEmail = HttpContext.Session.GetString("AdminEmail");
            if (string.IsNullOrEmpty(adminEmail))
            {
                return RedirectToAction("Login", "Admin");
            }

            return View();
        }
    }

    public class FaceDataModel
    {
        public string ImageData { get; set; } = string.Empty;
    }
}
