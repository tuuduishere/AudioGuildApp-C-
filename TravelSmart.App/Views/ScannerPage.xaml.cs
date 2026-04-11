using ZXing.Net.Maui;

namespace TravelSmart.App.Views;

public partial class ScannerPage : ContentPage
{
    // Tạo một sự kiện để "hét" lên cho MainPage biết là tao quét được chữ gì rồi
    public event EventHandler<string> OnQRCodeScanned;

    public ScannerPage()
    {
        InitializeComponent();

        // Cấu hình chỉ quét mã QR thôi, bỏ qua mấy cái mã vạch siêu thị cho nó nhẹ máy
        CameraReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional, // Chỉ 2D (QR Code)
            AutoRotate = true,
            Multiple = false
        };
    }

    // HÀM NÀY CHẠY KHI MÁY ẢNH CHỚP ĐƯỢC MÃ
    private void CameraReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        // Lấy cái kết quả đầu tiên quét được
        var result = e.Results?.FirstOrDefault();
        if (result != null)
        {
            // Phải đẩy lên luồng chính (MainThread) vì mình chuẩn bị tắt giao diện
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // 1. Tắt máy quét ngay lập tức để không bị spam quét liên tục
                CameraReader.IsDetecting = false;

                // 2. Rung điện thoại 1 cái báo hiệu quét thành công (Tùy chọn)
                try { HapticFeedback.Perform(HapticFeedbackType.LongPress); } catch { }

                // 3. Hét lên cho thằng MainPage biết (Truyền chữ quét được về)
                OnQRCodeScanned?.Invoke(this, result.Value);

                // 4. Tắt trang Camera, quay về màn hình Bản đồ
                await Navigation.PopModalAsync();
            });
        }
    }

    // Bấm nút X thì tắt trang
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        CameraReader.IsDetecting = false;
        await Navigation.PopModalAsync();
    }
}