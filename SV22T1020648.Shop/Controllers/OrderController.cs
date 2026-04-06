using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020648.BusinessLayers;
using SV22T1020648.Models.Sales;

namespace SV22T1020648.Shop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private const string SHOPPING_CART = "ShoppingCart";

        /// <summary>
        /// Hiển thị giao diện giỏ hàng của khách hàng
        /// </summary>
        public IActionResult ShoppingCart()
        {
            var model = GetShoppingCart();
            return View(model);
        }
        /// <summary>
        /// Helper lấy giỏ hàng từ Session sử dụng Model có sẵn
        /// </summary>
        private List<OrderDetailViewInfo> GetShoppingCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(SHOPPING_CART);
            if (cart == null)
            {
                cart = new List<OrderDetailViewInfo>();
                ApplicationContext.SetSessionData(SHOPPING_CART, cart);
            }
            return cart;
        }

        /// <summary>
        /// Thêm hàng vào giỏ
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (quantity <= 0) return Json(new { success = false, message = "Số lượng không hợp lệ" });

            var cart = GetShoppingCart();
            var item = cart.FirstOrDefault(m => m.ProductID == productId);

            if (item == null)
            {
                var product = await CatalogDataService.GetProductAsync(productId);
                if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại" });

                cart.Add(new OrderDetailViewInfo
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    Photo = product.Photo ?? "nophoto.png",
                    Unit = product.Unit,
                    Quantity = quantity,
                    SalePrice = product.Price
                });
            }
            else
            {
                item.Quantity += quantity;
            }

            ApplicationContext.SetSessionData(SHOPPING_CART, cart);
            return Json(new { success = true, cartCount = cart.Count });
        }

        /// <summary>
        /// Xóa mặt hàng khỏi giỏ
        /// </summary>
        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetShoppingCart();
            var item = cart.FirstOrDefault(m => m.ProductID == id);

            if (item != null)
            {
                cart.Remove(item);
                ApplicationContext.SetSessionData(SHOPPING_CART, cart);
            }
            return RedirectToAction("ShoppingCart");
        }
        /// <summary>
        /// Cập nhật số lượng mặt hàng trong giỏ
        /// </summary>
        [HttpPost]
        public IActionResult UpdateCart(int productId, int quantity)
        {
            if (quantity <= 0) return Json(new { success = false, message = "Số lượng không hợp lệ" });

            var cart = GetShoppingCart();
            var item = cart.FirstOrDefault(m => m.ProductID == productId);

            if (item == null) return Json(new { success = false, message = "Sản phẩm không có trong giỏ" });

            item.Quantity = quantity;
            ApplicationContext.SetSessionData(SHOPPING_CART, cart);

            return Json(new { success = true });
        }

        /// <summary>
        /// Trang nhập thông tin thanh toán
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetShoppingCart();
            if (!cart.Any()) return RedirectToAction("ShoppingCart");

            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login", "Account");

            var customer = await PartnerDataService.GetCustomerAsync(int.Parse(userData.UserId));

            ViewBag.CustomerName = customer?.CustomerName ?? userData.DisplayName;
            ViewBag.CustomerPhone = customer?.Phone ?? userData.Phone;
            ViewBag.CustomerAddress = customer?.Address ?? userData.Address;
            ViewBag.CustomerProvince = customer?.Province ?? userData.Province;
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();

            return View(cart);
        }
        /// <summary>
        /// Xác nhận đặt hàng và lưu vào Database
        /// </summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCheckout(string deliveryProvince, string deliveryAddress)
        {
            var cart = GetShoppingCart();
            if (!cart.Any()) return RedirectToAction("ShoppingCart");

            if (string.IsNullOrWhiteSpace(deliveryProvince) || string.IsNullOrWhiteSpace(deliveryAddress))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ tỉnh/thành và địa chỉ giao hàng.");
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
                ViewBag.CustomerProvince = deliveryProvince;
                ViewBag.CustomerAddress = deliveryAddress;
                return View("Checkout", cart);
            }

            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login", "Account");

            var orderDetails = cart.Select(item => new OrderDetail
            {
                ProductID = item.ProductID,
                Quantity = item.Quantity,
                SalePrice = item.SalePrice
            }).ToList();

            int orderId = await SalesDataService.InitOrderAsync(
                int.Parse(userData.UserId), deliveryProvince, deliveryAddress, orderDetails
            );

            if (orderId > 0)
            {
                ClearCart();
                TempData["SuccessMessage"] = "Đặt hàng thành công!";
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Không thể tạo đơn hàng. Vui lòng thử lại.");
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();
            return View("Checkout", cart);
        }
        /// <summary>
        /// Xóa sạch giỏ hàng
        /// </summary>
        public IActionResult ClearCart()
        {
            ApplicationContext.SetSessionData(SHOPPING_CART, new List<OrderDetailViewInfo>());
            return RedirectToAction("ShoppingCart");
        }


        /// <summary>
        /// Theo dõi lịch sử mua hàng của cá nhân
        /// </summary>
        public async Task<IActionResult> History()
        {
            var userData = User.GetUserData();
            if (userData == null) return RedirectToAction("Login", "Account");

            var orders = await SalesDataService.ListOrdersOfCustomerAsync(int.Parse(userData.UserId));
            var model = new List<(OrderViewInfo Order, List<OrderDetailViewInfo> Details)>();

            foreach (var order in orders)
            {
                var details = await SalesDataService.ListDetailsAsync(order.OrderID);
                model.Add((order, details));
            }

            return View(model);
        }

        /// <summary>
        /// Xem chi tiết và trạng thái xử lý của một đơn hàng cụ thể
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            var userData = User.GetUserData();

            if (order == null || order.CustomerID.ToString() != userData?.UserId)
                return RedirectToAction("History");

            var details = await SalesDataService.ListDetailsAsync(id);
            return View((Order: order, Details: details));
        }

        /// <summary>
        /// Hủy đơn hàng đã đặt (chỉ áp dụng cho đơn hàng chưa giao)
        /// </summary>
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            var userData = User.GetUserData();

            if (order == null || order.CustomerID.ToString() != userData?.UserId)
                return RedirectToAction("History");

            if (await SalesDataService.CancelOrderAsync(id))
                TempData["SuccessMessage"] = $"Đã hủy đơn hàng #{id} thành công.";
            else
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng này.";

            return RedirectToAction("History");
        }
    }
}