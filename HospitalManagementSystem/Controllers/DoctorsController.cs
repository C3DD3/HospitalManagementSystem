using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HospitalManagementSystem.Models;
using HospitalManagementSystem.Repository;
using HospitalManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace HospitalManagementSystem.Controllers
{
    //[Authorize (Roles = "Manager")] 
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly UserManager<IdentityUser> _userManager;

        public DoctorsController(ApplicationDbContext context, IUserService userService, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userService = userService;
            _userManager = userManager;
        }

        // GET: Doctors
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Doctors.Include(d => d.Department);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Doctors filtered
        public async Task<IActionResult> Index2()
        {
            var doctors = await _context.Doctors
                .Include(d => d.Department)
                .Select(d => new DoctorViewModel
                {
                    FirstName = d.FirstName,
                    LastName = d.LastName,
                    DepartmentName = d.Department.Name
                })
                .ToListAsync();

            return View(doctors);
        }

        public async Task<IActionResult> PersonalizedIndex()
        {
            // Giriş yapan kullanıcının IdentityUser Id'sini alın
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Kullanıcı ID'sine göre Doctors tablosundan doktoru alın
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.IdentityUserId == user.Id);

            if (doctor == null)
            {
                // Kullanıcıyla eşleşen bir doktor bulunamazsa
                return RedirectToAction("Create");
            }

            // ViewData ile doktor adını view'a gönderin
            ViewData["DoctorName"] = $"{doctor.FirstName} {doctor.LastName}";

            return View();
        }



        // GET: Doctors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(m => m.DoctorId == id);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        public IActionResult Create()
        {
            ViewData["Departments"] = new SelectList(_context.Departments, "DepartmentId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorInputModel inputModel)
        {
            
            if (ModelState.IsValid)
            {
                // Email ve IdentityUser işlemleri için bir servis kullanabiliriz
                var createdIdentityUser = await _userService.CreateIdentityUser(inputModel.Email, "");
                if (createdIdentityUser == null)
                {
                    ModelState.AddModelError(string.Empty, "Email ile kullanıcı oluşturulamadı.");
                    ViewData["Departments"] = new SelectList(_context.Departments, "DepartmentId", "Name", inputModel.Doctor.DepartmentId);
                    return View(inputModel);
                }

                inputModel.Doctor.IdentityUserId = createdIdentityUser.CreatedUser.Id;

                _context.Add(inputModel.Doctor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["Departments"] = new SelectList(_context.Departments, "DepartmentId", "Name", inputModel.Doctor.DepartmentId);
            return View(inputModel);
        }


        // GET: Doctors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentId", doctor.DepartmentId);
            return View(doctor);
        }

        // POST: Doctors/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DoctorId,FirstName,LastName,IdNumber,PhoneNumber,IdentityUserId,DepartmentId")] Doctor doctor)
        {
            if (id != doctor.DoctorId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.DoctorId))
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
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentId", doctor.DepartmentId);
            return View(doctor);
        }

        // GET: Doctors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(m => m.DoctorId == id);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // POST: Doctors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.DoctorId == id);
        }
    }

    public class DoctorInputModel
    {
        public string Email { get; set; } // Email için ek alan
        public Doctor Doctor { get; set; } // Doctor özelliklerini içeren nesne
    }
    public class DoctorViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DepartmentName { get; set; }
    }

    }
