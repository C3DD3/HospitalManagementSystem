using Microsoft.AspNetCore.Identity;

namespace HospitalManagementSystem.Services
{
    public interface IUserService
    {
        Task<UserCreateResponse> CreateIdentityUser(string email, string password = "");
        Task<IdentityResult> DeleteIdentityUser(string userId);
        Task<IdentityResult> LinkDoctorAndIdentityUser(int doctorId, string email);
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
