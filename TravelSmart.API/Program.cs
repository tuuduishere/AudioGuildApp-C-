using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TravelSmart.API.Models;
using TravelSmart.API.Hubs;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => { options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); });

builder.Services.AddDbContext<VinhKhanhTravelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".apk"] = "application/vnd.android.package-archive";
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/download", () => Results.Redirect("/web"));

app.MapGet("/api/Pois/nearest", async (double lat, double lng, VinhKhanhTravelDbContext db) => {
    var pois = await db.Pois.ToListAsync();
    var nearest = pois.OrderBy(p => Math.Sqrt(Math.Pow(p.Latitude - lat, 2) + Math.Pow(p.Longitude - lng, 2))).FirstOrDefault();
    if (nearest == null) return Results.NotFound();

    var trans = await db.PoiTranslations.FirstOrDefaultAsync(t => t.PoiId == nearest.PoiId && t.LanguageCode == "vi");
    string poiName = trans?.Name ?? "Quán Ẩm Thực Vĩnh Khánh";
    string ttsText = trans?.Description ?? $"Chào mừng bạn đến với {poiName}";

    return Results.Ok(new { name = poiName, imageUrl = nearest.ImageUrl, audioUrl = $"/audio/{nearest.PoiId}_vi.mp3", ttsContent = ttsText });
});

app.MapGet("/api/web/pois", async (VinhKhanhTravelDbContext db) => {
    var pois = await db.Pois.ToListAsync();
    var translations = await db.PoiTranslations.Where(t => t.LanguageCode == "vi").ToListAsync();

    var result = pois.Select(p => {
        var trans = translations.FirstOrDefault(t => t.PoiId == p.PoiId);
        string poiName = trans?.Name ?? "Quán Ẩm Thực";
        string ttsText = trans?.Description ?? $"Bạn đang xem thông tin của {poiName}.";

        return new
        {
            id = p.PoiId,
            lat = p.Latitude,
            lng = p.Longitude,
            name = poiName,
            audioUrl = p.AudioUrl ?? $"/audio/{p.PoiId}_vi.mp3",
            imageUrl = p.ImageUrl ?? "https://images.unsplash.com/photo-1514933651103-005eec06c04b?q=80&w=800&auto=format&fit=crop",
            ttsContent = ttsText
        };
    });
    return Results.Ok(result);
});

