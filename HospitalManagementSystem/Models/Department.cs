namespace HospitalManagementSystem.Models
{
    public class Department
    {
        public int DepartmentId {  get; set; }
        public string Name { get; set; }
        public string? Description  { get; set; }

        public virtual ICollection<Doctor>? Doctors { get; set; } //Bölüm birden fazla doktora sahip olabilir
    }
}
