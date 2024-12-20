﻿namespace HospitalManagementSystem.Models
{
    public class Patient
    {
        public int PatientId { get; set; }        
        public string NationalId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string? IdentityUserId { get; set; }
        public DateTime DateOfBirth { get; set; }   //DateOnly kullansak sadece yıl ay gün seçebilirdik.


        public virtual ICollection<Appointment>? Appointments { get; set; }


    }
}
