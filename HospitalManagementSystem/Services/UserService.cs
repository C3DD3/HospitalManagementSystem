using HospitalManagementSystem.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Security.Claims;

namespace HospitalManagementSystem.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public UserService(UserManager<IdentityUser> userManager, IUserStore<IdentityUser> userStore, IEmailSender emailSender, ApplicationDbContext context, IConfiguration configuration, RoleManager<IdentityRole> roleManager, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _emailSender = emailSender;
            _context = context;
            _configuration = configuration;
            _roleManager = roleManager;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<UserCreateResponse> CreateIdentityUser(string email, string password = "")
        {
            var user = CreateUser();
            try
            {
                await _userStore.SetUserNameAsync(user, email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, email, CancellationToken.None);
                if (string.IsNullOrEmpty(password))
                {
                    password = GetDefaultPassword();
                }
                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    return new UserCreateResponse
                    {
                        Result = result,
                        CreatedUser = null
                    };
                }

                // Doctor rolünü kontrol et ve varsa ekle
                if (!await _roleManager.RoleExistsAsync("Doctor"))
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole("Doctor"));
                    if (!roleResult.Succeeded)
                    {
                        return new UserCreateResponse
                        {
                            Result = IdentityResult.Failed(new IdentityError { Description = "Failed to create Doctor role." }),
                            CreatedUser = null
                        };
                    }
                }

                // Kullanıcıya Doctor rolünü ata
                var addRoleResult = await _userManager.AddToRoleAsync(user, "Doctor");
                if (!addRoleResult.Succeeded)
                {
                    return new UserCreateResponse
                    {
                        Result = addRoleResult,
                        CreatedUser = null
                    };
                }

                return new UserCreateResponse
                {
                    Result = IdentityResult.Success,
                    CreatedUser = user
                };
            }
            catch (Exception ex)
            {
                return new UserCreateResponse
                {
                    Result = IdentityResult.Failed(new IdentityError { Description = ex.Message }),
                    CreatedUser = null
                };
            }
        }

        public async Task<IdentityResult> DeleteIdentityUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return IdentityResult.Failed(new IdentityError { Description = "User not found." });
                }

                return await _userManager.DeleteAsync(user);
            }
            catch (Exception ex)
            {
                return IdentityResult.Failed(new IdentityError { Description = ex.Message });
            }
        }

        public async Task<IdentityResult> LinkDoctorAndIdentityUser(int doctorId, string email)
        {
            if (doctorId == 0)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Doctor Id cannot be null or 0" });
            }

            var doctor = _context.Doctors.FirstOrDefault(x => x.DoctorId == doctorId);
            if (doctor == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = $"Doctor couldn't be found by id: {doctorId}" });
            }

            var password = GetDefaultPassword();

            var userResponse = await CreateIdentityUser(email, password);
            if (userResponse == null || !userResponse.Result.Succeeded)
            {
                return userResponse.Result;
            }

            var createdUser = userResponse.CreatedUser;
            doctor.IdentityUserId = createdUser.Id;

            try
            {
                _context.Update(doctor);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var deleteResult = await DeleteIdentityUser(doctor.IdentityUserId);
                if (!deleteResult.Succeeded)
                {
                    var errorMessages = string.Join("\n", deleteResult.Errors.Select(e => e.Description));
                    return IdentityResult.Failed(new IdentityError { Description = $"An error occurred during the process and user cleanup failed: {errorMessages}" });
                }

                throw new Exception($"An error occurred during the update to doctor service, but the user saved in the aspnetusers table was reverted. Exception message: {ex.Message}");
            }

            return IdentityResult.Success;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await _emailSender.SendEmailAsync(email, subject, htmlMessage);
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }

        private string GetDefaultPassword()
        {
            var password = _configuration["DoctorDefaultPassword"];
            if (string.IsNullOrEmpty(password))
            {
                throw new NotSupportedException("Default password couldn't be found in configuration.");
            }
            return password;
        }
        public string GetUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }

    public class UserCreateResponse
    {
        public IdentityResult Result { get; set; }
        public IdentityUser CreatedUser { get; set; }
    }
}