// 🔥 GIAO DIỆN WEBAPP SPA
app.MapGet("/web", () => {
    string html = @"
    <!DOCTYPE html>
    <html lang='vi'>
    <head>
        <meta charset='utf-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no'>
        <title>TravelSmart WebApp</title>
        <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css'>
        <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
        <script src='https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/7.0.5/signalr.min.js'></script>
        <style>
            body, html { margin: 0; padding: 0; height: 100%; font-family: sans-serif; overflow: hidden; background: #e0e5ec; }
            .mobile-container { width: 100%; height: 100%; position: relative; max-width: 480px; margin: 0 auto; background: #f4f6f8; box-shadow: 0 0 20px rgba(0,0,0,0.2); display: flex; flex-direction: column;}
            .view-section { flex: 1; overflow-y: auto; display: none; width: 100%; position: relative; }
            #view-map { display: flex; flex-direction: column; overflow: hidden; }
            #map { flex: 1; width: 100%; z-index: 1; }
            .top-bar { position: absolute; top: 20px; left: 15px; right: 15px; z-index: 1000; display: flex; gap: 10px; align-items: center; }
            .search-box { flex: 1; background: white; height: 45px; border-radius: 25px; display: flex; align-items: center; padding: 0 15px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
            .search-box input { border: none; outline: none; width: 100%; margin-left: 10px; font-size: 15px; }
            .bell-btn { width: 45px; height: 45px; background: white; border-radius: 50%; display: flex; justify-content: center; align-items: center; box-shadow: 0 2px 10px rgba(0,0,0,0.1); color: #00838F; font-size: 20px; cursor: pointer; }
            .header-title { padding: 20px; font-size: 24px; font-weight: bold; color: #00838F; background: white; display: flex; align-items: center; gap: 10px; border-bottom: 1px solid #eee; }
            .bottom-nav { height: 60px; background: white; display: flex; justify-content: space-around; align-items: center; border-top: 1px solid #ddd; padding-bottom: max(env(safe-area-inset-bottom), 5px); z-index: 1000; }
            .nav-item { display: flex; flex-direction: column; align-items: center; color: #888; font-size: 10px; width: 20%; cursor: pointer; }
            .nav-item i { font-size: 20px; margin-bottom: 3px; transition: 0.2s; }
            .nav-item.active { color: #00838F; }
            .nav-item.active i { transform: scale(1.2); }
            .fab-scan { position: absolute; bottom: 30px; left: 50%; transform: translateX(-50%); width: 65px; height: 65px; background: #00838F; border-radius: 50%; display: flex; justify-content: center; align-items: center; border: 5px solid white; box-shadow: 0 -2px 10px rgba(0,0,0,0.1); z-index: 1001; cursor: pointer; color: white; font-size: 28px; }
            .sheet-overlay { position: absolute; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 1999; opacity: 0; pointer-events: none; transition: opacity 0.3s; }
            .sheet-overlay.show { opacity: 1; pointer-events: auto; }
            .bottom-sheet { position: absolute; bottom: -100%; left: 0; width: 100%; background: white; border-radius: 20px 20px 0 0; box-shadow: 0 -5px 15px rgba(0,0,0,0.2); z-index: 2000; transition: bottom 0.3s cubic-bezier(0.1, 0.8, 0.2, 1); padding: 20px; box-sizing: border-box; max-height: 85%; overflow-y: auto;}
            .bottom-sheet.show { bottom: 0; }
            .sheet-handle { width: 40px; height: 5px; background: #ddd; border-radius: 3px; margin: 0 auto 15px; }
            .sheet-img { width: 100%; height: 180px; object-fit: cover; border-radius: 10px; margin-bottom: 15px; background: #eee; }
            .sheet-title { font-size: 22px; font-weight: bold; margin: 0 0 5px; color: #006064; text-align: center; }
            .lang-container { display: flex; justify-content: center; gap: 10px; margin-bottom: 15px; }
            .btn-lang { padding: 6px 12px; border-radius: 15px; border: 1px solid #00838F; background: #E0F7FA; color: #00838F; font-weight: bold; cursor: pointer; font-size: 13px; }
            .btn-lang.active { background: #00838F; color: white; }
            .btn-dl { display: block; width: 100%; box-sizing: border-box; padding: 12px; background: #FFC107; border: none; border-radius: 10px; font-weight: bold; color: #000; margin-bottom: 10px; text-decoration: none; text-align: center; font-size: 14px; white-space: normal;}
            .btn-close { width: 100%; box-sizing: border-box; padding: 12px; background: #f1f1f1; border: none; border-radius: 10px; font-weight: bold; color: #555; cursor: pointer;}
            .card { background: white; margin: 15px; padding: 15px; border-radius: 15px; box-shadow: 0 2px 8px rgba(0,0,0,0.05); display: flex; gap: 15px; align-items: center; cursor: pointer;}
            .card-icon { width: 50px; height: 50px; background: #E0F7FA; color: #00838F; border-radius: 12px; display: flex; justify-content: center; align-items: center; font-size: 24px; }
            .noti-card { background: white; margin: 0 15px 15px 15px; padding: 15px; border-radius: 12px; box-shadow: 0 2px 10px rgba(0,0,0,0.05); display: flex; gap: 15px; align-items: center; border-left: 4px solid #FFC107;}
            /* 🔥 NÚT TẢI APP NỔI TRÊN BẢN ĐỒ */
            .floating-dl-btn { position: absolute; bottom: 80px; left: 50%; transform: translateX(-50%); background: #E53935; color: white; padding: 12px 25px; border-radius: 30px; font-weight: bold; text-decoration: none; z-index: 1000; box-shadow: 0 4px 15px rgba(229, 57, 53, 0.4); display: flex; align-items: center; gap: 8px; white-space: nowrap; font-size: 15px; border: 2px solid white; animation: pulse 2s infinite;}
            @keyframes pulse { 0% { transform: translateX(-50%) scale(1); } 50% { transform: translateX(-50%) scale(1.05); } 100% { transform: translateX(-50%) scale(1); } }
        </style>
    </head>
    <body>
        <div class='mobile-container'>
            <div id='view-map' class='view-section' style='display: flex;'>
                <div class='top-bar'>
                    <div class='search-box'>
                        <i class='fa-solid fa-magnifying-glass' style='color:#00838F;'></i>
                        <input type='text' id='searchInput' placeholder='Tìm quán ốc, lẩu...' onkeyup='searchPoi()'>
                    </div>
                    <div class='bell-btn' onclick='switchTab(""notifications"")'><i class='fa-solid fa-bell'></i></div>
                </div>
                <div id='map'></div>
                
                <!-- 🔥 NÚT TẢI APP LUÔN NỔI LÊN CHO KHÁCH BẤM -->
                <a href='/apk/travelsmart.apk' download class='floating-dl-btn'>
                    <i class='fa-brands fa-android fs-5'></i> TẢI APP NGAY
                </a>
            </div>

            <div id='view-tours' class='view-section'>
                <div class='header-title'><i class='fa-solid fa-route'></i> Tuyến đi nổi bật</div>
                <div id='tour-list'>
                    <p style='text-align:center; color:gray; margin-top:20px;'>Đang tải danh sách...</p>
                </div>
            </div>

            <div id='view-notifications' class='view-section'>
                <div class='header-title'><i class='fa-solid fa-bell'></i> Thông báo</div>
                <div style='padding-top: 15px;'>
                    <div class='noti-card'>
                        <div style='color: #FFC107; font-size: 28px;'><i class='fa-solid fa-bullhorn'></i></div>
                        <div>
                            <h3 style='margin:0 0 5px; font-size:16px; color:black;'>Hệ thống</h3>
                            <p style='margin:0; font-size:13px; color:gray;'>Chào mừng bạn đến với TravelSmart Vĩnh Khánh!</p>
                        </div>
                    </div>
                </div>
            </div>

            <div id='view-history' class='view-section'>
                <div class='header-title'><i class='fa-solid fa-clock-rotate-left'></i> Lịch sử tham quan</div>
                <div id='history-list'></div>
                <button onclick='clearHistory()' style='margin:20px auto; display:block; padding:10px 20px; background:#E53935; color:white; border:none; border-radius:8px; font-weight:bold; cursor:pointer;'>XÓA LỊCH SỬ</button>
            </div>

            <div id='view-profile' class='view-section'>
                <div class='header-title'><i class='fa-solid fa-user'></i> Cá nhân</div>
                <div class='card' style='margin-top: 20px;'>
                    <div class='card-icon' style='border-radius:50%;'><i class='fa-solid fa-user-astronaut'></i></div>
                    <div>
                        <h3 style='margin:0 0 5px; color:#00838F;'>Khách Vãng Lai</h3>
                        <p style='margin:0; font-size:12px; color:gray;'>Đang dùng phiên bản Web trải nghiệm</p>
                    </div>
                </div>
                
                <!-- 🔥 GÀI THÊM NÚT TẢI APP TRONG TAB CÁ NHÂN -->
                <a href='/apk/travelsmart.apk' download style='display:block; margin: 20px 15px; padding: 15px; background: #FFC107; color: black; text-align:center; text-decoration:none; border-radius: 12px; font-weight:bold; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
                    <i class='fa-brands fa-android fs-5 me-2'></i> CÀI ĐẶT APP TRAVELSMART
                </a>
            </div>

            <div id='sheetOverlay' class='sheet-overlay' onclick='closeSheet()'></div>

            <div id='poiSheet' class='bottom-sheet'>
                <div class='sheet-handle'></div>
                <img id='sheetImg' class='sheet-img' src=''/>
                <h2 id='sheetTitle' class='sheet-title'>Tên Quán</h2>
                
                <div class='lang-container'>
                    <button class='btn-lang active' id='btnLang-vi' onclick='changeLang(""vi"")'>🇻🇳 VN</button>
                    <button class='btn-lang' id='btnLang-en' onclick='changeLang(""en"")'>🇬🇧 EN</button>
                    <button class='btn-lang' id='btnLang-ja' onclick='changeLang(""ja"")'>🇯🇵 JP</button>
                </div>

                <p id='audioStatus' style='color:white; font-size:15px; font-weight:bold; margin:0 0 15px; display:none; text-align:center; padding: 12px; background: #FF9800; border-radius: 8px; cursor: pointer; box-shadow: 0 2px 5px rgba(0,0,0,0.2);'></p>
                <audio id='sheetAudio' style='display:none;' controls></audio>
                
                <a href='/apk/travelsmart.apk' download class='btn-dl'><i class='fa-brands fa-android'></i> TẢI APP ĐỂ XEM THỰC ĐƠN VÀ ĐÁNH GIÁ</a>
                <button class='btn-close' onclick='closeSheet()'>ĐÓNG LẠI</button>
            </div>

            <div class='bottom-nav'>
                <div class='nav-item active' onclick='switchTab(""map"", this)'><i class='fa-solid fa-location-dot'></i>Bản đồ</div>
                <div class='nav-item' onclick='switchTab(""tours"", this)'><i class='fa-solid fa-route'></i>Tuyến đi</div>
                <div class='nav-item' style='opacity:0; cursor:default;'>.</div>
                <div class='nav-item' onclick='switchTab(""history"", this)'><i class='fa-solid fa-clock-rotate-left'></i>Lịch sử</div>
                <div class='nav-item' onclick='switchTab(""profile"", this)'><i class='fa-solid fa-user'></i>Tôi</div>
            </div>
        </div>

        <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
        <script>
            let currentActivePoi = null;
            let allMapMarkers = []; 

            function switchTab(tabId, element) {
                document.querySelectorAll('.view-section').forEach(el => el.style.display = 'none');
                document.getElementById('view-' + tabId).style.display = (tabId === 'map') ? 'flex' : 'block';
                document.querySelectorAll('.nav-item').forEach(el => el.classList.remove('active'));
                if (element) element.classList.add('active');
                if(tabId === 'map') map.invalidateSize();
                if(tabId === 'history') loadHistoryView();
                if(tabId === 'tours') loadToursView();
            }

            var map = L.map('map', { zoomControl: false }).setView([10.7605, 106.7025], 15);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);
            var redIcon = new L.Icon({
                iconUrl: 'https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-red.png',
                shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/0.7.7/images/marker-shadow.png',
                iconSize: [25, 41], iconAnchor: [12, 41], popupAnchor: [1, -34], shadowSize: [41, 41]
            });

            navigator.geolocation.getCurrentPosition(pos => {
                map.setView([pos.coords.latitude, pos.coords.longitude], 16);
                L.circleMarker([pos.coords.latitude, pos.coords.longitude], { color: 'white', fillColor: '#00838F', fillOpacity: 1, radius: 8, weight: 3 }).addTo(map);
            });

            fetch('/api/web/pois', { headers: { 'ngrok-skip-browser-warning': 'true' } })
                .then(res => res.json())
                .then(data => {
                    data.forEach(poi => {
                        var marker = L.marker([poi.lat, poi.lng], {icon: redIcon}).addTo(map);
                        allMapMarkers.push({ poiData: poi, mapMarker: marker });

                        marker.on('click', function() {
                            if (currentActivePoi && connection.state === 'Connected') {
                                connection.invoke('LeavePoi', currentActivePoi.id.toLowerCase()).catch(err => console.error(err));
                            }
                            currentActivePoi = poi;
                            if (connection.state === 'Connected') {
                                connection.invoke('JoinPoi', poi.id.toLowerCase()).catch(err => console.error(err));
                            }

                            document.getElementById('sheetImg').src = poi.imageUrl;
                            document.getElementById('sheetTitle').innerText = poi.name;
                            
                            document.querySelectorAll('.btn-lang').forEach(el => el.classList.remove('active'));
                            document.getElementById('btnLang-vi').classList.add('active');

                            document.getElementById('sheetOverlay').classList.add('show');
                            document.getElementById('poiSheet').classList.add('show');
                            map.setView([poi.lat, poi.lng], 17);
                            
                            playAudioWithFallback(poi, 'vi', false);
                            
                            saveAndLogHistory(poi);
                            if (connection && connection.state === 'Connected') {
                                connection.invoke('LogListen', poi.id.toLowerCase()).catch(err => console.error(err));
                            }
                        });
                    });
                });

            function searchPoi() {
                let keyword = document.getElementById('searchInput').value.toLowerCase();
                allMapMarkers.forEach(item => {
                    if (item.poiData.name.toLowerCase().includes(keyword)) {
                        if (!map.hasLayer(item.mapMarker)) map.addLayer(item.mapMarker);
                    } else {
                        if (map.hasLayer(item.mapMarker)) map.removeLayer(item.mapMarker);
                    }
                });
            }

            function changeLang(lang) {
                if(!currentActivePoi) return;
                document.querySelectorAll('.btn-lang').forEach(el => el.classList.remove('active'));
                document.getElementById('btnLang-' + lang).classList.add('active');
                playAudioWithFallback(currentActivePoi, lang, true); 
            }

            let currentUtterance = null;

            function playAudioWithFallback(poi, lang, isUserClick = false) {
                window.speechSynthesis.cancel();

                let audioEl = document.getElementById('sheetAudio');
                let statusEl = document.getElementById('audioStatus');
                
                let textToRead = poi.ttsContent ? poi.ttsContent : ('Đang xem thông tin quán ' + poi.name);

                audioEl.pause();
                audioEl.style.display = 'none';
                statusEl.style.display = 'block';

                if (isUserClick) {
                    executeLogic();
                } else {
                    statusEl.style.background = '#FF9800';
                    statusEl.innerHTML = '<i class=""fa-solid fa-volume-high""></i> Bấm vào đây để nghe âm thanh';
                    statusEl.onclick = function() { executeLogic(); };
                    
                    setTimeout(() => { statusEl.click(); }, 100);
                }

                function executeLogic() {
                    statusEl.innerHTML = '<i class=""fa-solid fa-spinner fa-spin""></i> Đang tải...';
                    statusEl.onclick = null;

                    let unlock = new SpeechSynthesisUtterance('');
                    unlock.volume = 0;
                    window.speechSynthesis.speak(unlock);

                    if (lang === 'vi') {
                        if (poi.audioUrl) { audioEl.src = poi.audioUrl; } 
                        else { audioEl.src = '/audio/' + poi.id + '_vi.mp3'; }

                        let playPromise = audioEl.play();
                        if (playPromise !== undefined) {
                            playPromise.then(() => {
                                statusEl.style.background = '#4CAF50';
                                statusEl.innerHTML = '<i class=""fa-solid fa-circle-pause""></i> Đang phát (Bấm để dừng)';
                                statusEl.onclick = function() { 
                                    audioEl.pause(); 
                                    statusEl.style.background = '#FF9800';
                                    statusEl.innerHTML = '<i class=""fa-solid fa-volume-high""></i> Đã dừng. Bấm nghe lại'; 
                                    statusEl.onclick = function() { playAudioWithFallback(poi, lang, true); };
                                };
                            }).catch(error => {
                                executeTTS();
                            });
                        } else {
                            executeTTS();
                        }
                    } else {
                        executeTTS();
                    }
                }

                function executeTTS() {
                    statusEl.style.background = '#2196F3';
                    statusEl.innerHTML = '<i class=""fa-solid fa-robot""></i> Đang phát bằng AI...';

                    if (lang === 'vi') {
                        speakTTS(textToRead, 'vi-VN');
                    } else {
                        let url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl=${lang}&dt=t&q=${encodeURIComponent(textToRead)}`;
                        fetch(url)
                            .then(res => res.json())
                            .then(data => {
                                let translatedText = '';
                                data[0].forEach(item => { translatedText += item[0]; });
                                speakTTS(translatedText, lang === 'en' ? 'en-US' : 'ja-JP');
                            })
                            .catch(err => {
                                speakTTS(lang === 'en' ? 'Translation error' : '翻訳エラー', lang === 'en' ? 'en-US' : 'ja-JP');
                            });
                    }
                }

                function speakTTS(text, locale) {
                    window.speechSynthesis.cancel();
                    currentUtterance = new SpeechSynthesisUtterance(text);
                    currentUtterance.lang = locale;
                    currentUtterance.rate = 1.0;
                    
                    currentUtterance.onend = function() { 
                        statusEl.style.background = '#FF9800';
                        statusEl.innerHTML = '<i class=""fa-solid fa-rotate-right""></i> Đã đọc xong. Nghe lại'; 
                        statusEl.onclick = function() { playAudioWithFallback(poi, lang, true); };
                    };
                    
                    window.speechSynthesis.speak(currentUtterance);
                    
                    statusEl.innerHTML = '<i class=""fa-solid fa-robot""></i> Đang phát AI (Bấm để dừng)';
                    statusEl.onclick = function() { 
                        window.speechSynthesis.cancel(); 
                        statusEl.style.background = '#FF9800';
                        statusEl.innerHTML = '<i class=""fa-solid fa-volume-high""></i> Đã dừng. Bấm nghe lại';
                        statusEl.onclick = function() { playAudioWithFallback(poi, lang, true); };
                    };
                }
            }

            function closeSheet() {
                document.getElementById('sheetOverlay').classList.remove('show');
                document.getElementById('poiSheet').classList.remove('show');
                let audioEl = document.getElementById('sheetAudio');
                audioEl.pause();
                window.speechSynthesis.cancel();
                
                if (currentActivePoi && connection.state === 'Connected') {
                    connection.invoke('LeavePoi', currentActivePoi.id.toLowerCase()).catch(err => console.error(err));
                    currentActivePoi = null;
                }
            }

            function saveAndLogHistory(poi) {
                let history = JSON.parse(localStorage.getItem('AppHistory') || '[]');
                let timeStr = new Date().toLocaleTimeString('vi-VN') + ' ' + new Date().toLocaleDateString('vi-VN');
                history.unshift({ PoiName: poi.name, Time: timeStr });
                localStorage.setItem('AppHistory', JSON.stringify(history));

                fetch('/api/Pois/history', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'ngrok-skip-browser-warning': 'true' },
                    body: JSON.stringify({ PoiId: poi.id, DeviceName: 'Khách WebApp (4G)', LanguageCode: 'vi', DurationMinutes: 2.0 })
                }).catch(err => console.log('Lỗi gửi Log'));
            }

            function loadHistoryView() {
                let history = JSON.parse(localStorage.getItem('AppHistory') || '[]');
                let html = '';
                if(history.length === 0) {
                    html = '<p style=""text-align:center; color:gray; margin-top:50px;"">Chưa có lịch sử tham quan.</p>';
                } else {
                    history.forEach(item => {
                        html += `<div class='card'><div class='card-icon'><i class='fa-solid fa-location-dot'></i></div><div><h3 style='margin:0 0 5px; font-size:16px;'>${item.PoiName}</h3><p style='margin:0; font-size:12px; color:orange;'><i class='fa-regular fa-clock'></i> ${item.Time}</p></div></div>`;
                    });
                }
                document.getElementById('history-list').innerHTML = html;
            }

            function clearHistory() {
                if(confirm('Bạn muốn xóa lịch sử cục bộ?')) {
                    localStorage.removeItem('AppHistory');
                    loadHistoryView();
                }
            }

            function loadToursView() {
                fetch('/api/Tours', { headers: { 'ngrok-skip-browser-warning': 'true' } })
                    .then(res => res.json())
                    .then(data => {
                        let html = '';
                        data.forEach(tour => {
                            html += `<div class='card' onclick='alert(""Vui lòng tải App để xem chi tiết đường đi của Tour này!"")'><div><h3 style='margin:0 0 5px; font-size:18px; color:#006064;'>${tour.name || 'Tuyến đi trải nghiệm'}</h3><p style='margin:0; font-size:13px; color:gray;'>Bấm để tải app và bắt đầu hành trình.</p></div><div class='card-icon' style='margin-left:auto; width:40px; height:40px; background:#FFC107; color:black;'><i class='fa-solid fa-arrow-right'></i></div></div>`;
                        });
                        document.getElementById('tour-list').innerHTML = html || '<p style=""text-align:center;"">Chưa có tuyến đi nào.</p>';
                    });
            }

            const connection = new signalR.HubConnectionBuilder().withUrl('/travelhub?clientType=app').withAutomaticReconnect().build();
            connection.start().catch(err => console.error('SignalR Error:', err));
        </script>
    </body>
    </html>";

    return Results.Content(html, "text/html");
});

app.MapControllers();
app.MapHub<TravelHub>("/travelhub");
app.Run();