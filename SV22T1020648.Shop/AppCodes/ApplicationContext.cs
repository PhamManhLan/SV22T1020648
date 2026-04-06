using System.Text.Json; // Khuyên dùng thay thế cho Newtonsoft.Json

namespace SV22T1020648.Shop;

public static class ApplicationContext
{
    private static IHttpContextAccessor? _httpContextAccessor;
    private static IWebHostEnvironment? _hostEnvironment;

    public static int PageSize { get; set; } // Sửa lại chữ hoa 'S' cho đúng chuẩn property C#

    public static void Configure(IHttpContextAccessor context, IWebHostEnvironment environment)
    {
        _httpContextAccessor = context ?? throw new ArgumentNullException(nameof(context));
        _hostEnvironment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public static HttpContext? HttpContext => _httpContextAccessor?.HttpContext;

    // Đã sửa lỗi chính tả (thêm chữ 'n' vào Environment)
    public static IWebHostEnvironment? HostEnvironment => _hostEnvironment;

    public static string WebRootPath => _hostEnvironment?.WebRootPath ?? string.Empty;
    public static string ContentRootPath => _hostEnvironment?.ContentRootPath ?? string.Empty;

    public static void SetSessionData(string key, object value)
    {
        try
        {
            string sValue = JsonSerializer.Serialize(value);
            if (!string.IsNullOrEmpty(sValue))
            {
                HttpContext?.Session.SetString(key, sValue);
            }
        }
        catch { /* Bỏ qua lỗi parse nếu có */ }
    }

    public static T? GetSessionData<T>(string key) where T : class
    {
        try
        {
            string? sValue = HttpContext?.Session.GetString(key);
            if (!string.IsNullOrEmpty(sValue))
            {
                return JsonSerializer.Deserialize<T>(sValue);
            }
        }
        catch { /* Bỏ qua lỗi parse nếu có */ }

        return null;
    }
}