using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SV22T1020648.DataLayers.Interfaces;
using SV22T1020648.Models.Catalog;
using SV22T1020648.Models.Common;

namespace SV22T1020648.DataLayers.SQLServer
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Product CRUD
        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            var result = new PagedResult<Product>()
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
                    CategoryID = input.CategoryID,
                    SupplierID = input.SupplierID,
                    MinPrice = input.MinPrice,
                    MaxPrice = input.MaxPrice,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                };

                string condition = @"(@SearchValue = '' OR ProductName LIKE @SearchValue)
                                    AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                                    AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                                    AND (@MinPrice = 0 OR Price >= @MinPrice)
                                    AND (@MaxPrice = 0 OR Price <= @MaxPrice)";

                string sql = $@"
                    SELECT COUNT(*) FROM Products WHERE {condition};
                    SELECT * FROM Products 
                    WHERE {condition}
                    ORDER BY ProductName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Product>()).ToList();
                }
            }
            return result;
        }

        public async Task<Product?> GetAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM Products WHERE ProductID = @ProductID";
                return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductID = productID });
            }
        }

        public async Task<int> AddAsync(Product data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"INSERT INTO Products(ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                               VALUES(@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                               SELECT CAST(SCOPE_IDENTITY() as int);";
                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        public async Task<bool> UpdateAsync(Product data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"UPDATE Products 
                               SET ProductName = @ProductName, ProductDescription = @ProductDescription, 
                                   SupplierID = @SupplierID, CategoryID = @CategoryID, Unit = @Unit, 
                                   Price = @Price, Photo = @Photo, IsSelling = @IsSelling
                               WHERE ProductID = @ProductID";
                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        public async Task<bool> DeleteAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    DELETE FROM ProductAttributes WHERE ProductID = @ProductID;
                    DELETE FROM ProductPhotos WHERE ProductID = @ProductID;
                    DELETE FROM Products WHERE ProductID = @ProductID;
                ";

                return await connection.ExecuteAsync(sql, new { ProductID = productID }) > 0;
            }
        }

        public async Task<bool> IsUsedAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"SELECT CASE 
                                    WHEN EXISTS(SELECT 1 FROM OrderDetails WHERE ProductID = @ProductID) THEN 1 
                                    ELSE 0 
                               END";
                return await connection.ExecuteScalarAsync<bool>(sql, new { ProductID = productID });
            }
        }
        #endregion

        #region Product Attributes
        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM ProductAttributes WHERE ProductID = @ProductID ORDER BY DisplayOrder";
                return (await connection.QueryAsync<ProductAttribute>(sql, new { ProductID = productID })).ToList();
            }
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM ProductAttributes WHERE AttributeID = @AttributeID";
                return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { AttributeID = attributeID });
            }
        }

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"INSERT INTO ProductAttributes(ProductID, AttributeName, AttributeValue, DisplayOrder)
                               VALUES(@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                               SELECT CAST(SCOPE_IDENTITY() as bigint);";
                return await connection.ExecuteScalarAsync<long>(sql, data);
            }
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"UPDATE ProductAttributes 
                               SET AttributeName = @AttributeName, AttributeValue = @AttributeValue, DisplayOrder = @DisplayOrder
                               WHERE AttributeID = @AttributeID";
                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM ProductAttributes WHERE AttributeID = @AttributeID";
                return await connection.ExecuteAsync(sql, new { AttributeID = attributeID }) > 0;
            }
        }
        #endregion

        #region Product Photos
        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM ProductPhotos WHERE ProductID = @ProductID ORDER BY DisplayOrder";
                return (await connection.QueryAsync<ProductPhoto>(sql, new { ProductID = productID })).ToList();
            }
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "SELECT * FROM ProductPhotos WHERE PhotoID = @PhotoID";
                return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { PhotoID = photoID });
            }
        }

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"INSERT INTO ProductPhotos(ProductID, Photo, Description, DisplayOrder, IsHidden)
                               VALUES(@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                               SELECT CAST(SCOPE_IDENTITY() as bigint);";
                return await connection.ExecuteScalarAsync<long>(sql, data);
            }
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = @"UPDATE ProductPhotos 
                               SET Photo = @Photo, Description = @Description, DisplayOrder = @DisplayOrder, IsHidden = @IsHidden
                               WHERE PhotoID = @PhotoID";
                return await connection.ExecuteAsync(sql, data) > 0;
            }
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string sql = "DELETE FROM ProductPhotos WHERE PhotoID = @PhotoID";
                return await connection.ExecuteAsync(sql, new { PhotoID = photoID }) > 0;
            }
        }
        #endregion
    }
}