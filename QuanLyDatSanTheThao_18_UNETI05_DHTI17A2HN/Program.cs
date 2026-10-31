using Microsoft.EntityFrameworkCore;
using QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Data;
var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. ĐĂNG KÝ CÁC DỊCH VỤ (SERVICES)
// =========================================================

// Cấu hình kết nối Cơ sở dữ liệu SQL Server (Lấy chuỗi kết nối từ appsettings.json)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Bật bộ nhớ đệm để lưu trữ Session
builder.Services.AddDistributedMemoryCache();

// Cấu hình Session (Phục vụ cho chức năng Đăng nhập và Phân quyền)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session sẽ hết hạn sau 30 phút không hoạt động
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Khai báo sử dụng mô hình MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// =========================================================
// 2. CẤU HÌNH PIPELINE (MIDDLEWARE)
// =========================================================

// Hỗ trợ Forwarded Headers khi chạy qua Dev Tunnels hoặc Reverse Proxy
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost
});

// Xử lý lỗi khi ứng dụng không chạy ở môi trường Development
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

// LƯU Ý QUAN TRỌNG: app.UseSession() BẮT BUỘC phải nằm sau app.UseRouting() và trước app.UseAuthorization()
app.UseSession();
app.UseAuthorization();

// Phục vụ các file tĩnh (css, js, images)
app.MapStaticAssets();

// Cấu hình định tuyến (Route) mặc định khi chạy trang web
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
