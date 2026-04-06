using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020648.BusinessLayers;
using SV22T1020648.Models.Common;
using SV22T1020648.Models.Partner;

namespace SV22T1020648.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    /// <summary>
    /// các chức năng liên quan đến khách hàng
    /// </summary>
    public class CustomerController : Controller
    {

        /// <summary>
        /// tên của biến dùng để lưu điều kiện tìm kiếm khách hàng trong session
        /// </summary>
        private const string CUSTOMER_SEARCH = "CustomerSearchInput";


        /// <summary>
        /// Nhập đầu vào tìm kiếm, Hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH);

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
            var result = await PartnerDataService.ListCustomersAsync(input);
            ApplicationContext.SetSessionData("CustomerSearchInput", input);
            return View(result);
        }



        /// <summary>
        /// Bổ sung khách hàng mới 
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Chỉnh sửa khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }
        [HttpPost]

        public async Task<IActionResult> SaveData(Customer data)
        {
            ViewBag.Tittle = data.CustomerID == 0 ? " Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";

            //TODO: kiểm tra tính hợp lệ của dữ liệu và thông báo lỗi kiểu dữ liệu không hợp lệ
            // sử dụng ModelState để kiểm soát thông bảo lỗi và gửi thông báo lỗi cho View
            try
            {
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập tên của khách hàng");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Email không được để trống");
                else if (!(await PartnerDataService.ValidatelCustomerEmailAsync(data.Email, data.CustomerID)))
                    ModelState.AddModelError(nameof(data.Email), "Email này bị trùng");

                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh thành");


                //Điều chỉnh lại các giá trị dữ liệu khác theo qui định/qui ước của App
                if (string.IsNullOrEmpty(data.ContactName)) data.ContactName = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";


                if (!ModelState.IsValid)
                {
                    return View("Edit", data);
                }


                // yêu cầu lưu dữ liệu vào CSDL
                if (data.CustomerID == 0)
                {
                    await PartnerDataService.AddCustomerAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateCustomerAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // lưu long lỗi trong ex
                ModelState.AddModelError("Error", "Hệ thống đang bận, vui lòng thử lại sau");
                return View("Edit", data);
            }

        }
        /// <summary>
        /// Xóa khách hàng 
        /// </summary>
        /// <param name="id">Mã khách hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            ViewBag.CanDelete = !(await PartnerDataService.IsUsedCustomerAsync(id));

            return View(model);
        }
        /// <summary>
        /// Đổi mật mật khẩu
        /// </summary>
        /// <param name="id">mật khẩu cần đổi</param>
        /// <returns></returns>
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Đổi mật khẩu khách hàng";
            return View(model);
        }
        /// <summary>
        /// [POST] Thực hiện lưu mật khẩu mới cho khách hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePassword(int customerId, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Xác nhận mật khẩu không khớp.";
                return RedirectToAction("ChangePassword", new { id = customerId });
            }

            var customer = await PartnerDataService.GetCustomerAsync(customerId);
            if (customer == null) return RedirectToAction("Index");

            bool result = await SecurityDataService.ChangePasswordAsync(customer.Email, newPassword);

            if (result)
            {
                // Gán thông báo thành công vào TempData
                TempData["SuccessMessage"] = $"Đã đổi mật khẩu thành công cho khách hàng: {customer.CustomerName}";
                return RedirectToAction("Index");
            }

            TempData["ErrorMessage"] = "Lỗi hệ thống, không thể đổi mật khẩu.";
            return RedirectToAction("ChangePassword", new { id = customerId });
        }
    }
}