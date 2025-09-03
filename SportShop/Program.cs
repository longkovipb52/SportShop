using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Services;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Thêm hỗ trợ Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Thêm HTTP Context Accessor để sử dụng Session trong View
builder.Services.AddHttpContextAccessor();

// Thêm PayPal Service
builder.Services.AddHttpClient<PayPalService>();
builder.Services.AddScoped<PayPalService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thêm Session middleware (phải đặt trước UseAuthorization)
app.UseSession();

app.UseAuthorization();

// Đảm bảo đăng ký controller routes TRƯỚC các route mặc định
app.MapControllers(); // Quan trọng! Đăng ký attribute routes

// Các route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
