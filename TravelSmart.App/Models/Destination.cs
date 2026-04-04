namespace TravelSmart.App.Models;

using System.ComponentModel;

public class Destination : INotifyPropertyChanged
{
    string name = "";
    string image = "";
    double rating;
    string price = "";
    double lat;
    double lng;
    bool isFavorite;

    public string Name { get => name; set { if (name == value) return; name = value; NotifyPropertyChanged(nameof(Name)); } }
    public string Image { get => image; set { if (image == value) return; image = value; NotifyPropertyChanged(nameof(Image)); } }
    public double Rating { get => rating; set { if (rating == value) return; rating = value; NotifyPropertyChanged(nameof(Rating)); } }
    public string Price { get => price; set { if (price == value) return; price = value; NotifyPropertyChanged(nameof(Price)); } }
    public double Lat { get => lat; set { if (lat == value) return; lat = value; NotifyPropertyChanged(nameof(Lat)); } }
    public double Lng { get => lng; set { if (lng == value) return; lng = value; NotifyPropertyChanged(nameof(Lng)); } }
    public bool IsFavorite { get => isFavorite; set { if (isFavorite == value) return; isFavorite = value; NotifyPropertyChanged(nameof(IsFavorite)); } }
    string description = "";
    public string Description { get => description; set { if (description == value) return; description = value; NotifyPropertyChanged(nameof(Description)); } }
    // Multiple images support for carousel
    public List<string> Images { get; set; } = new List<string>();

    public event PropertyChangedEventHandler? PropertyChanged;
    void NotifyPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
}
