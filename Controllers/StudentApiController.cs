using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester_Project.Data;
using Semester_Project.Models;
using Semester_Project.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Semester_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;

        public StudentApiController(ApplicationDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // POST: api/StudentApi/Login
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { success = false, message = "Email and password are required" });
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == request.Email);

            if (student == null)
            {
                return Unauthorized(new { success = false, message = "Invalid email or password" });
            }

            // Verify password
            var passwordHash = HashPassword(request.Password);
            if (student.PasswordHash != passwordHash)
            {
                return Unauthorized(new { success = false, message = "Invalid email or password" });
            }

            // Generate JWT token
            var token = _jwtService.GenerateToken(student.Email, "Student", student.Id);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                token = token,
                student = new
                {
                    id = student.Id,
                    name = student.Name,
                    email = student.Email,
                    permanentId = student.PermanentID
                }
            });
        }

        // GET: api/StudentApi/Profile
        [Authorize(Roles = "Student")]
        [HttpGet("Profile")]
        public async Task<IActionResult> GetProfile()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { success = false, message = "Invalid token" });
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == email);

            if (student == null)
            {
                return NotFound(new { success = false, message = "Student not found" });
            }

            return Ok(new
            {
                success = true,
                student = new
                {
                    id = student.Id,
                    name = student.Name,
                    email = student.Email,
                    permanentId = student.PermanentID,
                    isVerified = student.IsVerified,
                    isFirstTimeLogin = student.IsFirstTimeLogin,
                    createdAt = student.CreatedAt,
                    lastLoginAt = student.LastLoginAt
                }
            });
        }

        // GET: api/StudentApi/Attendance
        [Authorize(Roles = "Student")]
        [HttpGet("Attendance")]
        public async Task<IActionResult> GetAttendance()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { success = false, message = "Invalid token" });
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == email);

            if (student == null)
            {
                return NotFound(new { success = false, message = "Student not found" });
            }

            var attendances = await _context.Attendances
                .Where(a => a.StudentId == student.Id)
                .OrderByDescending(a => a.Date)
                .Select(a => new
                {
                    id = a.Id,
                    date = a.Date,
                    amount = a.Amount,
                    mealType = a.MealType,
                    isPaid = a.IsPaid,
                    paidAt = a.PaidAt
                })
                .ToListAsync();

            var totalAmount = attendances.Sum(a => a.amount);

            return Ok(new
            {
                success = true,
                totalAmount = totalAmount,
                count = attendances.Count,
                attendances = attendances
            });
        }

        // GET: api/StudentApi/Announcements
        [Authorize(Roles = "Student")]
        [HttpGet("Announcements")]
        public async Task<IActionResult> GetAnnouncements()
        {
            var now = DateTime.Now;
            
            var announcements = await _context.Announcements
                .Where(a => a.IsActive && 
                           (a.ExpiresAt == null || a.ExpiresAt > now) &&
                           (a.VisibleFrom == null || a.VisibleFrom <= now) &&
                           (a.VisibleTo == null || a.VisibleTo >= now))
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    id = a.Id,
                    title = a.Title,
                    content = a.Content,
                    priority = a.Priority,
                    createdAt = a.CreatedAt,
                    expiresAt = a.ExpiresAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = announcements.Count,
                announcements = announcements
            });
        }

        // POST: api/StudentApi/Complaint
        [Authorize(Roles = "Student")]
        [HttpPost("Complaint")]
        public async Task<IActionResult> SubmitComplaint([FromBody] ComplaintRequest request)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { success = false, message = "Invalid token" });
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == email);

            if (student == null)
            {
                return NotFound(new { success = false, message = "Student not found" });
            }

            if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest(new { success = false, message = "Subject and description are required" });
            }

            var complaint = new Complaint
            {
                StudentId = student.Id,
                Subject = request.Subject,
                Description = request.Description,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Complaint submitted successfully",
                complaint = new
                {
                    id = complaint.Id,
                    subject = complaint.Subject,
                    description = complaint.Description,
                    status = complaint.Status,
                    createdAt = complaint.CreatedAt
                }
            });
        }

        // Helper method for password hashing (matches StudentController)
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }

    // Request models
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ComplaintRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
