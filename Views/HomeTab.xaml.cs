using System.Net.Http.Json;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using Smart_Stroller_App.Services;

namespace Smart_Stroller_App.Views
{
    // Target: merge BrakePage logic into HomeTab
    public partial class HomeTab : ContentPage
    {
        private IDispatcherTimer? _timer;    // single timer for battery demo + brake polling
        private double _batteryLevel = 0;    // demo battery level (0..100)

        public HomeTab()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += async (_, __) =>
            {
                // keep existing battery demo behavior
                UpdateBattery();

                // poll brake status from backend
                await UpdateBrakeAsync();
            };
            _timer.Start();
        }

        protected override void OnDisappearing()
        {
            _timer?.Stop();
            _timer = null;
            base.OnDisappearing();
        }

        // ================= Battery (existing demo) =================
        private void UpdateBattery()
        {
            // Increment battery by 1% and wrap around at 100
            _batteryLevel = (_batteryLevel + 1) % 101;

            // We're already on UI thread (Dispatcher timer), but be defensive if reused elsewhere
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BatteryValue.Text = $"{(int)_batteryLevel}%";

                var parentWidth = this.Width - 40;       // 20 padding left/right on ScrollView
                if (parentWidth <= 0) parentWidth = 200; // Default width if not yet measured
                var ratio = _batteryLevel / 100.0;
                BatteryBar.WidthRequest = Math.Max(4, parentWidth * ratio);

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
                BatteryBar.Fill = new SolidColorBrush(newColor); // Rectangle.Fill expects a Brush
            }
        }

        // ================= Brake (merged from BrakePage) =================
        private async Task UpdateBrakeAsync()
        {
            try
            {
                var dto = await ApiClient.Http.GetFromJsonAsync<BrakeDto>("/api/v1/brake");
                if (dto == null) return;

                bool locked = dto.Locked;

                // chip + panel
                BrakeStatus.Text = locked ? "LOCKED" : "UNLOCKED";
                BrakePanelText.Text = BrakeStatus.Text; // keep in sync

                BrakeChip.BackgroundColor = locked ? Color.FromArgb("#FEE2E2") : Color.FromArgb("#DCFCE7");
                BrakeStatus.TextColor = locked ? Colors.DarkRed : Colors.DarkGreen;
            }
            catch
            {
                // Optional offline/unknown state — uncomment if you want to surface it
                //BrakeStatus.Text = "UNKNOWN";
                //BrakeChip.BackgroundColor = Color.FromArgb("#FFF3CD");
                //BrakeStatus.TextColor = Colors.DarkGoldenrod;
            }
        }

        private async void OnToggleBrakeClicked(object? sender, EventArgs e)
        {
            try
            {
                // Demo endpoint that toggles brake; adjust for your real API/command bus
                var resp = await ApiClient.Http.PostAsync("/api/v1/brake/toggle", content: null);
                resp.EnsureSuccessStatusCode();
                await UpdateBrakeAsync();
            }
            catch
            {
                // ignore transient errors in demo
            }
        }
    }

    // Keep DTO nearby (move to Models if preferred)
    public class BrakeDto { public bool Locked { get; set; } }
}
