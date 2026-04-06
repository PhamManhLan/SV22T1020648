using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.Models.Security;

namespace SV22T1020648.DataLayers.SQLServer
{
    /// <summary>
    /// Repository xử lý các chức năng liên quan đến tài khoản nhân viên.
    /// Thực hiện xác thực đăng nhập và thay đổi mật khẩu từ bảng Employees.
    /// </summary>
    public class EmployeeAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối cơ sở dữ liệu.
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến SQL Server</param>
        public EmployeeAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Xác thực thông tin đăng nhập của nhân viên.
        /// </summary>
        /// <param name="userName">Email đăng nhập</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>
        /// Thông tin tài khoản nếu đăng nhập hợp lệ, ngược lại trả về null.
        /// </returns>
        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Thay N'employee' bằng tên cột RoleNames thực tế trong bảng
                string sql = @"SELECT CAST(EmployeeID as nvarchar) AS UserId,
                              Email AS UserName,
                              FullName AS DisplayName,
                              Email,
                              Photo,
                              RoleNames
                       FROM Employees
                       WHERE Email = @Email AND Password = @Password AND IsWorking = 1";

                var parameters = new { Email = userName, Password = password };
                return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, parameters);
            }
        }

        /// <summary>
        /// Thay đổi mật khẩu của nhân viên.
        /// </summary>
        /// <param name="userName">Email của nhân viên</param>
        /// <param name="password">Mật khẩu mới</param>
        /// <returns>true nếu cập nhật thành công</returns>
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Cập nhật mật khẩu mới cho nhân viên
                string sql = "UPDATE Employees SET Password = @Password WHERE Email = @Email";

                var rowsAffected = await connection.ExecuteAsync(sql, new { Email = userName, Password = password });
                return rowsAffected > 0;
            }
        }
    }
}