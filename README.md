# TravelSmart

TravelSmart là một hệ thống du lịch trải nghiệm gồm các thành phần chính:

- Backend: C# .NET API (ASP.NET Core)
- Web Admin: Blazor Web (Dashboard quản trị và realtime)
- Web Mini App: Ứng dụng web nhẹ cho trải nghiệm nhanh (dùng khi quét QR)
- Mobile Native: .NET MAUI (Android) — ứng dụng cài đặt đầy đủ

Mô tả cách hoạt động

1. Backend (API) cung cấp dữ liệu, xác thực, và các endpoint realtime (SignalR) để Web Admin hiển thị thống kê online và heatmap.
2. Web Admin (Blazor) là giao diện dành cho quản trị viên: quản lý POI, tour, người dùng, và theo dõi truy cập realtime.
3. Web Mini App được truy cập khi người dùng quét mã QR trên Dashboard — cho phép trải nghiệm nhanh (bản web) trên điện thoại 4G mà không cần cài app.
4. Ứng dụng .NET MAUI (Android) cung cấp trải nghiệm đầy đủ: định vị, danh sách tour, audio đa ngôn ngữ, lịch sử truy cập. App kết nối tới Backend bằng biến `ServerUrl` cấu hình.

Triển khai & Demo (tóm tắt)

- Để demo qua 4G, dùng Cloudflare Tunnel để đưa `localhost` ra Internet. Mở tunnel trỏ vào cổng API và lấy đường dẫn `https://...trycloudflare.com`.
- Cập nhật đường dẫn Cloudflare vào hai chỗ chính trước khi chạy hoặc build:
  - `TravelSmart.App/AppConfig.cs` -> `ServerUrl`
  - `TravelSmart.Web/Pages/Home.razor` -> biến `serverUrl` trong khối `@code`
- Build file APK cho Android:
  - Chuyển cấu hình sang `Release`.
  - Chạy: `dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk` trong thư mục `TravelSmart.App`.
  - Lấy file `*-Signed.apk`, đổi tên thành `travelsmart.apk` và copy vào `TravelSmart.API/wwwroot/apk/` để người dùng tải về.
- Chạy Backend + Web Admin (F5 trên Visual Studio). Giám khảo quét QR (4G) -> mở Web Mini App -> có thể tải APK và cài app native.

Ghi chú kỹ thuật ngắn

- Dự án target .NET 10.
- Sử dụng SignalR cho realtime.
- Các cấu hình môi trường (Server URL, cổng) cần cập nhật khi Tunnel thay đổi link (Cloudflare miễn phí có link động).

Tác giả

Võ Lê Chí Dũng - tuuduishere

