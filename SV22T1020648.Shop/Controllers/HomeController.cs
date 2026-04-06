using Microsoft.AspNetCore.Mvc;
using SV22T1020648.BusinessLayers;
using SV22T1020648.Models.Catalog;
using SV22T1020648.Models.Common;
using SV22T1020648.Shop.Models;
using System.Diagnostics;

namespace SV22T1020648.Shop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Tên biến dùng để lưu điều kiện tìm kiếm product trong session
        /// </summary>
        private const string PRODUCT_SEARCH = "ProductSearchInput";

        /// <summary>
        /// Nhập đầu vào tìm kiếm, Hiển thị kết quả tìm kiếm
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);

            if (input == null)
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize, // Lấy từ appsettings
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };

            // Lấy danh sách loại hàng và nhà cung cấp để đổ vào Dropdown
            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput() { Page = 1, PageSize = 1000, SearchValue = "" });
            ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput() { Page = 1, PageSize = 1000, SearchValue = "" });

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            // Thêm dòng này để "cứu nguy" nếu PageSize bị mất hoặc bằng 0
            if (input.PageSize <= 0)
            {
                input.PageSize = 12; // Hoặc 20, tùy bạn muốn hiển thị bao nhiêu sản phẩm 1 trang
            }

            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);

            return PartialView(result);
        }
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        /// <summary>
        /// Xem thông tin chi tiết của mặt hàng
        /// </summary>
        public async Task<IActionResult> ProductDetails(int id)
        {
            // Lấy thông tin cơ bản của sản phẩm
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
            {
                return RedirectToAction("Index");
            }

            // Sử dụng đúng tên hàm trong CatalogDataService của bạn
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);

            return View(product);
        }

    }
}
