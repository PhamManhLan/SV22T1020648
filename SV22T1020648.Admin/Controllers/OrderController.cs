using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020648.BusinessLayers;
using SV22T1020648.Models.Catalog;
using SV22T1020648.Models.Common;
using SV22T1020648.Models.Sales;

namespace SV22T1020648.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.Sales}")]
    /// <summary>
    /// Các chức năng liên quan đến đơn hàng
    /// </summary>
    public class OrderController : Controller
    {
        /// <summary>
        /// Lưu điều kiện tìm kiếm vào session
        /// </summary>
        private const string ORDER_SEARCH = "OrderSearchInput";

        /// <summary>
        /// Trang chính
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH);

            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.Pagesize,
                    SearchValue = "",
                    Status = 0, // 0 = tất cả
                    DateFrom = null,
                    DateTo = null
                };
            }

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm + phân trang
        /// </summary>
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            if (input.DateFrom.HasValue && input.DateTo.HasValue && input.DateFrom.Value > input.DateTo.Value)
            {
                ViewBag.ErrorMessage = "Thời gian 'Từ ngày' không được lớn hơn 'Đến ngày'.";

                return View(new PagedResult<OrderViewInfo> { DataItems = new List<OrderViewInfo>() });
            }

            var result = await SalesDataService.ListOrdersAsync(input);

            // Chỉ lưu điều kiện tìm kiếm vào session khi dữ liệu hợp lệ
            ApplicationContext.SetSessionData(ORDER_SEARCH, input);

            return View(result);
        }
        private const string SEARCH_PRODUCT = "SearchProductToSale";
        /// <summary>
        /// Giao diện thực hiện các chức năng lập đơn hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(SEARCH_PRODUCT);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 3,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            }
            return View(input);
        }
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(SEARCH_PRODUCT, input);
            return View(result);
        }

        public IActionResult ShowCart()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return View(cart);
        }
        /// <summary>
        /// Hiển thị thông tin của 1 đơn hàng và điều hướng đến các chức năng xử lý đơn hàng
        /// </summary>
        /// <param name="id">Mã của đơn hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Detail(int id)
        {
            var model = await SalesDataService.GetOrderAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            var details = await SalesDataService.ListDetailsAsync(id);
            ViewBag.ListDetails = details ?? new List<OrderDetailViewInfo>();

            return View(model);
        }
        /// <summary>
        /// Thêm hàng vào giỏ hàng
        /// </summary>
        /// <param name="id"></param>
        /// <param name="productId"></param>
        /// <param name="quantity"></param>
        /// <param name="price"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productId = 0, int quantity = 0, decimal price = 0)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));

            if (price <= 0)
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));

            var product = await CatalogDataService.GetProductAsync(productId);
            if (product == null)
            {
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));
            }
            if (!product.IsSelling)
                return Json(new ApiResult(0, "Mặt hàng này đã nhưng bán"));

            //Thêm hàng vào giỏ
            var item = new OrderDetailViewInfo()
            {
                ProductID = productId,
                Quantity = quantity,
                SalePrice = price,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.pnj"
            };
            ShoppingCartService.AddCartItem(item);
            return Json(new ApiResult(1, "Đã thêm vào giỏ hàng"));
        }
        /// <summary>
        /// Xóa mặt hàng ra khỏi giỏ hàng
        /// </summary>
        /// <param name="productId">Mã mặt hàng cần xóa khỏi giỏ hàng</param>
        /// <returns></returns>


        public IActionResult DeleteCartItem(int productId = 0)
        {
            //Post: Xóa hàng khỏi giỏ
            if (Request.Method == "POST")
            {
                ShoppingCartService.RemoveCartItem(productId);
                return Json(new ApiResult(1, ""));
            }
            //Get: Hiển thị giao diện để xác nhận
            ViewBag.ProductID = productId;
            return PartialView();
        }

        /// <summary>
        /// Xóa giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult ClearCart()
        {
            //Post: Xóa giỏ hàng
            if (Request.Method == "POST")
            {
                ShoppingCartService.ClearCart();
                return Json(new ApiResult(1, ""));
            }
            //Get: 
            return PartialView();
        }
        /// <summary>
        /// Cập nhật thông tin của 1 mặt hàng trong giỏ hàng
        /// </summary>
        /// <param name="productId">Mã mặt hàng cần thay đổi số lượng hoặc giá bán</param>
        /// <returns></returns>
        public IActionResult EditCartItem(int productId)
        {
            var item = ShoppingCartService.GetCartItem(productId);
            return PartialView(item);
        }

        public IActionResult UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            if (salePrice < 0)
                return Json(new ApiResult(1, "Giá phải lớn hơn 0"));
            //Update trong giỏ hàng
            ShoppingCartService.UpdateCartItem(productID, quantity, salePrice);
            return Json(new ApiResult(1, ""));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
            var cart = ShoppingCartService.GetShoppingCart();
            if (cart.Count == 0)
            {
                return Json(new ApiResult(0, "Giỏ hàng rỗng"));
            }
            //Lập đơn hàng và ghi chi tiết của đơn hàng
            //TODO: có thể add all dl vào
            //Tạo đơn hàng mới và bổ sung vào cơ sở dữ liệu
            int orderID = await SalesDataService.AddOrderAsync(customerID, province, address);
            foreach (var item in cart)
            {
                item.OrderID = orderID;
                await SalesDataService.AddDetailAsync(item);
            }

            //clear giỏ hàng
            ShoppingCartService.ClearCart();
            return Json(new ApiResult(orderID, ""));
        }
        /// <summary>
        /// Cập nhật thông tin order
        /// </summary>
        /// <param name="id">Mã order cần cần cập nhật</param>
        /// <returns></returns>
        public IActionResult Edit(int id)
        {
            ViewData["Title"] = "Cập nhật đơn hàng";
            return View();
        }
        /// <summary>
        /// Hiển thị hộp thoại xác nhận duyệt đơn hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Accept(int id = 0)
        {
            if (id <= 0)
                return Content("Đơn hàng không hợp lệ.");

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return Content("Không tìm thấy đơn hàng.");

            return View(order);
        }

        /// <summary>
        /// Xử lý duyệt chấp nhận đơn hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(OrderViewInfo model)
        {
            if (model.OrderID <= 0)
                return Content("Đơn hàng không hợp lệ.");

            // Lấy thông tin User đang đăng nhập từ Cookie
            var userData = User.GetUserData();

            // Ép kiểu UserId sang int (với giá trị mặc định là 0 nếu null)
            int employeeID = Convert.ToInt32(userData?.UserId ?? "0");

            // Kiểm tra xem có lấy được mã nhân viên hợp lệ không
            if (employeeID <= 0)
            {
                return Content("Không thể xác định thông tin nhân viên đang xử lý. Vui lòng đăng nhập lại.");
            }

            // Gọi service với employeeID động
            bool result = await SalesDataService.AcceptOrderAsync(model.OrderID, employeeID);

            if (!result)
            {
                ViewBag.ErrorMessage = "Không thể duyệt đơn hàng. Có thể đơn hàng không tồn tại hoặc không còn ở trạng thái chờ duyệt.";
                var order = await SalesDataService.GetOrderAsync(model.OrderID);
                return View(order);
            }

            return RedirectToAction("Detail", new { id = model.OrderID });
        }

        /// <summary>
        /// Chuyển trạng thái đang giao hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Shipping(int id = 0)
        {
            if (id <= 0)
                return Content("Đơn hàng không hợp lệ.");

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return Content("Không tìm thấy đơn hàng.");

            return PartialView(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Shipping(int orderID = 0, int shipperID = 0)
        {
            if (orderID <= 0)
                return Content("Đơn hàng không hợp lệ.");

            var order = await SalesDataService.GetOrderAsync(orderID);
            if (order == null)
                return Content("Không tìm thấy đơn hàng.");

            if(shipperID <= 0)
            {

                TempData["ErrorMessage"] = "Vui lòng chọn người giao hàng.";
                return RedirectToAction("Detail", new { id = orderID });
            }

            bool result = await SalesDataService.ShipOrderAsync(orderID, shipperID);

            if (!result)
            {
                ViewBag.ErrorMessage = "Không thể chuyển đơn hàng sang trạng thái giao hàng.";
                return View(order);
            }

            return RedirectToAction("Detail", new { id = orderID });
        }
        /// <summary>
        /// Xác nhận đơn hàng đã hoàn tất thành công
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Finish(int id = 0)
        {
            if (id <= 0)
                return Content("Đơn hàng không hợp lệ.");

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return Content("Không tìm thấy đơn hàng.");

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finish(OrderViewInfo model)
        {
            if (model.OrderID <= 0)
                return Content("Đơn hàng không hợp lệ.");

            bool result = await SalesDataService.CompleteOrderAsync(model.OrderID);

            if (!result)
            {
                ViewBag.ErrorMessage = "Không thể hoàn tất đơn hàng.";
                var order = await SalesDataService.GetOrderAsync(model.OrderID);
                return View(order);
            }

            return RedirectToAction("Detail", new { id = model.OrderID });
        }

        /// <summary>
        /// Hiển thị giao diện xác nhận từ chối đơn hàng
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Reject(int id = 0)
        {
            if (id <= 0)
                return Content("Đơn hàng không hợp lệ.");

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return Content("Không tìm thấy đơn hàng.");

            return View(order);
        }

        /// <summary>
        /// Xử lý từ chối đơn hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(OrderViewInfo model) // Hoặc truyền thẳng int id nếu Form của bạn chỉ gửi ID
        {
            if (model.OrderID <= 0)
                return Content("Đơn hàng không hợp lệ.");

            // Lấy thông tin User đang đăng nhập từ Cookie
            var userData = User.GetUserData();

            // Ép kiểu UserId sang int (giống y hệt Accept)
            int employeeID = Convert.ToInt32(userData?.UserId ?? "0");

            if (employeeID <= 0)
            {
                return Content("Không thể xác định thông tin nhân viên đang xử lý. Vui lòng đăng nhập lại.");
            }

            // Gọi service chuyển trạng thái đơn hàng sang Từ chối
            bool result = await SalesDataService.RejectOrderAsync(model.OrderID, employeeID);

            if (!result)
            {
                ViewBag.ErrorMessage = "Không thể từ chối đơn hàng. Đơn hàng có thể đã bị hủy hoặc không ở trạng thái chờ duyệt.";
                var order = await SalesDataService.GetOrderAsync(model.OrderID);
                return View(order);
            }

            // Nếu thành công, quay lại trang chi tiết đơn hàng
            return RedirectToAction("Detail", new { id = model.OrderID });
        }

        /// <summary>
        /// Hủy đơn hàng (đã duyệt nhưng bị hủy)
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Cancel(int id = 0)
        {
            if (id <= 0)
                return Content("Đơn hàng không hợp lệ.");

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return Content("Không tìm thấy đơn hàng.");

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(OrderViewInfo model)
        {
            if (model.OrderID <= 0)
                return Content("Đơn hàng không hợp lệ.");

            bool result = await SalesDataService.CancelOrderAsync(model.OrderID);

            if (!result)
            {
                ViewBag.ErrorMessage = "Không thể hủy đơn hàng.";
                var order = await SalesDataService.GetOrderAsync(model.OrderID);
                return View(order);
            }

            return RedirectToAction("Detail", new { id = model.OrderID });
        }

        /// <summary>
        /// Xóa đơn hàng khỏi hệ thống
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int orderID)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await SalesDataService.DeleteOrderAsync(orderID);
                    return Json(new ApiResult(1));
                }
                catch
                {
                    return Json(new ApiResult(0, "Xoá đơn hàng thất bại"));
                }
            }

            var model = await SalesDataService.GetOrderAsync(orderID);
            return PartialView(model);
        }
    }
}