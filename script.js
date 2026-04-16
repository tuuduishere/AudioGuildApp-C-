/* ===== DIAGRAM DATA ===== */
window.DIAGRAM_DATA = {
    sections: {
        "gps": [
            { key: "geofence_flow", label: "Luồng Geofence" },
            { key: "gps_arch", label: "Kiến trúc GPS" }
        ],
        "arch": [
            { key: "system_arch", label: "Kiến trúc hệ thống" },
            { key: "data_flow", label: "Luồng dữ liệu" },
            { key: "offline_sync", label: "Offline Sync" }
        ]
    },
    diagrams: {
        geofence_flow: {
            title: "Luồng Geofence — Kích hoạt thuyết minh",
            mermaid: `flowchart TD
    A([📡 GPS cập nhật vị trí]) --> B[Tính khoảng cách đến tất cả POI]
    B --> C{POI nào < bán kính?}
    C -- Không --> A
    C -- Có --> D{Đã trong cooldown?}
    D -- Có --> A
    D -- Không --> E[Cập nhật trạng thái geofence]
    E --> F{Loại trigger?}
    F -- Enter Zone --> G[Phát ngay khi vào vùng]
    F -- Nearest --> H[Phát POI gần nhất]
    G --> I[Debounce 500ms]
    H --> I
    I --> J[Đẩy vào Audio Queue]
    J --> K{Có file audio sẵn?}
    K -- Tier 1: Có --> L[🔊 Phát file MP3 local]
    K -- Không --> M{Có mạng?}
    M -- Có --> N[Tier 2/3: Download / TTS API]
    M -- Không --> O[Tier 4: Device TTS offline]
    N --> L
    O --> L
    L --> P[Set cooldown timer]
    P --> A`
        },

        gps_arch: {
            title: "Kiến trúc GPS & Location Service",
            mermaid: `graph TB
    subgraph App["📱 MAUI App"]
      subgraph FG["Foreground"]
        UI[Map Screen]
        UIM[User Marker]
      end
      subgraph BG["Background Service"]
        GPS[GPS Listener<br/>High Accuracy]
        OPT[Optimizer<br/>Giảm tần suất khi đứng yên]
        GEO[Geofence Engine]
      end
      subgraph Data["Local Data"]
        SQL[(SQLite<br/>POI Cache)]
        AC[Audio Cache<br/>MP3 files]
      end
      subgraph Audio["Audio Engine"]
        Q[Audio Queue FIFO]
        TTS[TTS Engine]
        PL[Player]
      end
    end
    GPS --> OPT --> GEO
    GEO --> Q
    GEO --> UIM
    GPS --> UIM
    SQL --> GEO
    Q --> TTS --> PL
    AC --> PL`
        },

        system_arch: {
            title: "Kiến trúc tổng quan hệ thống TravelSmart",
            mermaid: `graph TB
    subgraph Client["Client Layer"]
      APP["📱 TravelSmart.App<br/>.NET MAUI<br/>iOS & Android"]
      ADMIN["🖥️ TravelSmart.Admin<br/>Blazor Web<br/>CMS & Analytics"]
    end
    subgraph Server["Server Layer"]
      API["⚡ TravelSmart.API<br/>ASP.NET Core<br/>REST API"]
      BG["🔄 Background Service<br/>Audio Processing<br/>TTS Generation"]
      DB[("🗄️ Database<br/>SQL Server")]
      FS["📁 File Storage<br/>Audio MP3<br/>Images"]
    end
    subgraph Mobile["Mobile Local"]
      SQL2[("💾 SQLite<br/>Offline Cache")]
      AC2["🎵 Audio Cache<br/>Local Files"]
    end
    APP <-->|"REST API<br/>Sync khi có mạng"| API
    ADMIN <-->|"REST API"| API
    API <--> DB
    API <--> FS
    API --> BG
    BG --> DB
    BG --> FS
    APP --- SQL2
    APP --- AC2
    SQL2 <-->|"Sync delta"| API`
        },

        data_flow: {
            title: "Luồng dữ liệu: Admin → API → App",
            mermaid: `sequenceDiagram
    participant A as 👤 Admin
    participant CMS as 🖥️ Blazor Admin
    participant API as ⚡ ASP.NET API
    participant BG as 🔄 Background
    participant APP as 📱 MAUI App
    participant SQL as 💾 SQLite

    A->>CMS: Tạo POI mới + upload audio
    CMS->>API: POST /api/poi
    API->>API: Lưu POI vào DB
    API->>BG: Queue xử lý audio
    API-->>CMS: 201 Created
    BG->>BG: Encode audio, tạo TTS scripts
    BG->>API: Cập nhật trạng thái audio

    Note over APP,SQL: App mở hoặc có mạng
    APP->>API: GET /api/poi/sync?since={last_sync}
    API-->>APP: Delta POI thay đổi
    APP->>SQL: Upsert vào SQLite
    APP->>APP: Download audio files mới

    Note over APP: GPS trigger Geofence
    APP->>SQL: Query POI trong bán kính
    SQL-->>APP: Danh sách POI
    APP->>APP: 🔊 Phát thuyết minh`
        },

        offline_sync: {
            title: "Chiến lược Offline-First & Sync",
            mermaid: `flowchart TD
    A([App khởi động]) --> B{Có mạng?}
    B -- Có --> C[Sync API: GET /poi/sync]
    B -- Không --> D[Dùng SQLite sẵn có]
    C --> E{Có delta?}
    E -- Có --> F[Upsert POI vào SQLite]
    E -- Không --> G[SQLite up to date]
    F --> H{Có audio mới?}
    H -- Có --> I[Download MP3 background]
    H -- Không --> G
    I --> G
    G --> J[App hoạt động bình thường]
    D --> J
    J --> K{GPS trigger POI}
    K --> L{Audio có local?}
    L -- Có --> M[🔊 Phát offline]
    L -- Không --> N{Có mạng?}
    N -- Có --> O[Stream từ server]
    N -- Không --> P[Device TTS fallback]
    M --> Q[Log analytics local]
    O --> Q
    P --> Q
    Q --> R{Có mạng sau?}
    R -- Có --> S[Sync analytics lên server]`
        }
    }
};

