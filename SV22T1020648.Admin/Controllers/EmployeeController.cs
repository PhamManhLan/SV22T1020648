using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020648.BusinessLayers;
using SV22T1020648.Models.Common;
using SV22T1020648.Models.HR;

namespace SV22T1020648.Admin.Controllers
{
    [Authorize(Roles = WebUserRoles.Administrator)]
    public class EmployeeController : Controller
    {
        // <summary>
        /// Tên biến dùng để lưu điều kiện tìm kiếm nhân viên trong session
        /// </summary>
        private const string EMPLOYEE_SEARCH = "EmployeeSearchInput";

        /// <summary>
        ///Nhập đầu vào tìm kiếm, Hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEE_SEARCH);

            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.Pagesize,
                    SearchValue = ""
                };

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await HRDataService.ListEmployeesAsync(input);

            // Lưu lại điều kiện tìm kiếm vào session
            ApplicationContext.SetSessionData(EMPLOYEE_SEARCH, input);

            return View(result);
        }
        /// <summary>
        /// Thêm nhân viên
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                //Kiểm tra dữ liệu đầu vào: FullName và Email là bắt buộc, Email chưa được sử dụng bởi nhân viên khác
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                //Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                //Tiền xử lý dữ liệu trước khi lưu vào database
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                //Lưu dữ liệu vào database (bổ sung hoặc cập nhật)
                if (data.EmployeeID == 0)
                {
                    await HRDataService.AddEmployeeAsync(data);
                }
                else
                {
                    await HRDataService.UpdateEmployeeAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch //(Exception ex)
            {
                //TODO: Ghi log lỗi căn cứ vào ex.Message và ex.StackTrace
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra dữ liệu hoặc thử lại sau");
                return View("Edit", data);
            }
        }


        /// <summary>
        /// Xóa nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                bool canDelete = !(await HRDataService.IsUsedEmployeeAsync(id));
                if (canDelete)
                {
                    await HRDataService.DeleteEmployeeAsync(id);
                }
                return RedirectToAction("Index");
            }

            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.CanDelete = !(await HRDataService.IsUsedEmployeeAsync(id));

            return View(model);
        }
        /// <summary>
        /// Thay đổi vai trò nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên cần thay đổi</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangeRole(int id)
        {
            ViewBag.Title = "Phân quyền nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        /// <summary>
        /// Lưu thông tin phân quyền của nhân viên
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRole(int employeeId, string[] roles)
        {
            var employee = await HRDataService.GetEmployeeAsync(employeeId);
            if (employee != null)
            {
                // Gộp mảng các hằng số thành chuỗi cách nhau bởi dấu phẩy
                employee.RoleNames = (roles != null) ? string.Join(",", roles) : "";

                await HRDataService.UpdateEmployeeAsync(employee);
                TempData["Message"] = $"Đã cập nhật quyền cho nhân viên {employee.FullName}";
            }
            return RedirectToAction("Index");
        }
        /// <summary>
        /// Đổi mật khẩu tài khoản của nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangePassword(int id)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null) return RedirectToAction("Index");

            return View(employee);
        }

        /// <summary>
        /// [POST] Xử lý lưu mật khẩu mới
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePassword(int employeeId, string userName, string newPassword, string confirmPassword)
        {
            // 1. Kiểm tra mật khẩu khớp nhau
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Xác nhận mật khẩu không khớp.";
                return RedirectToAction("ChangePassword", new { id = employeeId });
            }

            // 2. Gọi Service để đổi mật khẩu (Admin đổi nên không check pass cũ)
            bool result = await SecurityDataService.ChangePasswordAsync(userName, newPassword);

            if (result)
            {
                TempData["Message"] = $"Đã đổi mật khẩu thành công cho nhân viên {userName}";
                return RedirectToAction("Index");
            }

            TempData["ErrorMessage"] = "Có lỗi xảy ra, không thể đổi mật khẩu.";
            return RedirectToAction("ChangePassword", new { id = employeeId });
        }
    }
}
