namespace TravelSmart.App.Services;

public class TTSService
{
    // Cục token này dùng để "bóp cổ" (ngắt) giọng đọc nếu cần
    private CancellationTokenSource _cts;

    public async Task SpeakAsync(string text)
    {
        // 1. Nếu đang đọc bài cũ mà có bài mới -> Ngắt ngay bài cũ
        CancelSpeech();

        _cts = new CancellationTokenSource();

        try
        {
            // 2. Cấu hình giọng đọc
            var options = new SpeechOptions
            {
                Volume = 1.0f,  // Âm lượng max
                Pitch = 1.0f    // Độ thanh trầm tự nhiên
                // MAUI sẽ tự động lấy giọng đọc Tiếng Việt mặc định của hệ điều hành Android/iOS
            };

            // 3. Phát âm thanh (truyền token vào để có thể hủy giữa chừng)
            await TextToSpeech.Default.SpeakAsync(text, options, cancelToken: _cts.Token);
        }
        catch (TaskCanceledException)
        {
            // Bị ngắt lời hợp lệ, không sao cả
            System.Diagnostics.Debug.WriteLine("Đã ngắt lời AI.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi TTS: {ex.Message}");
        }
    }

    public void CancelSpeech()
    {
        // Hàm này gọi khi muốn nó nín ngay lập tức
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
    }
}