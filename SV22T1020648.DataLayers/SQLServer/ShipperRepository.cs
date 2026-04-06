using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.Models.Common;
using SV22T1020648.Models.Partner;

namespace SV22T1020648.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu bảng Shippers trong SQL Server
    /// thông qua Dapper và cài đặt interface IGenericRepository
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor nhận chuỗi kết nối CSDL
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối tới SQL Server</param>
        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Truy vấn danh sách người giao hàng có phân trang và tìm kiếm
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả dữ liệu dạng phân trang</returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            var result = new PagedResult<Shipper>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string searchValue = $"%{input.SearchValue}%";

            string countSql = @"SELECT COUNT(*)
                                FROM Shippers
                                WHERE ShipperName LIKE @search";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql,
                new { search = searchValue });

            if (input.PageSize == 0)
            {
                string sql = @"SELECT *
                               FROM Shippers
                               WHERE ShipperName LIKE @search
                               ORDER BY ShipperName";

                var data = await connection.QueryAsync<Shipper>(sql,
                    new { search = searchValue });

                result.DataItems = data.ToList();
            }
            else
            {
                string sql = @"SELECT *
                               FROM Shippers
                               WHERE ShipperName LIKE @search
                               ORDER BY ShipperName
                               OFFSET @offset ROWS
                               FETCH NEXT @pagesize ROWS ONLY";

                var data = await connection.QueryAsync<Shipper>(sql, new
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
        /// Lấy thông tin một người giao hàng theo mã
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>Thông tin Shipper hoặc null nếu không tồn tại</returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT *
                           FROM Shippers
                           WHERE ShipperID = @id";

            return await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { id });
        }

        /// <summary>
        /// Thêm mới một người giao hàng vào CSDL
        /// </summary>
        /// <param name="data">Dữ liệu người giao hàng cần thêm</param>
        /// <returns>Mã ShipperID được tạo</returns>
        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"INSERT INTO Shippers
                           (ShipperName, Phone)
                           VALUES
                           (@ShipperName, @Phone);
                           SELECT SCOPE_IDENTITY();";

            int id = await connection.ExecuteScalarAsync<int>(sql, data);

            return id;
        }

        /// <summary>
        /// Cập nhật thông tin người giao hàng
        /// </summary>
        /// <param name="data">Dữ liệu người giao hàng cần cập nhật</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"UPDATE Shippers
                           SET ShipperName = @ShipperName,
                               Phone = @Phone
                           WHERE ShipperID = @ShipperID";

            int rows = await connection.ExecuteAsync(sql, data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa người giao hàng theo mã
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>true nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"DELETE FROM Shippers
                           WHERE ShipperID = @id";

            int rows = await connection.ExecuteAsync(sql, new { id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra người giao hàng có đang được sử dụng trong bảng Orders hay không
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>true nếu có dữ liệu liên quan</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT COUNT(*)
                           FROM Orders
                           WHERE ShipperID = @id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });

            return count > 0;
        }
    }
}