using SqlServerManager.Mobile.Services;
using SqlServerManager.Mobile.Models;
using System.Collections.ObjectModel;
using Microsoft.Data.SqlClient;

namespace SqlServerManager.Mobile.Views
{
    public partial class ConnectionPage : ContentPage
    {
        private readonly IConnectionService _connectionService;
        private ObservableCollection<ConnectionInfo> _savedConnections;

        public ConnectionPage()
        {
            InitializeComponent();
            _connectionService = new ConnectionService();
            _savedConnections = new ObservableCollection<ConnectionInfo>();
            SavedConnectionsView.ItemsSource = _savedConnections;
            
            AuthTypePicker.SelectedIndex = 0; // Default to Windows Auth
            LoadSavedConnections();
        }

        private void OnAuthTypeChanged(object sender, EventArgs e)
        {
            SqlAuthSection.IsVisible = AuthTypePicker.SelectedIndex == 1;
        }

        private async void OnTestConnectionClicked(object sender, EventArgs e)
        {
            var connectionString = BuildConnectionString();
            
            StatusLabel.Text = "Testing connection...";
            StatusLabel.TextColor = Colors.Blue;

            try
            {
                bool success = await _connectionService.TestConnectionAsync(connectionString);
                
                if (success)
                {
                    StatusLabel.Text = "Connection successful!";
                    StatusLabel.TextColor = Colors.Green;
                }
                else
                {
                    StatusLabel.Text = "Connection failed.";
                    StatusLabel.TextColor = Colors.Red;
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error: {ex.Message}";
                StatusLabel.TextColor = Colors.Red;
            }
        }

        private async void OnConnectClicked(object sender, EventArgs e)
        {
            var connectionString = BuildConnectionString();
            
            StatusLabel.Text = "Connecting...";
            StatusLabel.TextColor = Colors.Blue;

            try
            {
                bool success = await _connectionService.ConnectAsync(connectionString);
                
                if (success)
                {
                    // Save the connection
                    var connectionInfo = new ConnectionInfo
                    {
                        ServerName = ServerNameEntry.Text,
                        AuthType = AuthTypePicker.SelectedIndex == 0 ? "Windows" : "SQL",
                        Username = UsernameEntry.Text,
                        Database = DatabaseEntry.Text,
                        DisplayName = $"{ServerNameEntry.Text} ({(AuthTypePicker.SelectedIndex == 0 ? "Windows Auth" : UsernameEntry.Text)})"
                    };
                    
                    await _connectionService.SaveConnectionAsync(connectionInfo);
                    
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await DisplayAlert("Connection Failed", "Unable to connect to the SQL Server.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Connection error: {ex.Message}", "OK");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private string BuildConnectionString()
        {
            var builder = new SqlConnectionStringBuilder();
            
            builder.DataSource = ServerNameEntry.Text;
            
            if (AuthTypePicker.SelectedIndex == 0) // Windows Authentication
            {
                builder.IntegratedSecurity = true;
            }
            else // SQL Server Authentication
            {
                builder.IntegratedSecurity = false;
                builder.UserID = UsernameEntry.Text;
                builder.Password = PasswordEntry.Text;
            }
            
            if (!string.IsNullOrWhiteSpace(DatabaseEntry.Text))
            {
                builder.InitialCatalog = DatabaseEntry.Text;
            }
            
            if (int.TryParse(TimeoutEntry.Text, out int timeout))
            {
                builder.ConnectTimeout = timeout;
            }
            
            // Important for Android connections
            if (TrustCertificateCheckBox.IsChecked)
            {
                builder.TrustServerCertificate = true;
            }
            
            // Enable MARS for better performance
            builder.MultipleActiveResultSets = true;
            
            return builder.ConnectionString;
        }

        private async void LoadSavedConnections()
        {
            try
            {
                var connections = await _connectionService.GetSavedConnectionsAsync();
                _savedConnections.Clear();
                foreach (var conn in connections)
                {
                    _savedConnections.Add(conn);
                }
            }
            catch (Exception ex)
            {
                // Handle error silently or log it
            }
        }

        private void OnSavedConnectionSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is ConnectionInfo connection)
            {
                ServerNameEntry.Text = connection.ServerName;
                DatabaseEntry.Text = connection.Database ?? "";
                
                if (connection.AuthType == "Windows")
                {
                    AuthTypePicker.SelectedIndex = 0;
                }
                else
                {
                    AuthTypePicker.SelectedIndex = 1;
                    UsernameEntry.Text = connection.Username ?? "";
                }
            }
        }
    }
}
