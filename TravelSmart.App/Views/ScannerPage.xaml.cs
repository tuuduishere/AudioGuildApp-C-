using TravelSmart.App.Services;
using TravelSmart.App.Models;
using ZXing.Net.Maui;

namespace TravelSmart.App.Views;

public partial class ScannerPage : ContentPage
{
    private readonly DataService _dataService;
    private bool _isScanning = true; // Biến chống quét 2 lần liên tục

    public ScannerPage()
    {
        InitializeComponent();
        _dataService = new DataService();

        // Cấu hình chỉ quét mã QR
        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }

    private async void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (!_isScanning || e.Results == null || !e.Results.Any()) return;

        // Tắt ngay camera để không bị quét dính lại
        _isScanning = false;
        BarcodeReader.IsDetecting = false;

        string qrCodeKey = e.Results.First().Value; // Đây chính là chữ OCOANH_01

        Dispatcher.Dispatch(async () =>
        {
            // 1. Kéo toàn bộ data từ Server về (hoặc quét trong SQLite)
            await _dataService.SyncFromServerAsync();
            var pois = await _dataService.GetPOIsAsync();

            // 2. Tìm quán ăn có QrCodeKey khớp với mã vừa quét
            var matchedPoi = pois.FirstOrDefault(p => p.QrCodeKey == qrCodeKey);

            if (matchedPoi != null)
            {
                // Báo tìm thấy và chuẩn bị đọc tiếng
                await DisplayAlert("Thành công", $"Bạn đã đến: {matchedPoi.Name}", "Nghe Thuyết Minh");

                // Đọc luôn và ngay!
                await TextToSpeech.Default.SpeakAsync(matchedPoi.TtsContent);

                // Ghi vào lịch sử tham quan
                // await _dataService.AddHistoryAsync(matchedPoi);
            }
            else
            {
                await DisplayAlert("Lỗi", "Không tìm thấy dữ liệu quán ăn này trên hệ thống!", "Thử lại");
                _isScanning = true;
                BarcodeReader.IsDetecting = true;
                return;
            }

            // Quét xong, nghe xong thì đóng màn hình Camera lại
            await Navigation.PopModalAsync();
        });
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        _isScanning = false;
        BarcodeReader.IsDetecting = false;
        await Navigation.PopModalAsync(); // Trở về trang trước (Bản đồ)
    }
}