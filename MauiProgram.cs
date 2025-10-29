// MauiProgram.cs
using Smart_Stroller_App;                 // <-- namespace where App lives
using Smart_Stroller_App.Services;
using Smart_Stroller_App.ViewModels;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

public static class MauiProgram
{
    public static IServiceProvider Services { get; private set; } = default!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // DI registrations (you already added these)
        builder.Services.AddSingleton<IBleService, BleService>();
        builder.Services.AddSingleton<BluetoothVm>();

        var app = builder.Build();

        // keep a reference to the service provider
        Services = app.Services;

        return app;
    }
}
