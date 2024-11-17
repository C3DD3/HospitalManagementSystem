namespace HospitalManagementSystem.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string IdNumber { get; set; }
        public string PhoneNumber { get; set; }
        public string? IdentityUserId { get; set; }

        public int DepartmentId { get; set; } // Foreign Key
        public virtual Department? Department { get; set; } // Navigation Property       

        public virtual ICollection<Appointment>? Appointments { get; set; } // Navigation Property

    }
}
