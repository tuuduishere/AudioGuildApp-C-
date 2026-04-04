using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Storage;

namespace TravelSmart.App.Services;

public static class FavoriteService
{
    const string Key = "favorites_v1";

    public static event Action? FavoritesChanged;

    public static HashSet<string> Load()
    {
        var saved = Preferences.Default.Get(Key, string.Empty);
        if (string.IsNullOrEmpty(saved)) return new HashSet<string>();
        return saved.Split(';', System.StringSplitOptions.RemoveEmptyEntries).ToHashSet();
    }

    public static void Save(HashSet<string> set)
    {
        var s = string.Join(';', set);
        Preferences.Default.Set(Key, s);
        FavoritesChanged?.Invoke();
    }
}
