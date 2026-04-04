namespace TravelSmart.App.Services;

public static class MapService
{
    static double? _selectedLat;
    static double? _selectedLng;
    static string? _selectedName;
    static double? _routeFromLat;
    static double? _routeFromLng;
    static bool _routeMode;

    public static double? SelectedLat
    {
        get => _selectedLat;
        set
        {
            _selectedLat = value;
            // if selection cleared, disable route mode
            if (!_selectedLat.HasValue || !_selectedLng.HasValue)
                _routeMode = false;
        }
    }

    public static double? SelectedLng
    {
        get => _selectedLng;
        set
        {
            _selectedLng = value;
            if (!_selectedLat.HasValue || !_selectedLng.HasValue)
                _routeMode = false;
        }
    }

    public static string? SelectedName
    {
        get => _selectedName;
        set => _selectedName = value;
    }

    // Route from (start) coordinates
    public static double? RouteFromLat { get => _routeFromLat; set => _routeFromLat = value; }
    public static double? RouteFromLng { get => _routeFromLng; set => _routeFromLng = value; }

    // When true, MapPage will render an in-app route between RouteFrom and Selected
    public static bool RouteMode
    {
        get => _routeMode;
        set
        {
            // only enable route mode when there is a selected destination
            if (value && (!_selectedLat.HasValue || !_selectedLng.HasValue))
            {
                _routeMode = false;
            }
            else
            {
                _routeMode = value;
            }
        }
    }

    // Clear selection and disable routing
    public static void ClearSelection()
    {
        _selectedLat = null;
        _selectedLng = null;
        _selectedName = null;
        _routeFromLat = null;
        _routeFromLng = null;
        _routeMode = false;
    }
}
