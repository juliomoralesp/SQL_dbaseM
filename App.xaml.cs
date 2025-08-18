using Microsoft.Maui.Controls;

namespace SqlServerManager.Mobile
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);
            
            // Set default window size for desktop platforms
            const int newWidth = 1200;
            const int newHeight = 700;

            window.Width = newWidth;
            window.Height = newHeight;

            return window;
        }
    }
}
