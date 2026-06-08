# 🚀 HƯỚNG DẪN VẬN HÀNH & DEMO ĐỒ ÁN TRAVELSMART

Hệ thống TravelSmart sử dụng kiến trúc kết hợp giữa **C# .NET API**, **Blazor Web Admin**, **Web Mini App** và **.NET MAUI (Android)**. 
Để phục vụ việc demo thực tế qua mạng 4G, hệ thống sử dụng **Cloudflare Tunnel** để đưa Localhost ra Internet. Do tính chất thay đổi link động của bản Cloudflare miễn phí, vui lòng thực hiện đúng **5 bước** dưới đây mỗi khi khởi động lại máy.

---

## ⚙️ YÊU CẦU CHUẨN BỊ (PREREQUISITES)
- Visual Studio 2022 (kèm workload .NET MAUI và ASP.NET Web).
- Thiết bị Android thực tế có kết nối 4G (Để test quét mã QR và cài App).
- File `cloudflared-windows-amd64.exe` đã được tải về máy.

---

## 🛠️ QUY TRÌNH 5 BƯỚC KHỞI ĐỘNG HỆ THỐNG

### BƯỚC 1: MỞ ĐƯỜNG HẦM CLOUDFLARE (LẤY LINK SERVER)
1. Mở thư mục chứa file `cloudflared-windows-amd64.exe`.
2. Mở Command Prompt (CMD) tại thư mục đó.
3. Chạy lệnh sau để kết nối với cổng của API (Ví dụ đang dùng cổng `5088`):
   ```cmd
   cloudflared-windows-amd64.exe tunnel --url http://localhost:5088
Chờ hệ thống khởi tạo. Tìm dòng có chứa chữ https://...trycloudflare.com và Copy đường link này.

⚠️ QUAN TRỌNG: Giữ nguyên và KHÔNG ĐƯỢC TẮT cửa sổ CMD này trong suốt quá trình chấm đồ án. Nếu tắt, Server sẽ sập.

BƯỚC 2: CẬP NHẬT LINK CHO TOÀN HỆ THỐNG
Mở Visual Studio, cập nhật đường link vừa copy vào 2 vị trí Cốt lõi sau:

1. Cho App MAUI (File TravelSmart.App/AppConfig.cs):

Mở file và dán đè link Cloudflare mới vào biến ServerUrl.

C#
public static readonly string ServerUrl = "https://[DÁN_LINK_CLOUDFLARE_MỚI_VÀO_ĐÂY]";
2. Cho Web Admin (File TravelSmart.Web/Pages/Home.razor):

Kéo xuống dưới cùng tại khối @code, dán đè link vào biến serverUrl.

C#
private string serverUrl = "https://[DÁN_LINK_CLOUDFLARE_MỚI_VÀO_ĐÂY]";
BƯỚC 3: ĐÓNG GÓI ỨNG DỤNG (BUILD APK)
Để App cài trên điện thoại nhận diện được link Server mới nhất, ta cần xuất lại file APK.

Trong Visual Studio, chuyển cấu hình Build trên thanh công cụ sang Release.

Mở Developer PowerShell (hoặc Terminal) ở góc dưới Visual Studio.

Gõ lệnh di chuyển vào thư mục App MAUI:

PowerShell
cd TravelSmart.App
Chạy lệnh sau để Build ra file APK (.NET 10):
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk
PowerShell
	
Chờ khoảng 1-2 phút cho đến khi Terminal báo Build succeeded (chữ màu xanh lá).

BƯỚC 4: ĐƯA FILE APK LÊN SERVER (CHUẨN BỊ CHO KHÁCH TẢI)
Mở File Explorer, đi theo đường dẫn sau để lấy file vừa Build:
TravelSmart.App/bin/Release/net10.0-android/publish/

Tìm file có tên kết thúc bằng chữ -Signed.apk (Ví dụ: com.companyname.travelsmart.app-Signed.apk).

Đổi tên file đó thành: travelsmart.apk (Viết thường, không dấu cách).

Copy file vừa đổi tên và Dán đè vào thư mục Public của Project API theo đường dẫn sau:
TravelSmart.API/wwwroot/apk/travelsmart.apk

BƯỚC 5: CHẠY SERVER & KỊCH BẢN DEMO
Tại Visual Studio, nhấn F5 (Start Debugging) để chạy project C# Backend (API & Web Admin).

Đảm bảo điện thoại dùng để Demo đã XÓA phiên bản App TravelSmart cũ (để xóa Cache).

🎯 Kịch bản Demo chuẩn với Hội Đồng:

Mở màn hình máy tính hiển thị trang Dashboard Admin (Đã có sẵn mã QR của ngày hôm nay).

Giám khảo dùng điện thoại (tắt Wifi, bật 4G) quét mã QR trên màn hình.

Hệ thống lập tức chuyển hướng vào WebApp (Mini App trải nghiệm nhanh không cần cài đặt).

Test các tính năng trên WebApp: Trải nghiệm bản đồ, hiển thị quán ăn gần nhất, nghe Audio đa ngôn ngữ (VN, EN, JP).

Giám khảo sẽ thấy dữ liệu truy cập (Realtime Online & Heatmap) nhảy số trực tiếp trên màn hình Admin.

Chuyển sang Tab "Tôi" trên WebApp -> Nhấn TẢI ỨNG DỤNG NGAY (APK).

File APK tải về điện thoại -> Cài đặt -> Mở App lên.

Toàn bộ App Native sẽ tự động kết nối hoàn hảo với Database mà không cần bất kỳ thao tác cấu hình nào thêm!