/* ===== LOAD MERMAID & INJECT DIAGRAM BUTTONS ===== */
(function () {
    const CDN = "https://cdnjs.cloudflare.com/ajax/libs/mermaid/10.6.1/mermaid.min.js";
    const s = document.createElement("script");
    s.src = CDN;
    s.onload = () => {
        mermaid.initialize({ startOnLoad: false, theme: "default", securityLevel: "loose" });
        injectDiagramButtons();
    };
    document.head.appendChild(s);
})();

function injectDiagramButtons() {
    const DATA = window.DIAGRAM_DATA;
    if (!DATA) return;
    Object.keys(DATA.sections).forEach(sectionId => {
        const section = document.getElementById(sectionId);
        if (!section) return;
        const anchor = document.getElementById(sectionId + "-diagrams") || section.querySelector(".section-header");
        if (!anchor) return;
        const btnsDiv = document.createElement("div");
        btnsDiv.className = "diagram-btns";
        DATA.sections[sectionId].forEach(d => {
            const btn = document.createElement("button");
            btn.className = "diagram-btn";
            btn.setAttribute("data-diagram", d.key);
            btn.innerHTML = "📊 " + d.label;
            btnsDiv.appendChild(btn);
        });
        anchor.after(btnsDiv);
    });
}

/* ===== ZOOM / PAN STATE ===== */
let scale = 1, tx = 0, ty = 0, renderN = 0;
let dragging = false, lastX = 0, lastY = 0;

const overlay = document.getElementById("dgOverlay");
const titleEl = document.getElementById("dgTitle");
const vp = document.getElementById("dgVp");
const dgBody = document.getElementById("dgBody");
const zpctEl = document.getElementById("zpct");

function applyTransform() {
    vp.style.transform = `translate(${tx}px,${ty}px) scale(${scale})`;
    zpctEl.textContent = Math.round(scale * 100) + "%";
}

function doZoom(delta, cx, cy) {
    const old = scale;
    scale = Math.max(0.1, Math.min(5, scale + delta));
    const r = scale / old;
    tx = cx - r * (cx - tx);
    ty = cy - r * (cy - ty);
    applyTransform();
}

function fitToView() {
    const svg = vp.querySelector("svg");
    if (!svg) { scale = 1; tx = 0; ty = 0; applyTransform(); return; }
    const bw = dgBody.clientWidth - 40;
    const bh = dgBody.clientHeight - 40;
    const vb = svg.viewBox?.baseVal;
    const sw = (vb?.width) || svg.getBoundingClientRect().width || bw;
    const sh = (vb?.height) || svg.getBoundingClientRect().height || bh;
    scale = Math.min(bw / sw, bh / sh, 2);
    tx = Math.max(0, (bw - sw * scale) / 2) + 20;
    ty = Math.max(0, (bh - sh * scale) / 2) + 20;
    applyTransform();
}

