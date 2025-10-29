using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Smart_Stroller_App.ViewModels;

#if ANDROID
using Android.App;
#endif

namespace Smart_Stroller_App.Views
{
    public partial class HomeTab : ContentPage
    {
        private readonly BluetoothVm _vm;

        public HomeTab()
        {
            InitializeComponent();
            _vm = MauiProgram.Services.GetRequiredService<BluetoothVm>();
            BindingContext = _vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

#if ANDROID
            await BlePermissions.EnsureAsync(Platform.CurrentActivity as Activity
                ?? throw new InvalidOperationException("No current Activity"));
#endif
            _ = _vm.ConnectAsync();

            // reflect BrakeLocked into your chip (quick code-behind; can refactor to DataTriggers later)
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(BluetoothVm.BrakeLocked))
                {
                    var locked = _vm.BrakeLocked;
                    BrakeStatus.Text = locked ? "LOCKED" : "UNLOCKED";
                    BrakePanelText.Text = BrakeStatus.Text;
                    BrakeChip.BackgroundColor = locked ? Color.FromArgb("#FEE2E2") : Color.FromArgb("#DCFCE7");
                    BrakeStatus.TextColor = locked ? Colors.DarkRed : Colors.DarkGreen;
                }
            };
        }

        private async void OnToggleBrakeClicked(object? sender, EventArgs e)
        {
            await _vm.ToggleBrakeAsync(); // works connected OR in Demo Mode
        }
    }
}
