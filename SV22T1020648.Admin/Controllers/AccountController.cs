using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace SV22T1020648.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến tài khoản
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        /// <summary>
        /// Đăng nhập
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.UserName = username;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Tên đăng nhập và mật khẩu không được để trống");
                return View();
            }

            string hasedPassword = CryptHelper.HashMD5(password);

            var userAccount = await SV22T1020648.BusinessLayers.SecurityDataService.AuthorizeAsync(username, password);

            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Tên đăng nhập hoặc mật khẩu không đúng. Hoặc nhân viên đã nghỉ việc tài khoản bị vô hiệu");
                return View();
            }

            //Chuẩn bị thông tin để ghi lên "giấy chứng nhận"
            var userData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo,
                Roles = userAccount.RoleNames.Split(',')
                                 .Select(r => r.Trim())
                                 .Where(r => !string.IsNullOrEmpty(r))
                                 .ToList()
            };

            //Tạo giấy chứng nhận (ClaimsPrincipal)
            var principal = userData.CreatePrincipal();

            //Cấp giấy chứng nhận cho người dùng (đăng nhập)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }
        /// <summary>
        /// Đăng xuất
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <returns></returns>
        public IActionResult ChangePassword()
        {
            User.GetUserData();
            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
