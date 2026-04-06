using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020648.BusinessLayers;
using SV22T1020648.Models.Partner;
using SV22T1020648.Shop.Models;

namespace SV22T1020648.Shop.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        /// <summary>
        /// Hiển thị trang đăng nhập
        /// </summary>
        [AllowAnonymous, HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập khách hàng/nhân viên
        /// </summary>
        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đủ tên đăng nhập và mật khẩu.");
                return View();
            }

            var userAccount = await SecurityDataService.AuthorizeAsync(username, password);
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Đăng nhập thất bại. Sai tài khoản hoặc bị khóa.");
                return View();
            }

            var customer = await PartnerDataService.GetCustomerAsync(int.Parse(userAccount.UserId));

            var userData = new WebUserData
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Address = customer?.Address ?? string.Empty,
                Province = customer?.Province ?? string.Empty,
                Phone = customer?.Phone ?? string.Empty
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, userData.CreatePrincipal());
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Giao diện đăng ký tài khoản mới
        /// </summary>
        [AllowAnonymous, HttpGet]
        public async Task<IActionResult> SignUp()
        {
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            return View(new SignUp());
        }

        /// <summary>
        /// Xử lý dữ liệu đăng ký
        /// </summary>
        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUp data)
        {
            if (data.Password != data.ConfirmPassword)
                ModelState.AddModelError(nameof(data.ConfirmPassword), "Mật khẩu xác nhận không khớp.");

            if (!await PartnerDataService.ValidatelCustomerEmailAsync(data.UserName))
                ModelState.AddModelError(nameof(data.UserName), "Email này đã được sử dụng.");

            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(data);
            }

            var customer = new Customer
            {
                CustomerName = data.UserName,
                ContactName = data.UserName,
                Email = data.UserName,
                Phone = data.Phone,
                Province = data.Province,
                Address = string.Empty,
                IsLocked = false
            };

            int customerId = await PartnerDataService.AddCustomerAsync(customer);

            if (customerId > 0)
            {
                await SecurityDataService.ChangePasswordAsync(data.UserName, data.Password);
                TempData["SuccessMessage"] = "Đăng ký thành công! Hãy đăng nhập.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Có lỗi xảy ra, vui lòng thử lại.");
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            return View(data);
        }

        /// <summary>
        /// Đăng xuất khỏi hệ thống
        /// </summary>
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Hiển thị giao diện đổi mật khẩu
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        /// <summary>
        /// Xử lý yêu cầu đổi mật khẩu
        /// </summary>
        /// <param name="oldPassword">Mật khẩu cũ (nếu bạn muốn kiểm tra)</param>
        /// <param name="newPassword">Mật khẩu mới</param>
        /// <param name="confirmPassword">Xác nhận mật khẩu mới</param>
        /// <returns></returns>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                ModelState.AddModelError("Error", "Mật khẩu mới không hợp lệ hoặc không khớp.");
                return View();
            }

            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login");

            bool result = await SecurityDataService.ChangePasswordAsync(userData.UserName, newPassword);

            if (result)
            {
                ViewBag.Message = "Đổi mật khẩu thành công!";
                return View();
            }

            ModelState.AddModelError("Error", "Đổi mật khẩu thất bại. Vui lòng thử lại.");
            return View();
        }

        /// <summary>
        /// Hiển thị thông tin hồ sơ khách hàng
        /// </summary>
        public async Task<IActionResult> Profile()
        {
            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login");

            var model = await PartnerDataService.GetCustomerAsync(int.Parse(userData.UserId));
            return model == null ? RedirectToAction("Login") : View(model);
        }

        /// <summary>
        /// Hiển thị giao diện chỉnh sửa thông tin cá nhân
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangeProfile()
        {
            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login");

            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            var model = await PartnerDataService.GetCustomerAsync(int.Parse(userData.UserId));

            return View(model);
        }

        /// <summary>
        /// Xử lý cập nhật thông tin cá nhân
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeProfile(Customer data)
        {
            if (string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Tên khách hàng không được để trống.");

            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                return View(data);
            }

            if (await PartnerDataService.UpdateCustomerAsync(data))
            {
                TempData["Message"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }

            ModelState.AddModelError("", "Cập nhật thất bại, vui lòng thử lại.");
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            return View(data);
        }
        /// <summary>
        /// Trang nhập email để lấy lại mật khẩu
        /// </summary>
        [AllowAnonymous, HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        /// <summary>
        /// Xử lý kiểm tra email có tồn tại không
        /// </summary>
        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> HandleForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || await PartnerDataService.ValidatelCustomerEmailAsync(email))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập Email hợp lệ hoặc Email không tồn tại.");
                return View("ForgotPassword");
            }

            TempData["ResetEmail"] = email;
            return View("ResetPassword");
        }

        /// <summary>
        /// Xử lý đặt lại mật khẩu mới sau khi xác thực email (POST)
        /// </summary>
        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("Error", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            if (await SecurityDataService.ChangePasswordAsync(email, newPassword))
            {
                TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("Error", "Có lỗi xảy ra, không thể đổi mật khẩu.");
            return View();
        }
    }
}