using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.Models.Common;
using SV22T1020648.Models.HR;

namespace SV22T1020648.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các thao tác dữ liệu cho Nhân viên trên SQL Server
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhân viên có phân trang
        /// </summary>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Employee>()
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
                        FROM Employees 
                        WHERE (FullName LIKE @SearchValue) OR (Address LIKE @SearchValue) OR (Phone LIKE @SearchValue) OR (Email LIKE @SearchValue);
                        
                        SELECT * FROM Employees 
                        WHERE (FullName LIKE @SearchValue) OR (Address LIKE @SearchValue) OR (Phone LIKE @SearchValue) OR (Email LIKE @SearchValue)
                        ORDER BY FullName;";
                }
                else
                {
                    sql = @"
                        SELECT COUNT(*) 
                        FROM Employees 
                        WHERE (FullName LIKE @SearchValue) OR (Address LIKE @SearchValue) OR (Phone LIKE @SearchValue) OR (Email LIKE @SearchValue);
                        
                        SELECT * FROM Employees 
                        WHERE (FullName LIKE @SearchValue) OR (Address LIKE @SearchValue) OR (Phone LIKE @SearchValue) OR (Email LIKE @SearchValue)
                        ORDER BY FullName
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
                }

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Employee>()).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy thông tin một nhân viên theo mã ID
        /// </summary>
        public async Task<Employee?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Employees WHERE EmployeeID = @id";
                return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { id });
            }
        }

        /// <summary>
        /// Thêm mới một nhân viên
        /// </summary>
        public async Task<int> AddAsync(Employee data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Bổ sung thêm cột RoleNames vào câu lệnh INSERT
                string sql = @"
                    INSERT INTO Employees(FullName, BirthDate, Address, Phone, Email, Photo, IsWorking, RoleNames)
                    VALUES(@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking, @RoleNames);
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Bổ sung thêm RoleNames = @RoleNames vào câu lệnh SET
                string sql = @"
                    UPDATE Employees 
                    SET FullName = @FullName, 
                        BirthDate = @BirthDate, 
                        Address = @Address, 
                        Phone = @Phone, 
                        Email = @Email,
                        Photo = @Photo,
                        IsWorking = @IsWorking,
                        RoleNames = @RoleNames
                    WHERE EmployeeID = @EmployeeID";

                int rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Xóa nhân viên
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM Employees WHERE EmployeeID = @id";
                int rowsAffected = await connection.ExecuteAsync(sql, new { id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra nhân viên có liên quan đến các bảng khác (ví dụ: Orders) không
        /// (Đúng tên IsUsed và trả về Task<bool> theo Interface)
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT CASE WHEN EXISTS(SELECT 1 FROM Orders WHERE EmployeeID = @id) THEN 1 ELSE 0 END";
                return await connection.ExecuteScalarAsync<bool>(sql, new { id });
            }
        }

        /// <summary>
        /// Kiểm tra email có hợp lệ (không bị trùng lặp) hay không
        /// </summary>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Tối ưu dùng EXISTS
                string sql = @"
                    SELECT CASE 
                        WHEN EXISTS(SELECT 1 FROM Employees WHERE Email = @Email AND EmployeeID <> @id) 
                        THEN 1 
                        ELSE 0 
                    END";

                bool isDuplicate = await connection.ExecuteScalarAsync<bool>(sql, new { Email = email, id = id });
                return !isDuplicate;
            }
        }
    }
}