using Microsoft.Maui.Dispatching;

namespace Smart_Stroller_App.Views;

public partial class HomePage : ContentPage
{
    private IDispatcherTimer? _timer;
    private double _batteryLevel = 0;

    public HomePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += UpdateBattery;
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
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Update percentage text
            BatteryValue.Text = $"{(int)_batteryLevel}%";
            
            // Update visual bar width using the same pattern as ToFPage
            var parentWidth = this.Width - 40; // 20 padding left/right on ScrollView
            if (parentWidth <= 0) parentWidth = 200; // fallback width
            var ratio = _batteryLevel / 100.0;
            BatteryBar.WidthRequest = Math.Max(4, parentWidth * ratio);
            
            // Update the color based on battery level
            UpdateBatteryColor((int)_batteryLevel);
        });
    }

    private void UpdateBatteryColor(int level)
    {
        Color newColor;
        if (level < 20)
        {
            newColor = Colors.Red;
        }
        else if (level < 50)
        {
            newColor = Color.FromArgb("#F59E0B"); // Amber
        }
        else
        {
            newColor = Color.FromArgb("#10B981"); // Teal
        }

        if (BatteryBar != null)
        {
            BatteryBar.Fill = newColor;
        }
    }
}