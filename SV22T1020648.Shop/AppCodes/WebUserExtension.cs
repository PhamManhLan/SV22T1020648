using System.Security.Claims;

namespace SV22T1020648.Shop;

public static class WebUserExtension
{
    /// <summary>
    /// Đọc thông tin của người dùng từ Principal (Cookie)
    /// </summary>
    public static WebUserData? GetUserData(this ClaimsPrincipal? principal)
    {
        // Kiểm tra an toàn: Nếu không có hoặc chưa đăng nhập thì trả về null luôn
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        return new WebUserData
        {
            UserId = principal.FindFirstValue(nameof(WebUserData.UserId)) ?? string.Empty,
            UserName = principal.FindFirstValue(nameof(WebUserData.UserName)) ?? string.Empty,
            DisplayName = principal.FindFirstValue(nameof(WebUserData.DisplayName)) ?? string.Empty,
            Address = principal.FindFirstValue(nameof(WebUserData.Address)) ?? string.Empty,
            Province = principal.FindFirstValue(nameof(WebUserData.Province)) ?? string.Empty,
            Phone = principal.FindFirstValue(nameof(WebUserData.Phone)) ?? string.Empty
        };
    }
}