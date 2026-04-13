using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TravelSmart.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Khởi tạo HttpClient để Blazor gọi sang API
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5088") }); // Đổi cổng nếu API của mày khác

await builder.Build().RunAsync();