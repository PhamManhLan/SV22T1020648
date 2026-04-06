using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.Models.Dashboard;

namespace SV22T1020648.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các thao tác dữ liệu cho Dashboard trên SQL Server
    /// </summary>
    public class DashboardRepository : IDashboardRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo đối tượng truy xuất dữ liệu Dashboard
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối cơ sở dữ liệu</param>
        public DashboardRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy thông tin tổng hợp cho Dashboard
        /// </summary>
        /// <returns>Thông tin Dashboard</returns>
        public async Task<DashboardInfo> GetDashboardInfoAsync()
        {
            var result = new DashboardInfo();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // 1. Thống kê nhanh
                string summarySql = @"
                    SELECT
                        ISNULL((
                            SELECT SUM(od.Quantity * od.SalePrice)
                            FROM Orders o
                            JOIN OrderDetails od ON o.OrderID = od.OrderID
                            WHERE CAST(o.OrderTime AS DATE) = CAST(GETDATE() AS DATE)
                        ), 0) AS TodayRevenue,

                        (SELECT COUNT(*) FROM Orders) AS OrderCount,
                        (SELECT COUNT(*) FROM Customers) AS CustomerCount,
                        (SELECT COUNT(*) FROM Products) AS ProductCount
                ";

                result = await connection.QueryFirstOrDefaultAsync<DashboardInfo>(summarySql)
                         ?? new DashboardInfo();

                // 2. Doanh thu 6 tháng gần nhất
                string revenueSql = @"
                    SELECT TOP 6
                        CONCAT(N'Tháng ', MONTH(o.OrderTime)) AS MonthLabel,
                        ISNULL(SUM(od.Quantity * od.SalePrice), 0) AS Revenue
                    FROM Orders o
                    JOIN OrderDetails od ON o.OrderID = od.OrderID
                    WHERE o.OrderTime IS NOT NULL
                    GROUP BY YEAR(o.OrderTime), MONTH(o.OrderTime)
                    ORDER BY YEAR(o.OrderTime) DESC, MONTH(o.OrderTime) DESC
                ";

                var revenues = (await connection.QueryAsync<MonthlyRevenueItem>(revenueSql)).ToList();
                revenues.Reverse(); // đảo lại để hiển thị từ cũ -> mới
                result.MonthlyRevenues = revenues;

                // 3. Top 5 sản phẩm bán chạy
                string topProductSql = @"
                    SELECT TOP 5
                        p.ProductName,
                        SUM(od.Quantity) AS QuantitySold
                    FROM OrderDetails od
                    JOIN Products p ON od.ProductID = p.ProductID
                    GROUP BY p.ProductID, p.ProductName
                    ORDER BY SUM(od.Quantity) DESC
                ";

                result.TopSellingProducts = (await connection.QueryAsync<TopSellingProductItem>(topProductSql)).ToList();

                // 4. Đơn hàng cần xử lý (ví dụ: status 1, 2, 3)
                string pendingOrderSql = @"
                    SELECT TOP 10
                        o.OrderID,
                        c.CustomerName,
                        o.OrderTime,
                        ISNULL(SUM(od.Quantity * od.SalePrice), 0) AS TotalAmount,
                        os.Description AS StatusDescription
                    FROM Orders o
                    JOIN Customers c ON o.CustomerID = c.CustomerID
                    JOIN OrderStatus os ON o.Status = os.Status
                    LEFT JOIN OrderDetails od ON o.OrderID = od.OrderID
                    WHERE o.Status IN (1, 2, 3)
                    GROUP BY o.OrderID, c.CustomerName, o.OrderTime, os.Description
                    ORDER BY o.OrderTime DESC
                ";

                result.PendingOrders = (await connection.QueryAsync<PendingOrderItem>(pendingOrderSql)).ToList();
            }

            return result;
        }
    }
}