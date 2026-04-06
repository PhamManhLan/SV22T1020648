using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020648.Models.Common;
using SV22T1020648.Models.Partner;

namespace SV22T1020648.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ShipperController : Controller
    {
        /// <summary>
        /// Tên của biến dùng để lưu điều kiện tìm kiếm người giao hàng trong session
        /// </summary>
        private const string SHIPPER_SEARCH = "ShipperSearchInput";

        /// <summary>
        /// Nhập đầu vào tìm kiếm, Hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPER_SEARCH);

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
            var result = await PartnerDataService.ListShippersAsync(input);

            ApplicationContext.SetSessionData(SHIPPER_SEARCH, input);

            return View(result);
        }
        /// <summary>
        /// Bổ sung người giao hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung người giao hàng";
            var model = new Shipper()
            {
                ShipperID = 0
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Chỉnh sửa người giao hàng
        /// </summary>
        /// <param name="id">Mã người giao hàng cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin người giao hàng";
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Shipper data)
        {
            ViewBag.Title = data.ShipperID == 0
                ? "Bổ sung người giao hàng"
                : "Cập nhật thông tin người giao hàng";

            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(data.ShipperName))
                    ModelState.AddModelError(nameof(data.ShipperName), "Vui lòng nhập tên người giao hàng");

                // Chuẩn hóa dữ liệu null
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";

                if (!ModelState.IsValid)
                {
                    return View("Edit", data);
                }

                // Lưu dữ liệu
                if (data.ShipperID == 0)
                {
                    await PartnerDataService.AddShipperAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateShipperAsync(data);
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
        /// Xóa người giao hàng
        /// </summary>
        /// <param name="id">Mã người giao hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteShipperAsync(id);
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.CanDelete = !(await PartnerDataService.IsUsedShipperAsync(id));

            return View(model);
        }
    }
}
