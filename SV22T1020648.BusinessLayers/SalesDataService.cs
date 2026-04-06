using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.DataLayers.SQLServer;
using SV22T1020648.Models.Common;
using SV22T1020648.Models.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace SV22T1020648.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bán hàng
    /// bao gồm: đơn hàng (Order) và chi tiết đơn hàng (OrderDetail).
    /// </summary>
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        #region Order

        /// <summary>
        /// Khởi tạo đơn hàng mới kèm theo chi tiết (Dùng cho Checkout)
        /// </summary>
        public static async Task<int> InitOrderAsync(int customerID, string province, string address, IEnumerable<OrderDetail> details)
        {
            try
            {
                // 1. Tạo Order mới để lấy OrderID
                int orderID = await AddOrderAsync(customerID, province, address);

                if (orderID > 0)
                {
                    // 2. Lặp qua giỏ hàng, gán OrderID vừa tạo và lưu vào Database
                    foreach (var item in details)
                    {
                        item.OrderID = orderID;
                        await AddDetailAsync(item);
                    }
                    return orderID; // Trả về mã đơn hàng nếu thành công
                }
                return 0;
            }
            catch
            {
                // Xử lý lỗi (Nếu có lỗi xảy ra trong quá trình lưu chi tiết)
                return 0;
            }
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng
        /// </summary>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }

        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        public static async Task<int> AddOrderAsync(int customerID = 0, string province = "", string address = "")
        {
            var order = new Order
            {
                CustomerID = customerID == 0 ? null : customerID,
                DeliveryProvince = province,
                DeliveryAddress = address,
                Status = OrderStatusEnum.New,
                OrderTime = DateTime.Now
            };
            return await orderDB.AddAsync(order);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        public static async Task<bool> UpdateOrderAsync(Order data)
        {
            return await orderDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            return await orderDB.DeleteAsync(orderID);
        }

        #endregion

        #region Order Status Processing

        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null || order.Status != OrderStatusEnum.New) return false;

            order.EmployeeID = employeeID;
            order.AcceptTime = DateTime.Now;
            order.Status = OrderStatusEnum.Accepted;

            return await orderDB.UpdateAsync(order);
        }

        public static async Task<bool> RejectOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null || order.Status != OrderStatusEnum.New) return false;

            order.EmployeeID = employeeID;
            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Rejected;

            return await orderDB.UpdateAsync(order);
        }

        public static async Task<bool> CancelOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null || (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)) return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Cancelled;

            return await orderDB.UpdateAsync(order);
        }

        public static async Task<bool> ShipOrderAsync(int orderID, int shipperID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null || order.Status != OrderStatusEnum.Accepted) return false;

            order.ShipperID = shipperID;
            order.ShippedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Shipping;

            return await orderDB.UpdateAsync(order);
        }

        public static async Task<bool> CompleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null || order.Status != OrderStatusEnum.Shipping) return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Completed;

            return await orderDB.UpdateAsync(order);
        }

        #endregion

        #region Order Detail

        public static async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            return await orderDB.ListDetailsAsync(orderID);
        }

        public static async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            return await orderDB.GetDetailAsync(orderID, productID);
        }

        public static async Task<bool> AddDetailAsync(OrderDetail data)
        {
            return await orderDB.AddDetailAsync(data);
        }

        public static async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            return await orderDB.UpdateDetailAsync(data);
        }

        public static async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            return await orderDB.DeleteDetailAsync(orderID, productID);
        }

        #endregion

        /// <summary>
        /// Lấy danh sách lịch sử đơn hàng của 1 khách hàng (Dùng cho Customer Site)
        /// </summary>
        public static async Task<List<OrderViewInfo>> ListOrdersOfCustomerAsync(int customerID)
        {
            return await orderDB.ListOrdersOfCustomerAsync(customerID);
        }
    }
}