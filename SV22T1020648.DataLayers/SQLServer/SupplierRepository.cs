using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.Models.Common;
using SV22T1020648.Models.Partner;

namespace SV22T1020648.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu bảng Supplier trong SQL Server
    /// thông qua Dapper và cài đặt interface IGenericRepository
    /// </summary>
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor nhận chuỗi kết nối CSDL
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối tới SQL Server</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Truy vấn danh sách nhà cung cấp có phân trang và tìm kiếm
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả dữ liệu dạng phân trang</returns>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            var result = new PagedResult<Supplier>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string searchValue = $"%{input.SearchValue}%";

            string countSql = @"SELECT COUNT(*)
                                FROM Suppliers
                                WHERE SupplierName LIKE @search
                                   OR ContactName LIKE @search";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql,
                new { search = searchValue });

            if (input.PageSize == 0)
            {
                string sql = @"SELECT *
                               FROM Suppliers
                               WHERE SupplierName LIKE @search
                                  OR ContactName LIKE @search
                               ORDER BY SupplierName";

                var data = await connection.QueryAsync<Supplier>(sql,
                    new { search = searchValue });

                result.DataItems = data.ToList();
            }
            else
            {
                string sql = @"SELECT *
                               FROM Suppliers
                               WHERE SupplierName LIKE @search
                                  OR ContactName LIKE @search
                               ORDER BY SupplierName
                               OFFSET @offset ROWS
                               FETCH NEXT @pagesize ROWS ONLY";

                var data = await connection.QueryAsync<Supplier>(sql, new
                {
                    search = searchValue,
                    offset = input.Offset,
                    pagesize = input.PageSize
                });

                result.DataItems = data.ToList();
            }

            return result;
        }

        /// <summary>
        /// Lấy thông tin một nhà cung cấp theo mã
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>Thông tin Supplier hoặc null nếu không tồn tại</returns>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT *
                           FROM Suppliers
                           WHERE SupplierID = @id";

            return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { id });
        }

        /// <summary>
        /// Thêm mới một nhà cung cấp vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu nhà cung cấp cần thêm</param>
        /// <returns>Mã SupplierID được tạo</returns>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"INSERT INTO Suppliers
                           (SupplierName, ContactName, Province, Address, Phone, Email)
                           VALUES
                           (@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                           SELECT SCOPE_IDENTITY();";

            var id = await connection.ExecuteScalarAsync<int>(sql, data);

            return id;
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp
        /// </summary>
        /// <param name="data">Dữ liệu nhà cung cấp cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"UPDATE Suppliers
                           SET SupplierName = @SupplierName,
                               ContactName = @ContactName,
                               Province = @Province,
                               Address = @Address,
                               Phone = @Phone,
                               Email = @Email
                           WHERE SupplierID = @SupplierID";

            int rows = await connection.ExecuteAsync(sql, data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa nhà cung cấp theo mã
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"DELETE FROM Suppliers
                           WHERE SupplierID = @id";

            int rows = await connection.ExecuteAsync(sql, new { id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra nhà cung cấp có đang được sử dụng trong bảng Products hay không
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>true nếu có dữ liệu liên quan</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT COUNT(*)
                           FROM Products
                           WHERE SupplierID = @id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });

            return count > 0;
        }
    }
}