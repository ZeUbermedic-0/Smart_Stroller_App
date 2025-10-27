using System.Net.Http.Json;
using Microsoft.Maui.Dispatching;
using Smart_Stroller_App.Services;

namespace Smart_Stroller_App.Views;

public partial class BrakePage : ContentPage
{
    private IDispatcherTimer? _timer;

    public BrakePage()
    {
    
        InitializeComponent(); // Initialize UI components
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1); 
        _timer.Tick += async (_, __) => await UpdateBrake(); 
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        _timer?.Stop();
        _timer = null;
        base.OnDisappearing();
    }

    private async Task UpdateBrake() // fetch and update brake status
    {
        try
        {
            var dto = await ApiClient.Http.GetFromJsonAsync<BrakeDto>("/api/v1/brake");  
            //if (dto is null) return; 

            bool locked = dto.Locked;   // get brake status

            // chip + panel
            BrakeStatus.Text = locked ? "LOCKED" : "UNLOCKED"; // update text
            BrakePanelText.Text = BrakeStatus.Text;   // sync panel text

            BrakeChip.BackgroundColor = locked ? Color.FromArgb("#FEE2E2") : Color.FromArgb("#DCFCE7");
            BrakeStatus.TextColor = locked ? Colors.DarkRed : Colors.DarkGreen;
        }
        catch
        {
            // optional: show offline state
            //BrakeStatus.Text = "UNKNOWN"; 
            //BrakeChip.BackgroundColor = Color.FromArgb("#FFF3CD");
            //BrakeStatus.TextColor = Colors.DarkGoldenrod;
        }
    }

    private async void OnToggleBrakeClicked(object? sender, EventArgs e) // event handler for button click
    {
        try
        {
            // In demo backend we expose /brake/toggle; for real HW you’ll POST a command bus
            var resp = await ApiClient.Http.PostAsync("/api/v1/brake/toggle", content: null);
            resp.EnsureSuccessStatusCode();
            await UpdateBrake();
        }
        catch { }
    }
}

public class BrakeDto { public bool Locked { get; set; } }
