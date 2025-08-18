using System.Collections.ObjectModel;
using SqlServerManager.Mobile.Services;
using SqlServerManager.Mobile.Models;

namespace SqlServerManager.Mobile.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly IDatabaseService _databaseService;
        private readonly IConnectionService _connectionService;
        private ObservableCollection<DatabaseInfo> _databases;
        private ObservableCollection<TableInfo> _tables;
        private ObservableCollection<ColumnInfo> _columns;
        private string _currentDatabase;

        public MainPage()
        {
            InitializeComponent();
            
            // Initialize services (in real app, use dependency injection)
            _databaseService = new DatabaseService();
            _connectionService = new ConnectionService();
            
            _databases = new ObservableCollection<DatabaseInfo>();
            _tables = new ObservableCollection<TableInfo>();
            _columns = new ObservableCollection<ColumnInfo>();
            
            DatabasesCollectionView.ItemsSource = _databases;
            TablesCollectionView.ItemsSource = _tables;
            ColumnsCollectionView.ItemsSource = _columns;
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ConnectionPage));
        }

        private async void OnDisconnectClicked(object sender, EventArgs e)
        {
            try
            {
                _connectionService.Disconnect();
                UpdateConnectionStatus(false);
                _databases.Clear();
                _tables.Clear();
                _columns.Clear();
                StatusLabel.Text = "Disconnected";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to disconnect: {ex.Message}", "OK");
            }
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadDatabases();
        }

        private void OnDatabasesTabClicked(object sender, EventArgs e)
        {
            DatabasesView.IsVisible = true;
            TablesView.IsVisible = false;
            DatabasesTabButton.BackgroundColor = Color.FromArgb("#512BD4");
            TablesTabButton.BackgroundColor = Colors.Gray;
        }

        private void OnTablesTabClicked(object sender, EventArgs e)
        {
            DatabasesView.IsVisible = false;
            TablesView.IsVisible = true;
            TablesTabButton.BackgroundColor = Color.FromArgb("#512BD4");
            DatabasesTabButton.BackgroundColor = Colors.Gray;
        }

        private async void OnDatabaseSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is DatabaseInfo database)
            {
                _currentDatabase = database.Name;
                await LoadTables(database.Name);
            }
        }

        private async void OnDatabaseDoubleTapped(object sender, EventArgs e)
        {
            if (sender is Grid grid && grid.BindingContext is DatabaseInfo database)
            {
                _currentDatabase = database.Name;
                await LoadTables(database.Name);
                OnTablesTabClicked(sender, e);
            }
        }

        private void OnDatabaseTapped(object sender, EventArgs e)
        {
            // Handle right-click context menu here
            // For mobile, this could show an action sheet
        }

        private async void OnTableSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is TableInfo table)
            {
                await LoadColumns(_currentDatabase, table.TableName);
            }
        }

        private async Task LoadDatabases()
        {
            try
            {
                StatusLabel.Text = "Loading databases...";
                var databases = await _databaseService.GetDatabasesAsync();
                
                _databases.Clear();
                foreach (var db in databases)
                {
                    _databases.Add(db);
                }
                
                StatusLabel.Text = $"Loaded {databases.Count} databases";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load databases: {ex.Message}", "OK");
                StatusLabel.Text = "Error loading databases";
            }
        }

        private async Task LoadTables(string databaseName)
        {
            try
            {
                StatusLabel.Text = $"Loading tables from {databaseName}...";
                var tables = await _databaseService.GetTablesAsync(databaseName);
                
                _tables.Clear();
                foreach (var table in tables)
                {
                    _tables.Add(table);
                }
                
                StatusLabel.Text = $"Loaded {tables.Count} tables";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load tables: {ex.Message}", "OK");
                StatusLabel.Text = "Error loading tables";
            }
        }

        private async Task LoadColumns(string databaseName, string tableName)
        {
            try
            {
                StatusLabel.Text = $"Loading columns from {tableName}...";
                var columns = await _databaseService.GetColumnsAsync(databaseName, tableName);
                
                _columns.Clear();
                foreach (var column in columns)
                {
                    _columns.Add(column);
                }
                
                StatusLabel.Text = $"Loaded {columns.Count} columns";
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load columns: {ex.Message}", "OK");
                StatusLabel.Text = "Error loading columns";
            }
        }

        private void UpdateConnectionStatus(bool connected)
        {
            if (connected)
            {
                ConnectionLabel.Text = "Connected";
                ConnectionLabel.TextColor = Colors.Green;
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
                RefreshButton.IsEnabled = true;
            }
            else
            {
                ConnectionLabel.Text = "Not Connected";
                ConnectionLabel.TextColor = Colors.Red;
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                RefreshButton.IsEnabled = false;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            if (_connectionService.IsConnected)
            {
                UpdateConnectionStatus(true);
                await LoadDatabases();
            }
        }
    }
}
