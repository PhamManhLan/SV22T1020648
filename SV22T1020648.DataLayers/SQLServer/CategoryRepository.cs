using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.Models.Catalog;
using SV22T1020648.Models.Common;

namespace SV22T1020648.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu bảng Categories trong SQL Server
    /// thông qua Dapper và cài đặt interface IGenericRepository
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor nhận chuỗi kết nối CSDL
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối tới SQL Server</param>
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Truy vấn danh sách loại hàng có phân trang và tìm kiếm
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả dữ liệu dạng phân trang</returns>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            var result = new PagedResult<Category>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string searchValue = $"%{input.SearchValue}%";

            string countSql = @"SELECT COUNT(*)
                                FROM Categories
                                WHERE CategoryName LIKE @search";

            result.RowCount = await connection.ExecuteScalarAsync<int>(
                countSql,
                new { search = searchValue }
            );

            if (input.PageSize == 0)
            {
                string sql = @"SELECT *
                               FROM Categories
                               WHERE CategoryName LIKE @search
                               ORDER BY CategoryID";

                var data = await connection.QueryAsync<Category>(
                    sql,
                    new { search = searchValue }
                );

                result.DataItems = data.ToList();
            }
            else
            {
                string sql = @"SELECT *
                               FROM Categories
                               WHERE CategoryName LIKE @search
                               ORDER BY CategoryID
                               OFFSET @offset ROWS
                               FETCH NEXT @pagesize ROWS ONLY";

                var data = await connection.QueryAsync<Category>(
                    sql,
                    new
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
        /// Lấy thông tin một loại hàng theo mã
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>Thông tin Category hoặc null nếu không tồn tại</returns>
        public async Task<Category?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT *
                           FROM Categories
                           WHERE CategoryID = @id";

            return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { id });
        }

        /// <summary>
        /// Thêm mới một loại hàng vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng cần thêm</param>
        /// <returns>Mã CategoryID được tạo</returns>
        public async Task<int> AddAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"INSERT INTO Categories
                           (CategoryName, Description)
                           VALUES
                           (@CategoryName, @Description);
                           SELECT SCOPE_IDENTITY();";

            int id = await connection.ExecuteScalarAsync<int>(sql, data);

            return id;
        }

        /// <summary>
        /// Cập nhật thông tin loại hàng
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"UPDATE Categories
                           SET CategoryName = @CategoryName,
                               Description = @Description
                           WHERE CategoryID = @CategoryID";

            int rows = await connection.ExecuteAsync(sql, data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa loại hàng theo mã
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"DELETE FROM Categories
                           WHERE CategoryID = @id";

            int rows = await connection.ExecuteAsync(sql, new { id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra loại hàng có đang được sử dụng trong bảng Products hay không
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>true nếu có dữ liệu liên quan</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT COUNT(*)
                           FROM Products
                           WHERE CategoryID = @id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });

            return count > 0;
        }
    }
}