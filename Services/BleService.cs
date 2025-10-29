using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System.Text;

namespace Smart_Stroller_App.Services;

public interface IBleService
{
    Task<bool> ConnectAsync(string namePrefix = "SmartStroller");
    Task SendAsync(object command);
    event EventHandler<string>? NotificationReceived;
    bool IsConnected { get; }
}

public class BleService : IBleService
{
    readonly IBluetoothLE _ble = CrossBluetoothLE.Current;
    readonly IAdapter _adapter = CrossBluetoothLE.Current.Adapter;
    IDevice? _device; ICharacteristic? _tx; ICharacteristic? _rx;

    public event EventHandler<string>? NotificationReceived;
    public bool IsConnected => _device != null && _device.State == DeviceState.Connected;

    static readonly Guid SVC = Guid.Parse("6e400001-b5a3-f393-e0a9-e50e24dcca9e");
    static readonly Guid RX = Guid.Parse("6e400002-b5a3-f393-e0a9-e50e24dcca9e");
    static readonly Guid TX = Guid.Parse("6e400003-b5a3-f393-e0a9-e50e24dcca9e");

    public async Task<bool> ConnectAsync(string namePrefix = "SmartStroller")
    {
        _device = null; _tx = null; _rx = null;

        IDevice? found = null;
        _adapter.ScanTimeout = 8000;
        void h(object? s, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            if (e.Device?.Name?.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase) == true)
                found ??= e.Device;
        }
        _adapter.DeviceDiscovered += h;
        await _adapter.StartScanningForDevicesAsync();
        _adapter.DeviceDiscovered -= h;

        if (found == null) return false;

        await _adapter.ConnectToDeviceAsync(found);
        _device = found;

        var svc = await _device.GetServiceAsync(SVC);
        if (svc == null) return false;

        _tx = await svc.GetCharacteristicAsync(TX);
        _rx = await svc.GetCharacteristicAsync(RX);
        if (_tx == null || _rx == null) return false;

        // prefer WriteWithoutResponse if supported
        if (_rx.Properties.HasFlag(Plugin.BLE.Abstractions.CharacteristicPropertyType.WriteWithoutResponse))
            _rx.WriteType = Plugin.BLE.Abstractions.CharacteristicWriteType.WithoutResponse;

        _tx.ValueUpdated += (_, args) =>
        {
            var json = Encoding.UTF8.GetString(args.Characteristic.Value);
            NotificationReceived?.Invoke(this, json);
        };
        await _tx.StartUpdatesAsync();
        return true;
    }

    public async Task SendAsync(object command)
    {
        if (_rx == null) return;
        var json = System.Text.Json.JsonSerializer.Serialize(command);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _rx.WriteAsync(bytes); // WriteType controls with/without response
    }
}
