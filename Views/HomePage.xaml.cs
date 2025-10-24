using Microsoft.Maui.Dispatching;                //MAUI's UI-thread timer + Dispatcher
using Smart_Stroller_App.Services;               //My ApiClient
using System.Net.Http.Json;                      //Adds GetFromJsonAsync<T>()  for HttpClient

namespace Smart_Stroller_App.Views;

public partial class HomePage : ContentPage    // [4] "partial" means XAML + .cs are merged at build
{
    private IDispatcherTimer? _timer;           // [5] UI-thread timer (lets you touch UI directly)

    public HomePage()
    {
        InitializeComponent();                 // [6] loads XAML and creates fields for x:Name controls
    }


    protected override void OnAppearing()     //Start work when visible; stop when hidden  
    {
        base.OnAppearing();

        
        _timer = Dispatcher.CreateTimer();                     //[8] timer runs on UI thread
        _timer.Interval = TimeSpan.FromSeconds(1);             //[9] tick every 1s
        _timer.Tick += async (_, __) => await UpdateBattery(); //call your poll method
        _timer.Start();                                        // [11] start polling
    }

    protected override void OnDisappearing()                   //[12] page going off-screen
    {
        _timer?.Stop();                          //works on IDispatcherTimer
        _timer = null;                           // [13] stop work when hidden (battery-friendly)
        base.OnDisappearing();
    }

    private async Task UpdateBattery()           // [14] poll server + update UI
    {
        try
        {   //Updates "Battery Value" label in xaml file
            var dto = await ApiClient.Http.GetFromJsonAsync<BatteryResponse>("/api/v1/battery");
            if (dto is null) return;

            // CA1416 warnings are harmless in MAUI; you can ignore or suppress.
#pragma warning disable CA1416
            BatteryValue.Text = $"{dto.Percent}%"; //[16] updates a Label named BatteryValue in XAML

#pragma warning restore CA1416

            // If you added a ProgressBar named BatteryProgress:
            // BatteryProgress.Progress = dto.Percent / 100.0;
        }
        catch
        {
            // ignore transient errors for demo
        }
    }
}

public class BatteryResponse
{
    public int Percent { get; set; } //[17]shape must match JSON
}
