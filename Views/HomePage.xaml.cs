using Microsoft.Maui.Dispatching;
using Smart_Stroller_App.Services;
using System.Net.Http.Json;

namespace Smart_Stroller_App.Views;

public partial class HomePage : ContentPage
{
    private IDispatcherTimer? _timer;
    private int _batteryLevel = 0;  // Local battery tracking

    public HomePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += UpdateBattery;  // Changed to direct method
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        _timer?.Stop();
        _timer = null;
        base.OnDisappearing();
    }

    private void UpdateBattery(object? sender, EventArgs e)
    {
        // Increment battery by 1% and wrap around at 100
        _batteryLevel = (_batteryLevel + 1) % 101;
        BatteryValue.Text = $"{_batteryLevel}%";
    }
}