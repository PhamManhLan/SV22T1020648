using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020648.BusinessLayers;
using SV22T1020648.Models.Catalog;
using SV22T1020648.Models.Common;

namespace SV22T1020648.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ProductController : Controller
    {
        // <summary>
        /// Tên biến dùng để lưu điều kiện tìm kiếm product trong session
        /// </summary>
        private const string PRODUCT_SEARCH = "ProductSearchInput";

        /// <summary>
        /// Nhập đầu vào tìm kiếm, Hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);

            if (input == null)
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.Pagesize,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };

            ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(
                new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = 1000,
                    SearchValue = ""
                });

            ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(
                new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = 1000,
                    SearchValue = ""
                });

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung mặt hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung mặt hàng";
            var model = new Product()
            {
                ProductID = 0,
                Price = 0,
                IsSelling = true
            };
            return View("Edit", model);
        }

        /// <summary>
        /// Chỉnh sửa mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin mặt hàng";
            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Product data)
        {
            ViewBag.Title = data.ProductID == 0
                ? "Bổ sung mặt hàng"
                : "Cập nhật thông tin mặt hàng";

            try
            {
                // Kiểm tra dữ liệu
                if (string.IsNullOrWhiteSpace(data.ProductName))
                    ModelState.AddModelError(nameof(data.ProductName), "Vui lòng nhập tên mặt hàng");

                if (!data.SupplierID.HasValue || data.SupplierID.Value <= 0)
                    ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp");

                if (!data.CategoryID.HasValue || data.CategoryID.Value <= 0)
                    ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");

                if (string.IsNullOrWhiteSpace(data.Unit))
                    ModelState.AddModelError(nameof(data.Unit), "Vui lòng nhập đơn vị tính");

                if (data.Price < 0)
                    ModelState.AddModelError(nameof(data.Price), "Giá bán không hợp lệ");

                // Chuẩn hóa dữ liệu
                if (string.IsNullOrEmpty(data.ProductDescription)) data.ProductDescription = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "";
                if (string.IsNullOrEmpty(data.Unit)) data.Unit = "";

                if (!ModelState.IsValid)
                    return View("Edit", data);

                // Lưu CSDL
                if (data.ProductID == 0)
                    await CatalogDataService.AddProductAsync(data);
                else
                    await CatalogDataService.UpdateProductAsync(data);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // TODO: log ex nếu cần
                ModelState.AddModelError("Error", "Hệ thống đang bận, vui lòng thử lại sau");
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteProductAsync(id);
                return RedirectToAction("Index");
            }

            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            ViewBag.CanDelete = !(await CatalogDataService.IsUsedProductAsync(id));

            return View(model);
        }
        /// <summary>
        /// Chi tiết mặt hàng
        /// </summary>
        public async Task<IActionResult> Detail(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            ViewBag.ListAttributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.ListPhotos = await CatalogDataService.ListPhotosAsync(id);

            return View(product);
        }

        /// <summary>
        /// Hiển thị danh sách thuộc tính của 1 sản phẩm
        /// </summary>
        /// <param name="id">Mã sản phẩm cần xem thuốc tính</param>
        /// <returns></returns>
        public async Task<IActionResult> ListAttributes(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            ViewBag.Product = product;

            var attributes = await CatalogDataService.ListAttributesAsync(id);
            return View(attributes);
        }

        /// <summary>
        /// Thêm thuốc tính cho sản phẩm
        /// </summary>
        /// <param name="id">Mã sản phẩm</param>
        /// <returns></returns>
        public IActionResult CreateAttribute(int id)
        {
            ViewBag.Title = "Bổ sung thuộc tính";

            var model = new ProductAttribute()
            {
                ProductID = id,
                DisplayOrder = 0
            };

            return View("EditAttribute", model);
        }

        /// <summary>
        /// Cập nhật thuộc tính của mặt hàng
        /// </summary>
        public async Task<IActionResult> EditAttribute(int id, int attributeId = 0)
        {
            ViewBag.Title = attributeId == 0 ? "Bổ sung thuộc tính" : "Cập nhật thuộc tính";
            if (attributeId == 0)
            {
                var newAttribute = new ProductAttribute
                {
                    ProductID = id,
                    DisplayOrder = 0
                };

                return View(newAttribute);
            }

            var model = await CatalogDataService.GetAttributeAsync(attributeId);
            if (model == null || model.ProductID != id)
                return RedirectToAction("Detail", new { id });

            return View(model);
        }

        /// <summary>
        /// Lưu thuộc tính (Thêm / Cập nhật)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            ViewBag.Title = data.AttributeID == 0 ? "Bổ sung thuộc tính" : "Cập nhật thuộc tính";

            if (string.IsNullOrWhiteSpace(data.AttributeName))
                ModelState.AddModelError(nameof(data.AttributeName), "Tên thuộc tính không được để trống");

            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                ModelState.AddModelError(nameof(data.AttributeValue), "Giá trị thuộc tính không được để trống");

            if (data.DisplayOrder < 0)
                ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự hiển thị phải lớn hơn hoặc bằng 0");

            if (!ModelState.IsValid)
                return View("EditAttribute", data);

            if (data.AttributeID == 0)
                await CatalogDataService.AddAttributeAsync(data);
            else
                await CatalogDataService.UpdateAttributeAsync(data);

            return RedirectToAction("Detail", new { id = data.ProductID });
        }


        /// <summary>
        /// Xóa thuốc tính
        /// </summary>
        /// <param name="id">Mã sản phẩm</param>
        /// <param name="attributeId">Mã thuốc tính cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteAttribute(int id, long attributeId = 0)
        {
            var model = await CatalogDataService.GetAttributeAsync(attributeId);

            if (model == null || model.ProductID != id)
                return RedirectToAction("Detail", new { id });

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAttribute(long attributeId, int id)
        {
            await CatalogDataService.DeleteAttributeAsync(attributeId);
            return Json(new { success = true });
        }

        /// <summary>
        /// hiển thị danh sách photo của sản phẩm
        /// </summary>
        /// <param name="id">Mã sản phẩm</param>
        /// <returns></returns>
        public async Task<IActionResult> ListPhotos(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            ViewBag.Product = product;
            var photos = await CatalogDataService.ListPhotosAsync(id);

            return View(photos);
        }

        /// <summary>
        /// Thêm photo
        /// </summary>
        /// <param name="id">mã photo </param>
        /// <returns></returns>
        public IActionResult CreatePhoto(int id)
        {
            var model = new ProductPhoto
            {
                ProductID = id,
                DisplayOrder = 0,
                IsHidden = false
            };

            return View("EditPhoto", model);
        }

        /// <summary>
        /// Chỉnh sửa thông tin photo của sản phẩm
        /// </summary>
        /// <param name="id">Mã sản phẩm</param>
        /// <param name="photoId">Mã photo cần chỉnh sửa </param>
        /// <returns></returns>
        public async Task<IActionResult> EditPhoto(int id, long photoId = 0)
        {
            ViewBag.Title = photoId == 0 ? "Bổ sung ảnh" : "Cập nhật ảnh";

            if (photoId == 0)
            {
                return View(new ProductPhoto
                {
                    ProductID = id,
                    DisplayOrder = 0,
                    IsHidden = false
                });
            }

            var model = await CatalogDataService.GetPhotoAsync(photoId);
            if (model == null || model.ProductID != id)
                return RedirectToAction("Detail", new { id });

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            if (string.IsNullOrWhiteSpace(data.Description))
                ModelState.AddModelError(nameof(data.Description), "Vui lòng nhập mô tả ảnh");

            if (data.DisplayOrder < 0)
                ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự hiển thị không hợp lệ");

            // Nếu thêm mới mà chưa chọn file
            if (data.PhotoID == 0 && (uploadPhoto == null || uploadPhoto.Length == 0))
                ModelState.AddModelError("uploadPhoto", "Vui lòng chọn ảnh");

            if (!ModelState.IsValid)
                return View("EditPhoto", data);

            // ===== Upload file nếu có =====
            if (uploadPhoto != null && uploadPhoto.Length > 0)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string extension = Path.GetExtension(uploadPhoto.FileName);
                string fileName = $"{Guid.NewGuid()}{extension}";
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }

                data.Photo = fileName;
            }
            else
            {
                // Nếu sửa mà không upload file mới → giữ ảnh cũ
                if (data.PhotoID != 0)
                {
                    var oldPhoto = await CatalogDataService.GetPhotoAsync(data.PhotoID);
                    if (oldPhoto != null)
                        data.Photo = oldPhoto.Photo;
                }
            }

            if (data.PhotoID == 0)
                await CatalogDataService.AddPhotoAsync(data);
            else
                await CatalogDataService.UpdatePhotoAsync(data);

            return RedirectToAction("Detail", new { id = data.ProductID });
        }
        /// <summary>
        /// Xóa photo của sản phẩm
        /// </summary>
        /// <param name="id">Mã sản phẩm</param>
        /// <param name="photoId">Mã photo cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> DeletePhoto(int id, long photoId)
        {
            var model = await CatalogDataService.GetPhotoAsync(photoId);

            if (model == null || model.ProductID != id)
                return RedirectToAction("Detail", new { id });

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> DeletePhoto(long photoId, int id)
        {
            var oldPhoto = await CatalogDataService.GetPhotoAsync(photoId);

            bool result = await CatalogDataService.DeletePhotoAsync(photoId);

            if (!result)
                return Json(new { code = 0, message = "Không thể xóa ảnh." });

            // Xóa file vật lý nếu có
            if (oldPhoto != null && !string.IsNullOrWhiteSpace(oldPhoto.Photo))
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", oldPhoto.Photo);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

            return Json(new { code = 1, message = "" });
        }
    }
}
