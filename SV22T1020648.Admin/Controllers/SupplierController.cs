using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020648.Models.Common;
using SV22T1020648.Models.Partner;

namespace SV22T1020648.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class SupplierController : Controller
    {
        /// <summary>
        /// Tên của biến dùng để lưu điều kiện tìm kiếm nhà cung cấp trong session
        /// </summary>
        private const string SUPPLIER_SEARCH = "SupplierSearchInput";


        /// <summary>
        /// Nhập đầu vào tìm kiếm, Hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH);

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
            var result = await PartnerDataService.ListSuppliersAsync(input);

            ApplicationContext.SetSessionData(SUPPLIER_SEARCH, input);

            return View(result);
        }
        /// <summary>
        /// Bổ sung nhà cung cấp mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";
            var model = new Supplier()
            {
                SupplierID = 0
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Chỉnh sửa nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Supplier data)
        {
            ViewBag.Title = data.SupplierID == 0
                ? "Bổ sung nhà cung cấp"
                : "Cập nhật thông tin nhà cung cấp";

            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(data.SupplierName))
                    ModelState.AddModelError(nameof(data.SupplierName), "Vui lòng nhập tên nhà cung cấp");

                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh thành");

                // Chuẩn hóa dữ liệu null
                if (string.IsNullOrEmpty(data.ContactName)) data.ContactName = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Email)) data.Email = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";

                if (!ModelState.IsValid)
                {
                    return View("Edit", data);
                }

                // Lưu CSDL
                if (data.SupplierID == 0)
                {
                    await PartnerDataService.AddSupplierAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateSupplierAsync(data);
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // TODO: log lỗi ex
                ModelState.AddModelError("Error", "Hệ thống đang bận, vui lòng thử lại sau");
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteSupplierAsync(id);
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.CanDelete = !(await PartnerDataService.IsUsedSupplierAsync(id));

            return View(model);
        }
    }
}
