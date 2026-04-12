using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace TravelSmart.App.Platforms.Android;

// Khai báo đây là một dịch vụ chuyên dùng để lấy Vị trí (Location)
[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
public class AndroidLocationService : Service
{
    public override IBinder OnBind(Intent intent) => null;

    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        var channelId = "TravelSmartChannel";

        // Tạo kênh thông báo cho Android 8.0 trở lên
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(channelId, "Dẫn đường Vĩnh Khánh", NotificationImportance.Low);
            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }

        // Tạo cái thông báo dính chặt trên màn hình khóa của khách
        var notification = new NotificationCompat.Builder(this, channelId)
            .SetContentTitle("TravelSmart đang chạy ngầm")
            .SetContentText("Đang quét các quán ốc xung quanh bạn...")
            .SetSmallIcon(global::Android.Resource.Drawable.IcMenuCompass) // Lấy thẳng icon La Bàn của Android cho chắc cú!
            .SetOngoing(true) // Không cho khách quẹt tắt thông báo này
            .Build();

        // Kích hoạt kim bài miễn tử!
        StartForeground(1001, notification);

        return StartCommandResult.Sticky;
    }
}