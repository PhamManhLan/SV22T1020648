using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.Models.Common;
using SV22T1020648.Models.Partner;

namespace SV22T1020648.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các thao tác dữ liệu cho Khách hàng trên SQL Server
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách khách hàng có phân trang
        /// </summary>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Customer>()
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
                    Offset = input.Offset,
                    PageSize = input.PageSize
                };

                string sql = "";

                if (input.PageSize == 0)
                {
                    sql = @"
                        SELECT COUNT(*) 
                        FROM Customers 
                        WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue) OR (Email LIKE @SearchValue);

                        SELECT * FROM Customers 
                        WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue) OR (Email LIKE @SearchValue)
                        ORDER BY CustomerName;";
                }
                else
                {
                    sql = @"
                        SELECT COUNT(*) 
                        FROM Customers 
                        WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue) OR (Email LIKE @SearchValue);

                        SELECT * FROM Customers 
                        WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue) OR (Email LIKE @SearchValue)
                        ORDER BY CustomerName
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
                }

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Customer>()).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy thông tin khách hàng theo ID
        /// </summary>
        public async Task<Customer?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Customers WHERE CustomerID = @id";
                return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { id });
            }
        }

        /// <summary>
        /// Thêm mới khách hàng
        /// </summary>
        public async Task<int> AddAsync(Customer data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    INSERT INTO Customers(CustomerName, ContactName, Province, Address, Phone, Email, IsLocked)
                    VALUES(@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @IsLocked);
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Cập nhật khách hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"
                    UPDATE Customers 
                    SET CustomerName = @CustomerName, 
                        ContactName = @ContactName, 
                        Province = @Province, 
                        Address = @Address, 
                        Phone = @Phone, 
                        Email = @Email,
                        IsLocked = @IsLocked
                    WHERE CustomerID = @CustomerID";

                int rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Xóa khách hàng
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Customers WHERE CustomerID = @id";
                int rowsAffected = await connection.ExecuteAsync(sql, new { id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra khách hàng có dữ liệu liên quan (Đơn hàng) không
        /// (Hàm này KHÔNG CÓ chữ Async ở tên để khớp với Interface, 
        /// nhưng bên trong vẫn chạy bất đồng bộ chuẩn chỉ)
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT CASE WHEN EXISTS(SELECT 1 FROM Orders WHERE CustomerID = @id) THEN 1 ELSE 0 END";
                return await connection.ExecuteScalarAsync<bool>(sql, new { id });
            }
        }

        /// <summary>
        /// Kiểm tra email có bị trùng lặp hay không
        /// </summary>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    SELECT CASE 
                        WHEN EXISTS(SELECT 1 FROM Customers WHERE Email = @Email AND CustomerID <> @CustomerID) 
                        THEN 1 
                        ELSE 0 
                    END";

                bool isDuplicate = await connection.ExecuteScalarAsync<bool>(sql, new { Email = email, CustomerID = id });

                return !isDuplicate;
            }
        }
    }
}