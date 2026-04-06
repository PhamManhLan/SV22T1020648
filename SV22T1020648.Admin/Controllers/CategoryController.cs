using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020648.BusinessLayers;
using SV22T1020648.Models.Catalog;
using SV22T1020648.Models.Common;

namespace SV22T1020648.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class CategoryController : Controller
    {
        /// <summary>
        /// Tên biến lưu điều kiện tìm kiếm trong session
        /// </summary>
        private const string CATEGORY_SEARCH = "CategorySearchInput";

        /// <summary>
        /// Hiển thị trang chính
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH);

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
        /// Tìm kiếm
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await CatalogDataService.ListCategoriesAsync(input);

            ApplicationContext.SetSessionData(CATEGORY_SEARCH, input);

            return View(result);
        }
        /// <summary>
        /// Bổ sung loại hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung loại hàng";
            var model = new Category()
            {
                CategoryID = 0
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Chỉnh sửa loại hàng
        /// </summary>
        /// <param name="id">Mã loại hàng cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Category data)
        {
            ViewBag.Title = data.CategoryID == 0
                ? "Bổ sung loại hàng"
                : "Cập nhật thông tin loại hàng";

            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(data.CategoryName))
                    ModelState.AddModelError(nameof(data.CategoryName), "Vui lòng nhập tên loại hàng");

                // Chuẩn hóa dữ liệu null
                if (string.IsNullOrEmpty(data.Description)) data.Description = "";

                if (!ModelState.IsValid)
                {
                    return View("Edit", data);
                }

                // Lưu dữ liệu
                if (data.CategoryID == 0)
                {
                    await CatalogDataService.AddCategoryAsync(data);
                }
                else
                {
                    await CatalogDataService.UpdateCategoryAsync(data);
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
        /// Xóa loại hàng
        /// </summary>
        /// <param name="id">Mã loại hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteCategoryAsync(id);
                return RedirectToAction("Index");
            }

            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.CanDelete = !(await CatalogDataService.IsUsedCategoryAsync(id));

            return View(model);
        }
    }
}
