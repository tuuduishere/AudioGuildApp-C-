using Microsoft.EntityFrameworkCore;
using TravelSmart.API.Data;
using TravelSmart.API.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Cấu hình CORS cho Mobile và Admin gọi vào
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", b => b.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. Sử dụng Swagger để test API
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseStaticFiles(); // Để truy cập ảnh và nhạc trong wwwroot
app.UseAuthorization();
app.MapControllers();

// 4. Tự động nạp dữ liệu mẫu khi chạy
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!context.Places.Any())
    {
        context.Places.AddRange(
            new Place { Name = "Dinh Độc Lập", Latitude = 10.7770, Longitude = 106.6953, Description = "Di tích lịch sử.", AudioUrl = "/audios/dinhdoclap.mp3" },
            new Place { Name = "Nhà Thờ Đức Bà", Latitude = 10.7797, Longitude = 106.6990, Description = "Kiến trúc cổ.", AudioUrl = "/audios/nhathoducba.mp3" }
        );
        context.SaveChanges();
    }
}

app.Run();