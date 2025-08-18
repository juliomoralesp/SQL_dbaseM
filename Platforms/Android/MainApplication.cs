using Android.App;
using Android.Runtime;
using Microsoft.Maui;

namespace SqlServerManager.Mobile
{
    [Application(UsesCleartextTraffic = true)] // Allow HTTP for SQL Server connections
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
