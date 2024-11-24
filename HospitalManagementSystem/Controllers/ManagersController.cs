using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagementSystem.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagersController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ManagerName"] = User.Identity.Name; // Kullanıcının adı
            return View();
        }
    }
}
