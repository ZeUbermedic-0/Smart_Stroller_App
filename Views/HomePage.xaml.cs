using Microsoft.Maui.Dispatching;

namespace Smart_Stroller_App.Views; //namespace for views

public partial class HomePage : ContentPage //defines behavior for HomePage.xaml
{
    private IDispatcherTimer? _timer;    //timer for periodic updates
    private double _batteryLevel = 0;    //initial battery level

    public HomePage() //constructor
    {
        InitializeComponent(); // Initialize UI components
    }

    protected override void OnAppearing()  //customize behavior as page appears.
    {
        base.OnAppearing();
        _timer = Dispatcher.CreateTimer();  //provides a timer 
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += UpdateBattery;
        _timer.Start();
    }

    protected override void OnDisappearing()  //customize behavior as page disappears.
    {
        _timer?.Stop();
        _timer = null;
        base.OnDisappearing(); // Stop when page(base) hidden
    }

    private void UpdateBattery(object? sender, EventArgs e)
    {
        // Increment battery by 1% and wrap around at 100
        _batteryLevel = (_batteryLevel + 1) % 101;
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Update percentage text
            BatteryValue.Text = $"{(int)_batteryLevel}%";
             
            var parentWidth = this.Width - 40;       // 20 padding left/right on ScrollView
            if (parentWidth <= 0) parentWidth = 200; // Default width if not yet measured
            var ratio = _batteryLevel / 100.0;
            BatteryBar.WidthRequest = Math.Max(4, parentWidth * ratio);
            
            // Update the color based on battery level
            UpdateBatteryColor((int)_batteryLevel);
        });
    }

    private void UpdateBatteryColor(int level) //changes color based on battery level
    {
        Color newColor;  // Variable to hold the new color
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