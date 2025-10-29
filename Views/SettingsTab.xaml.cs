using Microsoft.Extensions.DependencyInjection;
using Smart_Stroller_App.ViewModels;

#if ANDROID
using Android.App;
#endif

namespace Smart_Stroller_App.Views;

public partial class SettingsTab : ContentPage
{
    readonly BluetoothVm _vm;

    public SettingsTab()
    {
        InitializeComponent();
        _vm = MauiProgram.Services.GetRequiredService<BluetoothVm>();
        BindingContext = _vm;
    }

    private async void OnConnectClicked(object? sender, EventArgs e)
    {
#if ANDROID
        await BlePermissions.EnsureAsync(Platform.CurrentActivity as Activity
            ?? throw new InvalidOperationException("No current Activity"));
#endif
        await _vm.ConnectAsync();
    }

    private async void OnLockClicked(object? s, EventArgs e) => await _vm.SetBrakeAsync(true);
    private async void OnUnlockClicked(object? s, EventArgs e) => await _vm.SetBrakeAsync(false);
}
