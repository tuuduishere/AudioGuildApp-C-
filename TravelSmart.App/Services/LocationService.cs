using Microsoft.Maui.Devices.Sensors;

namespace TravelSmart.App.Services;

public class LocationService
{
    // Ống dẫn tín hiệu: Mỗi khi có tọa độ mới, nó sẽ bắn ra đây
    public event EventHandler<Location> OnLocationUpdated;

    public async Task StartTrackingAsync()
    {
        // 1. Kiểm tra và xin quyền người dùng
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        // Nếu họ cấm thì chịu, nghỉ chơi
        if (status != PermissionStatus.Granted) return;

        // 2. Lắp bộ lắng nghe sự kiện
        Geolocation.LocationChanged += Geolocation_LocationChanged;

        // 3. Cấu hình Radar: Độ chính xác cao, quét mỗi 5 giây
        var request = new GeolocationListeningRequest
        {
            DesiredAccuracy = GeolocationAccuracy.High,
            MinimumTime = TimeSpan.FromSeconds(5)
        };

        // 4. Khởi động Radar!
        await Geolocation.StartListeningForegroundAsync(request);
    }

    public void StopTracking()
    {
        // Tắt radar cho đỡ tốn pin khi đóng app
        Geolocation.LocationChanged -= Geolocation_LocationChanged;
        Geolocation.StopListeningForeground();
    }

    private void Geolocation_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        // Bắn tọa độ mới nhặt được ra ngoài
        OnLocationUpdated?.Invoke(this, e.Location);
    }
}