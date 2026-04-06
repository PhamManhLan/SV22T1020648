using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.DataLayers.SQLServer;
using SV22T1020648.Models.Security;

namespace SV22T1020648.BusinessLayers
{
    /// <summary>
    /// Xử lý nghiệp vụ liên quan đến bảo mật (đăng nhập, đổi mật khẩu)
    /// </summary>
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository customerAccountDB;
        private static readonly IUserAccountRepository employeeAccountDB;

        /// <summary>
        /// Khởi tạo repository cho 2 loại tài khoản
        /// </summary>
        static SecurityDataService()
        {
            customerAccountDB = new CustomerAccountRepository(Configuration.ConnectionString);
            employeeAccountDB = new EmployeeAccountRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Xác thực đăng nhập (ưu tiên nhân viên trước, sau đó đến khách hàng)
        /// </summary>
        public static async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return null;

            // kiểm tra employee
            var user = await employeeAccountDB.AuthorizeAsync(userName, password);
            if (user != null)
                return user;

            // kiểm tra customer
            return await customerAccountDB.AuthorizeAsync(userName, password);
        }

        /// <summary>
        /// Đổi mật khẩu (không cần mật khẩu cũ)
        /// </summary>
        /// <param name="userName">Tên đăng nhập / Email</param>
        /// <param name="newPassword">Mật khẩu mới</param>
        /// <returns></returns>
        public static async Task<bool> ChangePasswordAsync(string userName, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(userName) ||
                string.IsNullOrWhiteSpace(newPassword))
                return false;

            // thử đổi cho employee trước
            bool result = await employeeAccountDB.ChangePasswordAsync(userName, newPassword);
            if (result)
                return true;

            // nếu không phải employee thì thử customer
            return await customerAccountDB.ChangePasswordAsync(userName, newPassword);
        }
    }
}