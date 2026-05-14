using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester_Project.Data;
using Semester_Project.Models;

namespace Semester_Project.Controllers
{
    public class ChargesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChargesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdminLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("AdminEmail"));
        }

        // GET: Charges/ManageCharges
        [HttpGet]
        public async Task<IActionResult> ManageCharges()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var charges = await _context.MonthlyCharges
                .OrderByDescending(c => c.IsActive)
                .ThenBy(c => c.ChargeName)
                .ToListAsync();

            return View(charges);
        }

        // GET: Charges/AddCharge
        [HttpGet]
        public IActionResult AddCharge()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            return View();
        }

        // POST: Charges/AddCharge
        [HttpPost]
        public async Task<IActionResult> AddCharge(MonthlyCharge charge)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            if (ModelState.IsValid)
            {
                charge.CreatedAt = DateTime.Now;
                _context.MonthlyCharges.Add(charge);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Monthly charge added successfully!";
                return RedirectToAction(nameof(ManageCharges));
            }

            return View(charge);
        }

        // GET: Charges/EditCharge/5
        [HttpGet]
        public async Task<IActionResult> EditCharge(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var charge = await _context.MonthlyCharges.FindAsync(id);
            if (charge == null)
            {
                return NotFound();
            }

            return View(charge);
        }

        // POST: Charges/EditCharge/5
        [HttpPost]
        public async Task<IActionResult> EditCharge(int id, MonthlyCharge charge)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            if (id != charge.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                charge.UpdatedAt = DateTime.Now;
                _context.Update(charge);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Monthly charge updated successfully!";
                return RedirectToAction(nameof(ManageCharges));
            }

            return View(charge);
        }

        // POST: Charges/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var charge = await _context.MonthlyCharges.FindAsync(id);
            if (charge == null)
            {
                return Json(new { success = false, message = "Charge not found" });
            }

            charge.IsActive = !charge.IsActive;
            charge.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isActive = charge.IsActive });
        }

        // POST: Charges/DeleteCharge/5
        [HttpPost]
        public async Task<IActionResult> DeleteCharge(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var charge = await _context.MonthlyCharges.FindAsync(id);
            if (charge == null)
            {
                return Json(new { success = false, message = "Charge not found" });
            }

            _context.MonthlyCharges.Remove(charge);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET: Charges/ManageDeadlines
        [HttpGet]
        public async Task<IActionResult> ManageDeadlines()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var deadlines = await _context.PaymentDeadlines
                .OrderByDescending(d => d.Year)
                .ThenByDescending(d => d.Month)
                .ToListAsync();

            return View(deadlines);
        }

        // GET: Charges/AddDeadline
        [HttpGet]
        public IActionResult AddDeadline()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            return View();
        }

        // POST: Charges/AddDeadline
        [HttpPost]
        public async Task<IActionResult> AddDeadline(PaymentDeadline deadline)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            if (ModelState.IsValid)
            {
                // Check if deadline already exists for this month/year
                var existing = await _context.PaymentDeadlines
                    .FirstOrDefaultAsync(d => d.Month == deadline.Month && d.Year == deadline.Year);

                if (existing != null)
                {
                    TempData["Error"] = "Deadline already exists for this month!";
                    return View(deadline);
                }

                deadline.CreatedAt = DateTime.Now;
                _context.PaymentDeadlines.Add(deadline);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Payment deadline added successfully!";
                return RedirectToAction(nameof(ManageDeadlines));
            }

            return View(deadline);
        }

        // POST: Charges/DeleteDeadline/5
        [HttpPost]
        public async Task<IActionResult> DeleteDeadline(int id)
        {
            if (!IsAdminLoggedIn())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var deadline = await _context.PaymentDeadlines.FindAsync(id);
            if (deadline == null)
            {
                return Json(new { success = false, message = "Deadline not found" });
            }

            _context.PaymentDeadlines.Remove(deadline);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET: Charges/BillRecheckRequests
        [HttpGet]
        public async Task<IActionResult> BillRecheckRequests()
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var requests = await _context.BillRecheckRequests
                .Include(r => r.Student)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return View(requests);
        }

        // GET: Charges/ReviewRequest/5
        [HttpGet]
        public async Task<IActionResult> ReviewRequest(int id)
        {
            try
            {
                if (!IsAdminLoggedIn())
                {
                    return RedirectToAction("Login", "Admin");
                }

                var request = await _context.BillRecheckRequests
                    .Include(r => r.Student)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                {
                    TempData["Error"] = "Bill recheck request not found.";
                    return RedirectToAction(nameof(BillRecheckRequests));
                }

                // Get student's attendance for that month
                var attendances = await _context.Attendances
                    .Where(a => a.StudentId == request.StudentId 
                        && a.Date.Month == request.Month 
                        && a.Date.Year == request.Year)
                    .OrderBy(a => a.Date)
                    .ToListAsync();

                ViewBag.Attendances = attendances ?? new List<Attendance>();
                return View(request);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading the request: " + ex.Message;
                return RedirectToAction(nameof(BillRecheckRequests));
            }
        }

        // POST: Charges/RespondToRequest
        [HttpPost]
        public async Task<IActionResult> RespondToRequest(int id, string response, string status)
        {
            if (!IsAdminLoggedIn())
            {
                return RedirectToAction("Login", "Admin");
            }

            var request = await _context.BillRecheckRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            request.AdminResponse = response;
            request.Status = status;
            request.ReviewedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Response sent successfully!";
            return RedirectToAction(nameof(BillRecheckRequests));
        }
    }
}
