using System;
using System.Collections.Generic;
using System.Text;

namespace TravelSmart.App;

public static class AppConfig
{
    // 🔥 NGÀY BẢO VỆ: Mày dán link Cloudflare mới nhất vào giữa 2 dấu ngoặc kép này, rồi Build APK là xong!
    public static readonly string ServerUrl = "https://nights-inserted-designated-expenditure.trycloudflare.com";

    // Tự động sinh link API cho toàn bộ các trang khác xài ké
    public static readonly string ApiBaseUrl = $"{ServerUrl}/api";
}