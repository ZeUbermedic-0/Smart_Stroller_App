using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Smart_Stroller_App.ViewModels;

namespace Smart_Stroller_App.Views;

public partial class PositionTab : ContentPage
{
    // ToF tuning — adjust to your hardware envelope
    private const int MinCm = 0;
    private const int MaxCm = 300;   // assume 0..300cm range
    private const int CloseMax = 50;
    private const int MidMax = 150;

    private readonly BluetoothVm _vm;

    // local EMA (smoothing) so demo looks steady
    private double _angleEma = 0;
    private int _tofEma = 0;
    private const double AngleAlpha = 0.15; // 0..1 (higher = more responsive, less smooth)
    private const double DistAlpha = 0.30;

    public PositionTab()
    {
        InitializeComponent();

        // Resolve the shared VM used in Hub/Settings
        _vm = MauiProgram.Services.GetRequiredService<BluetoothVm>();
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe to VM changes (added once; harmless if repeated)
        _vm.PropertyChanged += VmOnPropertyChanged;

        // Initialize UI from current values
        ApplyAngle(_vm.ImuAngle);
        ApplyDistance(_vm.TofDistance);
    }

    protected override void OnDisappearing()
    {
        _vm.PropertyChanged -= VmOnPropertyChanged;
        base.OnDisappearing();
    }

    private void VmOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BluetoothVm.ImuAngle))
        {
            ApplyAngle(_vm.ImuAngle);
        }
        else if (e.PropertyName == nameof(BluetoothVm.TofDistance))
        {
            ApplyDistance(_vm.TofDistance);
        }
    }

    // ==================== IMU UI (Angle only, smoothed) ====================
    private void ApplyAngle(double angleDeg)
    {
        // EMA smoothing
        _angleEma = AngleAlpha * angleDeg + (1 - AngleAlpha) * _angleEma;

        // Update main angle label
        IMUAngleLabel.Text = $"Angle: {_angleEma:0.#}°";

        // Hide raw values / pitch/roll if you only want angle in demo
        IMUXVal.Text = "--";
        IMUYVal.Text = "--";
        IMUZVal.Text = "--";
        PitchValue.Text = "--";
        RollValue.Text = "--";
        YawValue.Text = "--";

        // Status bands + readable text colors
        var angle = Math.Abs(_angleEma);
        if (angle <= 0.2) // basically flat
        {
            IMUStatusText.Text = "Balanced";
            IMUStatusBar.BackgroundColor = Color.FromArgb("#DCFCE7"); // light green
            IMUStatusText.TextColor = Color.FromArgb("#0F5132"); // dark green
            AlertValue.Text = "OK";
        }
        else if (angle <= 5) // gentle tilt
        {
            IMUStatusText.Text = "Caution";
            IMUStatusBar.BackgroundColor = Color.FromArgb("#FEF9C3"); // light amber
            IMUStatusText.TextColor = Color.FromArgb("#7A5B00"); // dark amber
            AlertValue.Text = "Caution";
        }
        else // bigger tilt
        {
            IMUStatusText.Text = "Alert!";
            IMUStatusBar.BackgroundColor = Color.FromArgb("#FEE2E2"); // light red
            IMUStatusText.TextColor = Color.FromArgb("#7F1D1D"); // dark red
            AlertValue.Text = "Alert!";
        }
    }

    // ==================== ToF UI (smoothed) ====================
    private void ApplyDistance(int distanceCm)
    {
        // clamp + EMA smoothing
        var d = Math.Clamp(distanceCm, MinCm, MaxCm);
        _tofEma = (int)(DistAlpha * d + (1 - DistAlpha) * _tofEma);

        ToFValue.Text = $"{_tofEma} cm";

        // Proportional bar width based on current page width
        var parentWidth = this.Width - 40; // account for outer padding
        if (parentWidth <= 0) parentWidth = 300; // fallback prior to first layout pass
        var ratio = _tofEma / (double)MaxCm;
        ToFBar.WidthRequest = Math.Max(6, parentWidth * ratio);

        // Range chips
        HighlightChip(ChipClose, _tofEma <= CloseMax);
        HighlightChip(ChipMid, _tofEma > CloseMax && _tofEma <= MidMax);
        HighlightChip(ChipFar, _tofEma > MidMax);
    }

    private static void HighlightChip(Border chip, bool active)
    {
        chip.BackgroundColor = active ? Color.FromArgb("#DCFCE7") : Color.FromArgb("#F1F5F9");
    }
}
