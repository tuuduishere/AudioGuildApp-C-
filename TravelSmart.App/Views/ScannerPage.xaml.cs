using System.Text.Json;
using ZXing.Net.Maui;

namespace TravelSmart.App.Views;

public partial class ScannerPage : ContentPage
{
    private Action<string> _onQrScannedCallback;
    private bool _isProcessing = false; // Biến khóa không cho quét liên tục lúc đang xử lý

    public ScannerPage(Action<string> onQrScannedCallback)
    {
        InitializeComponent();
        _onQrScannedCallback = onQrScannedCallback;

        // Cấu hình chỉ quét mã QR cho tốc độ chớp nhoáng
        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }

    // 1. KHI QUÉT BẰNG CAMERA SẼ CHẠY VÀO ĐÂY
    private void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessing) return;
        var firstResult = e.Results?.FirstOrDefault();
        if (firstResult != null)
        {
            _isProcessing = true;
            BarcodeReader.IsDetecting = false; // Bắt được mã là tắt ngay Camera

            Dispatcher.Dispatch(async () =>
            {
                await Navigation.PopModalAsync(); // Đóng màn hình Camera
                _onQrScannedCallback?.Invoke(firstResult.Value); // Bắn mã QR về cho Trang Chủ
            });
        }
    }

    // 2. KHI BẤM NÚT "TẢI ẢNH LÊN" SẼ CHẠY VÀO ĐÂY
    private async void OnUploadClicked(object sender, EventArgs e)
    {
        if (_isProcessing) return;

        try
        {
            // Mở thư viện ảnh của điện thoại
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Chọn ảnh QR Code",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                _isProcessing = true;
                BarcodeReader.IsDetecting = false; // Tạm tắt camera

                // Hiện hiệu ứng Loading
                LoadingOverlay.IsVisible = true;
                LoadingIndicator.IsVisible = true;

                // Gửi ảnh lên Cloud API để giải mã
                using var stream = await result.OpenReadAsync();
                using var client = new HttpClient();
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(stream), "file", result.FileName);

                var response = await client.PostAsync("https://api.qrserver.com/v1/read-qr-code/", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();

                    // Bóc tách kết quả JSON trả về
                    using var doc = JsonDocument.Parse(jsonString);
                    var data = doc.RootElement[0].GetProperty("symbol")[0].GetProperty("data").GetString();

                    if (!string.IsNullOrEmpty(data))
                    {
                        // Tìm thấy mã QR trong ảnh -> Đóng cửa sổ và bắn mã về MainPage
                        await Navigation.PopModalAsync();
                        _onQrScannedCallback?.Invoke(data);
                        return;
                    }
                }

                // Nếu ảnh không chứa mã QR
                await DisplayAlert("Thất bại", "Không tìm thấy mã QR nào trong bức ảnh này!", "Thử lại");

                // Trả lại trạng thái ban đầu để người dùng thử ảnh khác hoặc quét Camera tiếp
                LoadingOverlay.IsVisible = false;
                LoadingIndicator.IsVisible = false;
                _isProcessing = false;
                BarcodeReader.IsDetecting = true;
            }
        }
        catch
        {
            await DisplayAlert("Lỗi", "Có lỗi xảy ra khi đọc ảnh!", "OK");
            LoadingOverlay.IsVisible = false;
            LoadingIndicator.IsVisible = false;
            _isProcessing = false;
            BarcodeReader.IsDetecting = true;
        }
    }

    // 3. KHI BẤM NÚT "ĐÓNG"
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        BarcodeReader.IsDetecting = false;
        await Navigation.PopModalAsync();
    }
}