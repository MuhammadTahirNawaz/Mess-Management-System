using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Semester_Project.Data;
using Semester_Project.Models;

namespace Semester_Project.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            
            // Only set Stripe API key if it's a real key (not placeholder)
            var stripeKey = _configuration["Stripe:SecretKey"];
            if (!string.IsNullOrEmpty(stripeKey) && stripeKey.StartsWith("sk_test_"))
            {
                StripeConfiguration.ApiKey = stripeKey;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login", "Student");
            }

            var student = await _context.Students
                .Include(s => s.Attendances)
                .FirstOrDefaultAsync(s => s.Email == studentEmail);

            if (student == null)
            {
                return NotFound();
            }

            // Get unpaid attendances
            var unpaidAttendances = student.Attendances
                .Where(a => !a.IsPaid)
                .ToList();

            if (!unpaidAttendances.Any())
            {
                TempData["Message"] = "You have no pending payments.";
                return RedirectToAction("Dashboard", "Student");
            }

            // Calculate total amount
            var totalAmount = unpaidAttendances.Sum(a => a.Amount);

            ViewBag.TotalAmount = totalAmount;
            ViewBag.UnpaidCount = unpaidAttendances.Count();
            ViewBag.StudentName = student.Name;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCheckoutSession()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return Json(new { success = false, message = "Please login first" });
            }

            var student = await _context.Students
                .Include(s => s.Attendances)
                .FirstOrDefaultAsync(s => s.Email == studentEmail);

            if (student == null)
            {
                return Json(new { success = false, message = "Student not found" });
            }

            // Get unpaid attendances
            var unpaidAttendances = student.Attendances
                .Where(a => !a.IsPaid)
                .ToList();

            if (!unpaidAttendances.Any())
            {
                return Json(new { success = false, message = "No pending payments" });
            }

            // Calculate total amount
            var totalAmount = unpaidAttendances.Sum(a => a.Amount);

            // Create payment record
            var payment = new Semester_Project.Models.Payment
            {
                StudentId = student.Id,
                Amount = totalAmount,
                PaymentStatus = "Pending",
                Description = $"Payment for {unpaidAttendances.Count()} attendance(s)",
                CreatedAt = DateTime.Now,
                AttendanceIds = string.Join(",", unpaidAttendances.Select(a => a.Id)),
                StripePaymentIntentId = $"mock_{Guid.NewGuid().ToString().Substring(0, 8)}"
            };

            try
            {
                // Check if Stripe is configured with real keys
                var stripeKey = _configuration["Stripe:SecretKey"];
                var useStripe = !string.IsNullOrEmpty(stripeKey) && stripeKey.StartsWith("sk_test_");

                if (useStripe)
                {
                    // Real Stripe integration
                    var domain = $"{Request.Scheme}://{Request.Host}";
                    var amountInCents = (long)(totalAmount * 100);

                    var options = new SessionCreateOptions
                    {
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = new List<SessionLineItemOptions>
                        {
                            new SessionLineItemOptions
                            {
                                PriceData = new SessionLineItemPriceDataOptions
                                {
                                    Currency = "pkr",
                                    ProductData = new SessionLineItemPriceDataProductDataOptions
                                    {
                                        Name = "Mess Attendance Payment",
                                        Description = $"Payment for {unpaidAttendances.Count()} attendance(s)"
                                    },
                                    UnitAmount = amountInCents,
                                },
                                Quantity = 1,
                            },
                        },
                        Mode = "payment",
                        SuccessUrl = $"{domain}/Payment/Success?session_id={{CHECKOUT_SESSION_ID}}",
                        CancelUrl = $"{domain}/Payment/Cancel",
                        CustomerEmail = student.Email,
                        Metadata = new Dictionary<string, string>
                        {
                            { "student_id", student.Id.ToString() },
                            { "attendance_ids", payment.AttendanceIds }
                        }
                    };

                    var service = new SessionService();
                    Session session = await service.CreateAsync(options);

                    payment.StripePaymentIntentId = session.Id;
                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, sessionId = session.Id, useStripe = true });
                }
                else
                {
                    // Mock payment - simulate instant success
                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, paymentId = payment.Id, useStripe = false });
                }
            }
            catch (StripeException ex)
            {
                payment.PaymentStatus = "Failed";
                payment.ErrorMessage = ex.Message;
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
            {
                return RedirectToAction("Dashboard", "Student");
            }

            try
            {
                var service = new SessionService();
                Session session = await service.GetAsync(session_id);

                if (session.PaymentStatus == "paid")
                {
                    // Update payment record
                    var payment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.StripePaymentIntentId == session_id);

                    if (payment != null)
                    {
                        payment.PaymentStatus = "Succeeded";
                        payment.PaidAt = DateTime.Now;
                        payment.CardBrand = session.PaymentMethodTypes.FirstOrDefault();
                        
                        // Update attendance records
                        if (!string.IsNullOrEmpty(payment.AttendanceIds))
                        {
                            var attendanceIds = payment.AttendanceIds.Split(',').Select(int.Parse).ToList();
                            var attendances = await _context.Attendances
                                .Where(a => attendanceIds.Contains(a.Id))
                                .ToListAsync();

                            foreach (var attendance in attendances)
                            {
                                attendance.IsPaid = true;
                            }
                        }

                        await _context.SaveChangesAsync();
                    }

                    ViewBag.Amount = session.AmountTotal / 100m; // Convert from cents
                    ViewBag.PaymentId = session.Id;
                    return View();
                }
            }
            catch (StripeException ex)
            {
                TempData["Error"] = $"Error verifying payment: {ex.Message}";
            }

            return RedirectToAction("Dashboard", "Student");
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessMockPayment(int paymentId, string cardNumber)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null)
            {
                return Json(new { success = false, message = "Payment not found" });
            }

            // Simulate payment processing
            payment.PaymentStatus = "Succeeded";
            payment.PaidAt = DateTime.Now;
            payment.CardBrand = "Visa";
            payment.CardLast4 = cardNumber.Substring(cardNumber.Length - 4);

            // Update attendance records
            if (!string.IsNullOrEmpty(payment.AttendanceIds))
            {
                var attendanceIds = payment.AttendanceIds.Split(',').Select(int.Parse).ToList();
                var attendances = await _context.Attendances
                    .Where(a => attendanceIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var attendance in attendances)
                {
                    attendance.IsPaid = true;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, paymentId = payment.Id });
        }

        [HttpGet]
        public async Task<IActionResult> MockSuccess(int payment_id)
        {
            var payment = await _context.Payments.FindAsync(payment_id);
            if (payment != null)
            {
                ViewBag.Amount = payment.Amount;
                ViewBag.PaymentId = payment.StripePaymentIntentId;
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var studentEmail = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(studentEmail))
            {
                return RedirectToAction("Login", "Student");
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == studentEmail);

            if (student == null)
            {
                return NotFound();
            }

            var payments = await _context.Payments
                .Where(p => p.StudentId == student.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(payments);
        }
    }
}