/* ===== OPEN / CLOSE DIAGRAM ===== */
async function openDiagram(key) {
    const d = window.DIAGRAM_DATA?.diagrams[key];
    if (!d) return;
    titleEl.textContent = d.title;
    scale = 1; tx = 0; ty = 0;
    vp.innerHTML = '<p class="loading-msg">Đang render sơ đồ...</p>';
    overlay.classList.add("active");
    document.body.style.overflow = "hidden";
    try {
        renderN++;
        const result = await mermaid.render("dgr" + renderN, d.mermaid);
        vp.innerHTML = result.svg;
        requestAnimationFrame(fitToView);
    } catch (err) {
        vp.innerHTML = `<p style="color:#c62828;padding:2rem">Render error: ${err.message}</p>`;
    }
}

function closeDiagram() {
    overlay.classList.remove("active");
    document.body.style.overflow = "";
}

/* ===== EVENT DELEGATION (CLICKS) ===== */
document.addEventListener("click", e => {
    const btn = e.target.closest("[data-diagram]");
    if (btn) { openDiagram(btn.getAttribute("data-diagram")); return; }
    if (e.target.closest("#zoomIn")) { doZoom(0.2, dgBody.clientWidth / 2, dgBody.clientHeight / 2); return; }
    if (e.target.closest("#zoomOut")) { doZoom(-0.2, dgBody.clientWidth / 2, dgBody.clientHeight / 2); return; }
    if (e.target.closest("#zoomReset")) { fitToView(); return; }
    if (e.target.closest("#dgClose") || e.target === overlay) { closeDiagram(); }
});

/* ===== MOUSE WHEEL ZOOM ===== */
dgBody.addEventListener("wheel", e => {
    e.preventDefault();
    const r = dgBody.getBoundingClientRect();
    doZoom(e.deltaY > 0 ? -0.12 : 0.12, e.clientX - r.left, e.clientY - r.top);
}, { passive: false });

/* ===== MOUSE DRAG PAN ===== */
dgBody.addEventListener("mousedown", e => {
    dragging = true; lastX = e.clientX; lastY = e.clientY; e.preventDefault();
});
window.addEventListener("mousemove", e => {
    if (!dragging) return;
    tx += e.clientX - lastX; ty += e.clientY - lastY;
    lastX = e.clientX; lastY = e.clientY;
    applyTransform();
});
window.addEventListener("mouseup", () => dragging = false);

/* ===== TOUCH SUPPORT (PAN + PINCH ZOOM) ===== */
let lastTouchDist = 0;
dgBody.addEventListener("touchstart", e => {
    if (e.touches.length === 1) {
        dragging = true;
        lastX = e.touches[0].clientX;
        lastY = e.touches[0].clientY;
    } else if (e.touches.length === 2) {
        lastTouchDist = Math.hypot(
            e.touches[0].clientX - e.touches[1].clientX,
            e.touches[0].clientY - e.touches[1].clientY
        );
    }
}, { passive: true });

dgBody.addEventListener("touchmove", e => {
    if (e.touches.length === 1 && dragging) {
        tx += e.touches[0].clientX - lastX;
        ty += e.touches[0].clientY - lastY;
        lastX = e.touches[0].clientX;
        lastY = e.touches[0].clientY;
        applyTransform();
    } else if (e.touches.length === 2) {
        const dist = Math.hypot(
            e.touches[0].clientX - e.touches[1].clientX,
            e.touches[0].clientY - e.touches[1].clientY
        );
        const r = dgBody.getBoundingClientRect();
        const cx = (e.touches[0].clientX + e.touches[1].clientX) / 2 - r.left;
        const cy = (e.touches[0].clientY + e.touches[1].clientY) / 2 - r.top;
        doZoom((dist - lastTouchDist) * 0.005, cx, cy);
        lastTouchDist = dist;
    }
}, { passive: true });
dgBody.addEventListener("touchend", () => dragging = false);

/* ===== ESC KEY ===== */
document.addEventListener("keydown", e => {
    if (e.key === "Escape" && overlay.classList.contains("active")) closeDiagram();
});

/* ===== NAV ACTIVE HIGHLIGHT ===== */
const navSections = document.querySelectorAll("[id]");
window.addEventListener("scroll", () => {
    const scrollY = window.pageYOffset;
    navSections.forEach(sec => {
        const top = sec.offsetTop - 80;
        const id = sec.getAttribute("id");
        const link = document.querySelector(`nav a[href="#${id}"]`);
        if (link) link.classList.toggle("active", scrollY >= top && scrollY < top + sec.offsetHeight);
    });
});

/* ===== SMOOTH SCROLL NAV ===== */
document.querySelectorAll('nav a[href^="#"]').forEach(a => {
    a.addEventListener("click", e => {
        e.preventDefault();
        document.querySelector(a.getAttribute("href"))?.scrollIntoView({ behavior: "smooth", block: "start" });
    });
});