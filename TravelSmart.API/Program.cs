using Microsoft.EntityFrameworkCore;
using TravelSmart.API.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. CẤU HÌNH DATABASE
// Nếu project cũ ông xài SQL Server thì đổi chữ UseInMemoryDatabase thành UseSqlServer(chuỗi_kết_nối) nha
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("TravelSmartDb"));

// 2. KHAI BÁO CÁC DỊCH VỤ
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 3. CẤU HÌNH GIAO DIỆN SWAGGER ĐỂ TEST API
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// Map đường dẫn tới cái PoiController anh em mình vừa tạo
app.MapControllers();
app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.Run();