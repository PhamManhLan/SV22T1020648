using SV22T1020648.Models.Dashboard;

namespace SV22T1020648.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu dùng chung trong hệ thống
    /// </summary>
    public static partial class CommonDataService
    {
        /// <summary>
        /// Lấy thông tin tổng hợp cho Dashboard
        /// </summary>
        /// <returns>Thông tin Dashboard</returns>
        public static async Task<DashboardInfo> GetDashboardInfoAsync()
        {
            return await Configuration.DashboardRepository.GetDashboardInfoAsync();
        }
    }
}