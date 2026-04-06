using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using SV22T1020648.BusinessLayers;
using SV22T1020648.Shop;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình các dịch vụ (Services)
builder.Services.AddControllersWithViews();

// Đăng ký HttpContextAccessor để có thể truy cập Session/User trong các lớp static
builder.Services.AddHttpContextAccessor();

// Cấu hình xác thực bằng Cookie (Dùng cho Login/Logout)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "SV22T1020648.Shop.Auth"; // Tên cookie
        options.LoginPath = "/Account/Login";           // Đường dẫn trang đăng nhập
        options.AccessDeniedPath = "/Account/Login"; // Trang báo lỗi quyền truy cập
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);  // Thời gian hết hạn phiên
    });

// Cấu hình Session (Dùng cho Giỏ hàng và lưu trữ tạm)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 2. Khởi tạo cấu hình cho Business Layer
// Lấy chuỗi kết nối từ file appsettings.json
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB") ?? "";
Configuration.Initialize(connectionString);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "temp-keys")))
    .SetApplicationName("SV22T1020648.Shop");

var app = builder.Build();

// 3. Khởi tạo ApplicationContext (Hỗ trợ truy cập HttpContext/Environment mọi nơi)
var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
ApplicationContext.Configure(httpContextAccessor, app.Environment);

// 4. Cấu hình HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

// Thứ tự Middleware cực kỳ quan trọng: Session -> Authentication -> Authorization
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();