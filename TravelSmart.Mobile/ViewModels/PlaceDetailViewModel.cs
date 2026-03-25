using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.Audio;
using TravelSmart.Mobile.Models;

namespace TravelSmart.Mobile.ViewModels;

public partial class PlaceDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _player;

    private Place? _selectedPlace;
    public Place? SelectedPlace { get => _selectedPlace; set => SetProperty(ref _selectedPlace, value); }

    private string _playPauseIcon = "▶️";
    public string PlayPauseIcon { get => _playPauseIcon; set => SetProperty(ref _playPauseIcon, value); }

    public PlaceDetailViewModel(IAudioManager audioManager) => _audioManager = audioManager;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Place", out var value) && value is Place p) SelectedPlace = p;
    }

    // [RelayCommand] sẽ tự tạo ra một thuộc tính tên là PlayPauseAudioCommand
    // Đảm bảo trong XAML ông gọi đúng: Command="{Binding PlayPauseAudioCommand}"
    [RelayCommand]
    private async Task PlayPauseAudio()
    {
        if (SelectedPlace == null) return;
        try
        {
            if (_player == null)
            {
                var stream = await new HttpClient().GetStreamAsync(SelectedPlace.AudioUrl);
                _player = _audioManager.CreatePlayer(stream);
            }
            if (_player.IsPlaying) { _player.Pause(); PlayPauseIcon = "▶️"; }
            else { _player.Play(); PlayPauseIcon = "⏸️"; }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }
}