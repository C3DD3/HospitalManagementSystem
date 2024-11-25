using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Repository;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNet.Identity;

namespace HospitalManagementSystem.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<IdentityUser> _userManager;
        public AppointmentsController(ApplicationDbContext context, Microsoft.AspNetCore.Identity.UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Appointments
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User); // Aktif kullanıcının IdentityUserId'sini al
            var isManager = User.IsInRole("Manager"); // Kullanıcının "Manager" rolünde olup olmadığını kontrol et
            var isDoctor = User.IsInRole("Doctor"); // Kullanıcının "Doctor" rolünde olup olmadığını kontrol et

            List<Appointment> appointments;

            if (isManager)
            {
                // Eğer kullanıcı Manager ise tüm randevuları getir
                appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .ToListAsync();
            }
            else if (isDoctor)
            {
                // Eğer kullanıcı Doctor ise kendi randevularını getir
                appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Where(a => a.Doctor.IdentityUserId == userId)
                    .ToListAsync();
            }
            else
            {
                // Eğer kullanıcı Patient ise kendi randevularını getir
                appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Where(a => a.Patient.IdentityUserId == userId)
                    .ToListAsync();
            }

            return View(appointments);
        }



        // GET: Appointments Filtered
        public async Task<IActionResult> Index2()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Giriş yapan kullanıcının ID'sini alır

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Department)
                .Where(a => a.Patient.IdentityUserId == userId) // Sadece giriş yapan kullanıcının randevuları
                .Select(a => new AppointmentViewModel
                {
                    AppointmentDate = a.AppointmentDate,
                    PatientName = a.Patient.FirstName + " " + a.Patient.LastName,
                    DoctorName = a.Doctor.FirstName + " " + a.Doctor.LastName,
                    DepartmentName = a.Doctor.Department.Name
                })
                .ToListAsync();

            return View(appointments);
        }



        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                    .ThenInclude(d => d.Department) // Department bilgilerini eklemek için
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }


        [HttpPost]
        public JsonResult GetDoctorsByDepartment(int departmentId)
        {
            var doctors = _context.Doctors
                .Where(d => d.DepartmentId == departmentId)
                .Select(d => new { id = d.DoctorId, name = d.FirstName + " " + d.LastName })
                .ToList();

            return Json(doctors);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new AppointmentCreateViewModel();
            ViewData["Departments"] = new SelectList(_context.Departments, "DepartmentId", "Name");
            ViewData["Doctors"] = new SelectList(Enumerable.Empty<SelectListItem>()); // Başlangıçta boş
            ViewData["Patients"] = new SelectList(_context.Patients, "PatientId", "Name");

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Create(AppointmentCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Hata varsa ViewModel’i tekrar doldur
                ViewData["Departments"] = new SelectList(_context.Departments, "DepartmentId", "Name", model.DepartmentId);
                ViewData["Doctors"] = new SelectList(_context.Doctors, "DoctorId", "Name", model.DoctorId);
                ViewData["Patients"] = new SelectList(_context.Patients, "PatientId", "Name", model.PatientId);

                return View(model);
            }
            if (model.AppointmentDate <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "The appointment date cannot be in the past.";
                return RedirectToAction(nameof(Create));
            }

            var user = await _userManager.GetUserAsync(User);
            // Yeni bir Appointment nesnesi oluştur ve doldur
            var appointment = new Appointment
            {
                PatientId = _context.Patients.FirstOrDefault(x=> x.IdentityUserId == user.Id)?.PatientId ?? throw new Exception("Patient not found"),
                DoctorId = model.DoctorId,
                AppointmentDate = model.AppointmentDate
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: Appointments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "DoctorId", appointment.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "PatientId", appointment.PatientId);
            return View(appointment);
        }

        // POST: Appointments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,PatientId,DoctorId,AppointmentDate")] Appointment appointment)
        {
            if (id != appointment.AppointmentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.AppointmentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "DoctorId", appointment.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "PatientId", appointment.PatientId);
            return View(appointment);
        }

        // GET: Appointments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.AppointmentId == id);
        }


        public class AppointmentViewModel
        {
            public DateTime AppointmentDate { get; set; }
            public string PatientName { get; set; }
            public string DoctorName { get; set; }
            public string DepartmentName { get; set; }
        }

        public class AppointmentCreateViewModel
        {
            public int DepartmentId { get; set; }
            public int DoctorId { get; set; }
            public int PatientId { get; set; }

            public DateTime AppointmentDate { get; set; }
        }
    }

}

