using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui;

namespace SqlServerManager.Mobile
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Request necessary permissions for network access
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                RequestPermissions(new[] { 
                    Android.Manifest.Permission.Internet,
                    Android.Manifest.Permission.AccessNetworkState
                }, 0);
            }
        }
    }
}
