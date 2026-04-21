using System.Text.Json;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace TravelSmart.App.Views;

public partial class ScannerPage : ContentPage
{
    private Action<string> _onQrScannedCallback;
    private bool _isProcessing = false;

    public ScannerPage(Action<string> onQrScannedCallback)
    {
        InitializeComponent();
        _onQrScannedCallback = onQrScannedCallback;

        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };
    }

    private void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessing || e.Results == null || !e.Results.Any()) return;

        var firstResult = e.Results.First();
        if (firstResult != null)
        {
            _isProcessing = true;
            BarcodeReader.IsDetecting = false;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Navigation.PopModalAsync();
                _onQrScannedCallback?.Invoke(firstResult.Value);
            });
        }
    }

    private async void OnUploadClicked(object sender, EventArgs e)
    {
        if (_isProcessing) return;

        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Chọn ảnh QR Code",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                _isProcessing = true;
                BarcodeReader.IsDetecting = false;

                LoadingOverlay.IsVisible = true;
                LoadingIndicator.IsVisible = true;

                using var stream = await result.OpenReadAsync();
                using var client = new HttpClient();
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(stream), "file", result.FileName);

                var response = await client.PostAsync("https://api.qrserver.com/v1/read-qr-code/", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(jsonString);
                    var data = doc.RootElement[0].GetProperty("symbol")[0].GetProperty("data").GetString();

                    if (!string.IsNullOrEmpty(data))
                    {
                        await Navigation.PopModalAsync();
                        _onQrScannedCallback?.Invoke(data);
                        return;
                    }
                }

                await DisplayAlert("Thất bại", "Không tìm thấy mã QR nào trong bức ảnh này!", "Thử lại");

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

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        BarcodeReader.IsDetecting = false;
        await Navigation.PopModalAsync();
    }
}