using SV22T1020648.Models.Dashboard;

namespace SV22T1020648.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các thao tác dữ liệu cho Dashboard
    /// </summary>
    public interface IDashboardRepository
    {
        /// <summary>
        /// Lấy thông tin tổng hợp cho Dashboard
        /// </summary>
        /// <returns>Thông tin Dashboard</returns>
        Task<DashboardInfo> GetDashboardInfoAsync();
    }
}