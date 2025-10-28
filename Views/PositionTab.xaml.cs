using System.Net.Http.Json;
using Microsoft.Maui.Dispatching;
using Smart_Stroller_App.Services;

namespace Smart_Stroller_App.Views;

public partial class PositionTab : ContentPage
{
    private IDispatcherTimer? _timer; // single UI-thread timer for both sensors

    // ToF tuning — adjust to your hardware envelope
    private const int MinCm = 0;
    private const int MaxCm = 300;   // assume 0..300cm range
    private const int CloseMax = 50;
    private const int MidMax = 150;

    public PositionTab()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += async (_, __) => await PollAsync(); // poll both endpoints once per second
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        _timer?.Stop();
        _timer = null;
        base.OnDisappearing();
    }

    private async Task PollAsync()
    {
        try
        {
            // Kick off both calls concurrently to minimize UI latency
            var imuTask = ApiClient.Http.GetFromJsonAsync<IMUDto>("/api/v1/imu");
            var tofTask = ApiClient.Http.GetFromJsonAsync<ToFDto>("/api/v1/tof");

            await Task.WhenAll(Safely(imuTask), Safely(tofTask));

            var imu = imuTask.Status == TaskStatus.RanToCompletion ? imuTask.Result : null;
            var tof = tofTask.Status == TaskStatus.RanToCompletion ? tofTask.Result : null;

            if (imu != null) UpdateImuUi(imu); else ClearImuUi();
            if (tof != null) UpdateTofUi(tof); else ClearTofUi();
        }
        catch
        {
            // swallow transient demo errors
        }
    }

    private static async Task<T?> Safely<T>(Task<T> t)
    {
        try { return await t; } catch { return default; }
    }

    // ==================== IMU UI ====================
    private void UpdateImuUi(IMUDto dto)
    {
        // 1) Raw axis readout
        IMUXVal.Text = dto.X.ToString("0.000");
        IMUYVal.Text = dto.Y.ToString("0.000");
        IMUZVal.Text = dto.Z.ToString("0.000");

        // 2) Compute tilt angle (degrees) from Z vs XY magnitude
        var xy = Math.Sqrt(dto.X * dto.X + dto.Y * dto.Y); // Angle = Atan2( sqrt(x^2 + y^2), z ) * 180/pi
        var angle = Math.Atan2(xy, dto.Z) * (180.0 / Math.PI);
        IMUAngleLabel.Text = $"Angle: {angle:0.#}°";

        // 3) Pitch/Roll from accelerometer (simple estimates)
        // pitch = atan2( X, sqrt(Y^2 + Z^2) )  (scaled to ±90° here)
        // roll  = atan2( Y, Z )
        var pitch = Math.Atan2(dto.X, Math.Sqrt(dto.Y * dto.Y + dto.Z * dto.Z)) * (90.0 / Math.PI);
        var roll = Math.Atan2(dto.Y, dto.Z) * (180.0 / Math.PI);
        PitchValue.Text = $"{pitch:0.#}°";
        RollValue.Text = $"{roll:0.#}°";

        // 4) Yaw needs a magnetometer (or gyro integration)
        YawValue.Text = "--";

        // 5) Status bands + readable text colors
        if (angle == 0)
        {
            IMUStatusText.Text = "Balanced";
            IMUStatusBar.BackgroundColor = Color.FromArgb("#DCFCE7"); // light green
            IMUStatusText.TextColor = Color.FromArgb("#0F5132"); // dark green
            AlertValue.Text = "OK";
        }
        else if (angle <= 1)
        {
            IMUStatusText.Text = "Caution";
            IMUStatusBar.BackgroundColor = Color.FromArgb("#FEF9C3"); // light amber
            IMUStatusText.TextColor = Color.FromArgb("#7A5B00"); // dark amber
            AlertValue.Text = "Caution";
        }
        else
        {
            IMUStatusText.Text = "Alert!";
            IMUStatusBar.BackgroundColor = Color.FromArgb("#FEE2E2"); // light red
            IMUStatusText.TextColor = Color.FromArgb("#7F1D1D"); // dark red
            AlertValue.Text = "Alert!";
        }
    }

    private void ClearImuUi()
    {
        IMUXVal.Text = "--";
        IMUYVal.Text = "--";
        IMUZVal.Text = "--";
        IMUAngleLabel.Text = "Angle: --";
        PitchValue.Text = "--";
        RollValue.Text = "--";
        YawValue.Text = "--";
        IMUStatusText.Text = "--";
        IMUStatusBar.BackgroundColor = Colors.Transparent;
        AlertValue.Text = "--";
    }

    // ==================== ToF UI ====================
    private void UpdateTofUi(ToFDto dto)
    {
        var d = Math.Clamp(dto.Distance, MinCm, MaxCm);
        ToFValue.Text = $"{d} cm";

        // Proportional bar width based on current page width
        var parentWidth = this.Width - 40; // account for outer padding if any
        if (parentWidth <= 0) parentWidth = 300; // fallback prior to first layout pass
        var ratio = d / (double)MaxCm;
        ToFBar.WidthRequest = Math.Max(6, parentWidth * ratio);

        // Range chips
        HighlightChip(ChipClose, d <= CloseMax);
        HighlightChip(ChipMid, d > CloseMax && d <= MidMax);
        HighlightChip(ChipFar, d > MidMax);
    }

    private void ClearTofUi()
    {
        HighlightChip(ChipClose, false);
        HighlightChip(ChipMid, false);
        HighlightChip(ChipFar, false);
        ToFValue.Text = "-- cm";
        ToFBar.WidthRequest = 0;
    }

    private static void HighlightChip(Border chip, bool active)
    {
        chip.BackgroundColor = active ? Color.FromArgb("#DCFCE7") : Color.FromArgb("#F1F5F9");
    }
}

// =============== DTOs (can live here or in a shared Models folder) ===============
public class IMUDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

public class ToFDto
{
    public int Distance { get; set; }
}
