namespace SV22T1020648.Models.Dashboard
{
    /// <summary>
    /// Thông tin tổng hợp hiển thị trên trang Dashboard
    /// </summary>
    public class DashboardInfo
    {
        /// <summary>
        /// Doanh thu trong ngày hôm nay
        /// </summary>
        public decimal TodayRevenue { get; set; }

        /// <summary>
        /// Tổng số đơn hàng
        /// </summary>
        public int OrderCount { get; set; }

        /// <summary>
        /// Tổng số khách hàng
        /// </summary>
        public int CustomerCount { get; set; }

        /// <summary>
        /// Tổng số mặt hàng
        /// </summary>
        public int ProductCount { get; set; }

        /// <summary>
        /// Danh sách doanh thu theo tháng
        /// </summary>
        public List<MonthlyRevenueItem> MonthlyRevenues { get; set; } = new();

        /// <summary>
        /// Danh sách sản phẩm bán chạy
        /// </summary>
        public List<TopSellingProductItem> TopSellingProducts { get; set; } = new();

        /// <summary>
        /// Danh sách đơn hàng cần xử lý
        /// </summary>
        public List<PendingOrderItem> PendingOrders { get; set; } = new();
    }

    /// <summary>
    /// Thông tin doanh thu theo tháng
    /// </summary>
    public class MonthlyRevenueItem
    {
        /// <summary>
        /// Tên tháng hiển thị
        /// </summary>
        public string MonthLabel { get; set; } = "";

        /// <summary>
        /// Doanh thu của tháng
        /// </summary>
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// Thông tin sản phẩm bán chạy
    /// </summary>
    public class TopSellingProductItem
    {
        /// <summary>
        /// Tên mặt hàng
        /// </summary>
        public string ProductName { get; set; } = "";

        /// <summary>
        /// Số lượng đã bán
        /// </summary>
        public int QuantitySold { get; set; }
    }

    /// <summary>
    /// Thông tin đơn hàng cần xử lý
    /// </summary>
    public class PendingOrderItem
    {
        /// <summary>
        /// Mã đơn hàng
        /// </summary>
        public int OrderID { get; set; }

        /// <summary>
        /// Tên khách hàng
        /// </summary>
        public string CustomerName { get; set; } = "";

        /// <summary>
        /// Thời gian lập đơn hàng
        /// </summary>
        public DateTime OrderTime { get; set; }

        /// <summary>
        /// Tổng tiền đơn hàng
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Trạng thái đơn hàng
        /// </summary>
        public string StatusDescription { get; set; } = "";
    }
}