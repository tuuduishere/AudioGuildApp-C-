using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using TravelSmart.API.Models;

namespace TravelSmart.API.Hubs
{
    public class TravelHub : Hub
    {
        // Nhúng Database Context để lấy dữ liệu cũ
        private readonly VinhKhanhTravelDbContext _db;

        private static int _totalOnline = 0;
        private static readonly ConcurrentDictionary<string, int> _poiViewers = new();
        private static readonly ConcurrentDictionary<string, string> _userLocations = new();

        // Để -1 để làm dấu nhận biết "Server vừa mới khởi động"
        private static int _totalListens = -1;

        // Constructor nhận DbContext
        public TravelHub(VinhKhanhTravelDbContext db)
        {
            _db = db;
        }

        public override async Task OnConnectedAsync()
        {
            // NẾU SERVER VỪA BẬT -> MÓC TỔNG SỐ LƯỢT NGHE TỪ DATABASE LÊN!
            if (_totalListens == -1)
            {
                try
                {
                    _totalListens = _db.VisitLogs.Count();
                }
                catch
                {
                    _totalListens = 0;
                }
            }

            string clientType = Context.GetHttpContext()?.Request.Query["clientType"].ToString();
            if (clientType == "app")
            {
                Interlocked.Increment(ref _totalOnline);
                await Clients.All.SendAsync("UpdateOnlineCount", _totalOnline);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string clientType = Context.GetHttpContext()?.Request.Query["clientType"].ToString();
            if (clientType == "app")
            {
                Interlocked.Decrement(ref _totalOnline);
                await Clients.All.SendAsync("UpdateOnlineCount", _totalOnline);
            }

            var connId = Context.ConnectionId;
            if (_userLocations.TryRemove(connId, out var poiId)) { LeavePoiInternal(poiId); }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinPoi(string poiId)
        {
            var connId = Context.ConnectionId;
            if (_userLocations.TryGetValue(connId, out var oldPoi)) { LeavePoiInternal(oldPoi); }

            _userLocations[connId] = poiId;
            _poiViewers.AddOrUpdate(poiId, 1, (key, count) => count + 1);
            await Clients.All.SendAsync("UpdateViewerCount", poiId, _poiViewers[poiId]);
        }

        public async Task LeavePoi(string poiId)
        {
            var connId = Context.ConnectionId;
            if (_userLocations.TryRemove(connId, out var savedPoi) && savedPoi == poiId) { LeavePoiInternal(poiId); }
        }

        private void LeavePoiInternal(string poiId)
        {
            if (_poiViewers.ContainsKey(poiId) && _poiViewers[poiId] > 0)
            {
                _poiViewers[poiId]--;
                Clients.All.SendAsync("UpdateViewerCount", poiId, _poiViewers[poiId]);
            }
        }

        // ==============================================================================
        // ==============================================================================
        public async Task LogListen(string poiId)
        {
   
            Interlocked.Increment(ref _totalListens);
            await Clients.All.SendAsync("UpdateTotalListens", _totalListens);
        }

        public async Task RequestCurrentCounts()
        {
            await Clients.Caller.SendAsync("UpdateOnlineCount", _totalOnline);
            foreach (var kvp in _poiViewers) { await Clients.Caller.SendAsync("UpdateViewerCount", kvp.Key, kvp.Value); }

            // Gửi Tổng lượt nghe cho Admin khi vừa mở web
            await Clients.Caller.SendAsync("UpdateTotalListens", _totalListens == -1 ? 0 : _totalListens);
        }
    }
}