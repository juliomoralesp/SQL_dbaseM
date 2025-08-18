using SqlServerManager.Mobile.Views;

namespace SqlServerManager.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Register routes for navigation
            Routing.RegisterRoute(nameof(ConnectionPage), typeof(ConnectionPage));
            Routing.RegisterRoute(nameof(DatabasesPage), typeof(DatabasesPage));
            Routing.RegisterRoute(nameof(TablesPage), typeof(TablesPage));
        }
    }
}
