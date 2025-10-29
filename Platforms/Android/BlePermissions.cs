#if ANDROID
using Android;
using Android.App;
using AndroidX.Core.App;
using AndroidX.Core.Content;

public static class BlePermissions
{
    public static Task EnsureAsync(Activity activity)
    {
        var missing = new List<string>();
        void Need(string p)
        {
            if (ContextCompat.CheckSelfPermission(activity, p) != Android.Content.PM.Permission.Granted)
                missing.Add(p);
        }
        Need(Manifest.Permission.BluetoothScan);
        Need(Manifest.Permission.BluetoothConnect);
        Need(Manifest.Permission.AccessFineLocation);
        if (missing.Count > 0)
            ActivityCompat.RequestPermissions(activity, missing.ToArray(), 42);
        return Task.CompletedTask;
    }
}
#endif
