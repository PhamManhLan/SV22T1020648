using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.Models.Common;
using SV22T1020648.Models.Sales;

namespace SV22T1020648.DataLayers.SQLServer
{
    /// <summary>
    /// Repository thực hiện các thao tác truy xuất dữ liệu liên quan đến đơn hàng.
    /// Bao gồm các chức năng quản lý đơn hàng và chi tiết đơn hàng.
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối cơ sở dữ liệu.
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến SQL Server</param>
        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy danh sách đơn hàng theo điều kiện tìm kiếm và phân trang.
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Danh sách đơn hàng dạng phân trang</returns>
        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            var result = new PagedResult<OrderViewInfo>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Status = (int)input.Status,
                    DateFrom = input.DateFrom,
                    DateTo = input.DateTo,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                };

                // Điều kiện lọc dữ liệu động
                string condition = @"(@Status = 0 OR o.Status = @Status)
                    AND (@DateFrom IS NULL OR o.OrderTime >= @DateFrom)
                    AND (@DateTo IS NULL OR o.OrderTime <= @DateTo)
                    AND (@SearchValue = '' OR c.CustomerName LIKE @SearchValue 
                                           OR o.DeliveryAddress LIKE @SearchValue
                                           OR o.DeliveryProvince LIKE @SearchValue)";

                string sql = $@"
                    SELECT COUNT(*) 
                    FROM Orders AS o
                    LEFT JOIN Customers AS c ON o.CustomerID = c.CustomerID
                    WHERE {condition};

                    SELECT o.*, 
                           c.CustomerName, c.ContactName AS CustomerContactName, c.Address AS CustomerAddress, 
                           c.Phone AS CustomerPhone, c.Email AS CustomerEmail,
                           e.FullName AS EmployeeName,
                           s.ShipperName, s.Phone AS ShipperPhone
                    FROM Orders AS o
                    LEFT JOIN Customers AS c ON o.CustomerID = c.CustomerID
                    LEFT JOIN Employees AS e ON o.EmployeeID = e.EmployeeID
                    LEFT JOIN Shippers AS s ON o.ShipperID = s.ShipperID
                    WHERE {condition}
                    ORDER BY o.OrderTime DESC, o.OrderID DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<OrderViewInfo>()).ToList();
                }
            }
            return result;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng theo mã.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Thông tin chi tiết đơn hàng</returns>
        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"SELECT o.*, 
                              c.CustomerName, 
                              c.Address AS CustomerPermanentAddress, -- Đổi tên để tránh nhầm
                              c.Phone AS CustomerPhone
                       FROM Orders AS o
                       LEFT JOIN Customers AS c ON o.CustomerID = c.CustomerID
                       WHERE o.OrderID = @OrderID";
                return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { OrderID = orderID });
            }
        }

        /// <summary>
        /// Thêm một đơn hàng mới vào cơ sở dữ liệu.
        /// </summary>
        /// <param name="data">Thông tin đơn hàng cần thêm</param>
        /// <returns>Mã đơn hàng vừa được tạo</returns>
        public async Task<int> AddAsync(Order data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"INSERT INTO Orders(CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, 
                                               EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                               VALUES(@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, 
                                      @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                               SELECT CAST(SCOPE_IDENTITY() as int);";

                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng.
        /// </summary>
        /// <param name="data">Dữ liệu đơn hàng cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Order data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"UPDATE Orders 
                               SET CustomerID = @CustomerID, OrderTime = @OrderTime, 
                                   DeliveryProvince = @DeliveryProvince, DeliveryAddress = @DeliveryAddress, 
                                   EmployeeID = @EmployeeID, AcceptTime = @AcceptTime, 
                                   ShipperID = @ShipperID, ShippedTime = @ShippedTime, 
                                   FinishedTime = @FinishedTime, Status = @Status
                               WHERE OrderID = @OrderID";

                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        /// <summary>
        /// Xóa một đơn hàng theo mã.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng cần xóa</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Xóa chi tiết đơn hàng trước do ràng buộc khóa ngoại
                string sql = @"DELETE FROM OrderDetails WHERE OrderID = @OrderID;
                               DELETE FROM Orders WHERE OrderID = @OrderID;";

                return await connection.ExecuteAsync(sql, new { OrderID = orderID }) > 0;
            }
        }


        /// <summary>
        /// Lấy danh sách chi tiết sản phẩm của một đơn hàng.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Danh sách chi tiết đơn hàng</returns>
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"SELECT od.*, p.ProductName, p.Unit, p.Photo
                               FROM OrderDetails AS od
                               JOIN Products AS p ON od.ProductID = p.ProductID
                               WHERE od.OrderID = @OrderID";

                return (await connection.QueryAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID })).ToList();
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một sản phẩm trong đơn hàng.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã sản phẩm</param>
        /// <returns>Chi tiết sản phẩm trong đơn hàng</returns>
        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"SELECT od.*, p.ProductName, p.Unit, p.Photo
                               FROM OrderDetails AS od
                               JOIN Products AS p ON od.ProductID = p.ProductID
                               WHERE od.OrderID = @OrderID AND od.ProductID = @ProductID";

                return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(
                    sql, new { OrderID = orderID, ProductID = productID });
            }
        }

        /// <summary>
        /// Thêm sản phẩm vào chi tiết đơn hàng.
        /// </summary>
        /// <param name="data">Thông tin chi tiết đơn hàng</param>
        /// <returns>true nếu thêm thành công</returns>
        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"INSERT INTO OrderDetails(OrderID, ProductID, Quantity, SalePrice)
                               VALUES(@OrderID, @ProductID, @Quantity, @SalePrice)";

                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        /// <summary>
        /// Cập nhật thông tin sản phẩm trong chi tiết đơn hàng.
        /// </summary>
        /// <param name="data">Thông tin chi tiết cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"UPDATE OrderDetails 
                               SET Quantity = @Quantity, SalePrice = @SalePrice
                               WHERE OrderID = @OrderID AND ProductID = @ProductID";

                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        /// <summary>
        /// Xóa một sản phẩm khỏi chi tiết đơn hàng.
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã sản phẩm</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = "DELETE FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID";

                return await connection.ExecuteAsync(sql, new { OrderID = orderID, ProductID = productID }) > 0;
            }
        }
        /// <summary>
        /// Lấy danh sách đơn hàng của một khách hàng cụ thể
        /// </summary>
        public async Task<List<OrderViewInfo>> ListOrdersOfCustomerAsync(int customerID)
        {
            using var connection = new SqlConnection(_connectionString);

            // Lưu ý: Cấu trúc câu Select có thể thay đổi tùy thuộc vào View của bạn trong SQL Server.
            // Nếu bạn có một View nối bảng chi tiết hơn, hãy thay đổi tên bảng/view ở đây.
            string sql = @"SELECT * FROM Orders 
                   WHERE CustomerID = @CustomerID 
                   ORDER BY OrderTime DESC";

            var data = await connection.QueryAsync<OrderViewInfo>(sql, new { CustomerID = customerID });

            return data.ToList();
        }
    }
}