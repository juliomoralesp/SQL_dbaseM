using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using SqlServerManager.Mobile.Services;
using SqlServerManager.Mobile.ViewModels;
using SqlServerManager.Mobile.Views;

namespace SqlServerManager.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register services
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IConnectionService, ConnectionService>();
            
            // Register ViewModels
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<ConnectionViewModel>();
            builder.Services.AddTransient<DatabasesViewModel>();
            builder.Services.AddTransient<TablesViewModel>();
            
            // Register Views
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ConnectionPage>();
            builder.Services.AddTransient<DatabasesPage>();
            builder.Services.AddTransient<TablesPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
