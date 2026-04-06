using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.DataLayers.SQLServer;

namespace SV22T1020648.BusinessLayers
{
    /// <summary>
    /// Lưu giữ các thông tin cấu hình sử dụng cho Business Layer 
    /// </summary>
    public static class Configuration
    {
        private static string _connectionString= "";

        /// <summary>
        /// Khởi tạo cấu hình cho Business Layer, thường là chuỗi kết nối đến CSDL
        /// (hàm được gọi trước khi chạy ứng dụng)
        /// </summary>
        /// <param name="connectionString"></param>
        /// 
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
            DashboardRepository = new DashboardRepository(_connectionString);
        }

        /// <summary>
        /// Lấy chuỗi tham số kết nối đến cơ sở dữ liệu, được sử dụng trong các lớp DAL để kết nối đến CSDL
        /// </summary>
        public static string ConnectionString => _connectionString;
        /// <summary>
        /// Repository xử lý dữ liệu Dashboard
        /// </summary>
        public static IDashboardRepository DashboardRepository { get; private set; } = null!;

    }
}
