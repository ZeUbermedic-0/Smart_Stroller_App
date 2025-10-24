using System.Net.Http.Json;
using Microsoft.Maui.Dispatching;
using Smart_Stroller_App.Services;

namespace Smart_Stroller_App.Views;

public partial class IMUPage : ContentPage
{
    private IDispatcherTimer? _timer;                                    // [C1] UI-thread timer (safe to touch UI)

    public IMUPage()
    {
        InitializeComponent();                                           //[C2] Wires x:Name fields from XAML
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += async (_, __) => await UpdateIMUAsync();          //poll once every second
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        _timer?.Stop();
        _timer = null;                                                    // [C4] Stop when page hidden
        base.OnDisappearing();
    }

    private async Task UpdateIMUAsync()
    {
        try
        {
            // Server returns: { x: double, y: double, z: double }        // [C5]
            var dto = await ApiClient.Http.GetFromJsonAsync<IMUDto>("/api/v1/imu");
            if (dto is null) return;
            
            UpdateImuUi(dto);
        }
        catch
        {
            // 'swallow' transient demo errors
        }
    }

    private void UpdateImuUi(IMUDto dto)
    {
        // 1) Raw axis readout
        IMUXVal.Text = dto.X.ToString("0.000");
        IMUYVal.Text = dto.Y.ToString("0.000");
        IMUZVal.Text = dto.Z.ToString("0.000");

        // 2) Compute tilt angle (degrees) from Z vs XY magnitude                                       // [C9]
        var xy = Math.Sqrt(dto.X * dto.X + dto.Y * dto.Y);                                              // Angle = Atan2( sqrt(x^2 + y^2), z ) * 180/pi    
        var angle = Math.Atan2(xy, dto.Z) * (180.0 / Math.PI);
        
        IMUAngleLabel.Text = $"Angle: {angle:0.#}°";

        // 3)Pitch/Roll from accelerometer (common simple estimates)
        // pitch = atan2( X, sqrt(Y^2 + Z^2) )
        // roll  = atan2( Y, Z )3) Status bands + readable text colors 

        var pitch = Math.Atan2(dto.X, Math.Sqrt(dto.Y * dto.Y + dto.Z * dto.Z)) * (90.0 / Math.PI);
        var roll = Math.Atan2(dto.Y, dto.Z) * (180.0 / Math.PI);

        PitchValue.Text = $"{pitch:0.#}°";
        RollValue.Text = $"{roll:0.#}°";
        
        // 4) Yaw needs a magnetometer (or gyro integration); show "--" unless you have it
        YawValue.Text = "--";

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
}

// JSON shape: { "x": <double>, "y": <double>, "z": <double> }
public class IMUDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}
