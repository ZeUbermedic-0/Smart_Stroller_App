// ViewModels/BluetoothVm.cs
using Smart_Stroller_App.Services;
using System.ComponentModel;

namespace Smart_Stroller_App.ViewModels;

public class BluetoothVm : INotifyPropertyChanged
{
    readonly IBleService _ble;
    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    // ----- Bindables -----
    string _status = "Disconnected";
    public string Status { get => _status; private set { _status = value; Raise(nameof(Status)); } }

    bool _brakeLocked;
    public bool BrakeLocked { get => _brakeLocked; private set { _brakeLocked = value; Raise(nameof(BrakeLocked)); } }

    // NEW: IMU angle (°) and ToF distance (cm)
    double _imuAngle;
    public double ImuAngle { get => _imuAngle; private set { _imuAngle = value; Raise(nameof(ImuAngle)); } }

    int _tofDistance;
    public int TofDistance { get => _tofDistance; private set { _tofDistance = value; Raise(nameof(TofDistance)); } }

    // Demo mode toggle
    bool _demoMode;
    public bool DemoMode
    {
        get => _demoMode;
        set
        {
            if (_demoMode == value) return;
            _demoMode = value;
            Raise(nameof(DemoMode));
            Status = value ? "Demo Mode" : Status;
            if (value) StartDemoLoop();
            else StopDemoLoop();
        }
    }

    public bool IsConnected => _ble.IsConnected;

    public BluetoothVm(IBleService ble)
    {
        _ble = ble;
        _ble.NotificationReceived += (_, json) => RouteIncoming(json);
    }

    // --- Parse ESP32 JSON notifications ---
    // Expect messages like:
    // {"t":"brake","locked":true}
    // {"t":"imu","angle":12.3}
    // {"t":"tof","d":123}
    double _angleEma = 0;   // smoothing buffers
    int _tofEma = 0;

    void RouteIncoming(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            var type = root.GetProperty("t").GetString();

            switch (type)
            {
                case "brake":
                    BrakeLocked = root.GetProperty("locked").GetBoolean();
                    break;

                case "imu":
                    var a = root.TryGetProperty("angle", out var ae) ? ae.GetDouble() : 0.0;
                    // EMA smoothing (tweak 0.15 for more/less smoothing)
                    _angleEma = 0.85 * _angleEma + 0.15 * a;
                    ImuAngle = Math.Round(_angleEma, 1);
                    break;

                case "tof":
                    var d = root.TryGetProperty("d", out var de) ? de.GetInt32() : 0;
                    d = Math.Clamp(d, 0, 300);
                    _tofEma = (int)(0.7 * _tofEma + 0.3 * d);
                    TofDistance = _tofEma;
                    break;
            }
        }
        catch
        {
            // ignore parse errors for demo
        }
    }

    // --- Commands / Connection ---
    public async Task<bool> ConnectAsync()
    {
        if (DemoMode)
        {
            Status = "Demo Mode";
            if (_demoCts is null) StartDemoLoop();
            return true;
        }

        Status = "Scanning…";
        var ok = await _ble.ConnectAsync("SmartStroller");
        Status = ok ? "Connected" : "Not found";
        return ok;
    }

    public async Task ToggleBrakeAsync()
    {
        if (DemoMode || !IsConnected)
        {
            BrakeLocked = !BrakeLocked;
            Status = DemoMode ? "Demo Mode" : "Disconnected (local)";
            return;
        }
        await _ble.SendAsync(new { cmd = "brake.toggle" });
        // ESP32 will notify brake state and update BrakeLocked
    }

    public async Task SetBrakeAsync(bool locked)
    {
        if (DemoMode || !IsConnected)
        {
            BrakeLocked = locked;
            Status = DemoMode ? "Demo Mode" : "Disconnected (local)";
            return;
        }
        await _ble.SendAsync(new { cmd = "brake.set", locked });
    }

    // --- Demo loop: generate calm values so UI looks great offline ---
    System.Threading.CancellationTokenSource? _demoCts;

    void StartDemoLoop()
    {
        StopDemoLoop();
        _demoCts = new();
        var token = _demoCts.Token;

        _ = Task.Run(async () =>
        {
            double t = 0;
            var rand = new Random();
            while (!token.IsCancellationRequested && DemoMode)
            {
                t += 0.15;
                // nice smooth wiggles
                var a = 8.0 + 4.0 * Math.Sin(t);     // 8° ±4°
                var d = 120 + (int)(10 * Math.Sin(t * 0.6)); // 120cm ±10

                _angleEma = 0.85 * _angleEma + 0.15 * a;
                _tofEma = (int)(0.7 * _tofEma + 0.3 * d);

                ImuAngle = Math.Round(_angleEma, 1);
                TofDistance = _tofEma;

                await Task.Delay(300, token);
            }
        }, token);
    }

    void StopDemoLoop()
    {
        try { _demoCts?.Cancel(); } catch { }
        _demoCts = null;
    }
}
