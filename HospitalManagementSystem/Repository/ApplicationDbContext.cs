using HospitalManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Repository
{
    public class ApplicationDbContext: DbContext
    {
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>(patient =>
            {
                patient.Property(patient => patient.PatientId).IsRequired();
            });

            modelBuilder.Entity<Doctor>(doctor =>
            {
                doctor.Property(doctor=> doctor.DoctorId).IsRequired();
            });

            modelBuilder.Entity<Appointment>(appointment =>
            {
                appointment.Property(appointment=> appointment.AppointmentId).IsRequired();
            });

            // Department - Doctor (One-to-Many)
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Department)
                .WithMany(dep => dep.Doctors)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict); // İsteğe bağlı: Kısıtlama ya da silme davranışı ayarlanabilir

            // Doctor - Appointment (One-to-Many)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Patient - Appointment (One-to-Many)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}
