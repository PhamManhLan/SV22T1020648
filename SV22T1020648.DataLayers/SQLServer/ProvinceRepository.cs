using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.Models.DataDictionary;

namespace SV22T1020648.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt truy vấn dữ liệu Tỉnh thành từ SQL Server
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách tỉnh thành
        /// </summary>
        /// <returns></returns>
        public async Task<List<Province>> ListAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Truy vấn lấy danh sách tỉnh thành và sắp xếp theo tên
                string sql = "SELECT ProvinceName FROM Provinces ORDER BY ProvinceName";

                var list = await connection.QueryAsync<Province>(sql);
                return list.ToList();
            }
        }
    }
}