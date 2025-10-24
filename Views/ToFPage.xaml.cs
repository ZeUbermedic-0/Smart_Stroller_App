using System.Net.Http.Json;
using Microsoft.Maui.Dispatching;
using Smart_Stroller_App.Services;

namespace Smart_Stroller_App.Views;

public partial class ToFPage : ContentPage
{
    private IDispatcherTimer? _timer;

    // tune these to your expected sensor envelope
    private const int MinCm = 0;
    private const int MaxCm = 300;   // assume 0..300cm range
    private const int CloseMax = 50;
    private const int MidMax = 150;

    public ToFPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += async (_, __) => await UpdateTof();
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        _timer?.Stop();
        _timer = null;
        base.OnDisappearing();
    }

    private async Task UpdateTof()
    {
        try
        {
            var dto = await ApiClient.Http.GetFromJsonAsync<ToFDto>("/api/v1/tof");
            if (dto is null) return;

            var d = Math.Clamp(dto.Distance, MinCm, MaxCm);
            ToFValue.Text = $"{d} cm";

            // Update visual bar (proportional width)
            // Parent grid has full width; we’ll set a WidthRequest based on page width.
            // Defer to the next layout pass with MainThread.BeginInvokeOnMainThread.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var parentWidth = this.Width - 40; // 20 padding left/right on ScrollView
                if (parentWidth <= 0) parentWidth = 300; // fallback
                var ratio = d / (double)MaxCm;
                ToFBar.WidthRequest = Math.Max(6, parentWidth * ratio);
            });

            // Highlight range chips
            HighlightChip(ChipClose, d <= CloseMax);
            HighlightChip(ChipMid, d > CloseMax && d <= MidMax);
            HighlightChip(ChipFar, d > MidMax);
        }
        catch
        {
            // optional: show an "offline" chip state
            HighlightChip(ChipClose, false);
            HighlightChip(ChipMid, false);
            HighlightChip(ChipFar, false);
            ToFValue.Text = "-- cm";
            ToFBar.WidthRequest = 0;
        }
    }

    private static void HighlightChip(Border chip, bool active)
    {
        chip.BackgroundColor = active ? Color.FromArgb("#DCFCE7") : Color.FromArgb("#F1F5F9");
    }
}

public class ToFDto { public int Distance { get; set; } }
