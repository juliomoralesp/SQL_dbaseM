using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using SqlServerManager.Mobile.Models;
using SqlServerManager.Mobile.Services;

namespace SqlServerManager.Mobile.ViewModels
{
    public class ConnectionViewModel : INotifyPropertyChanged
    {
        private readonly IConnectionService _connectionService;
        private string _serverName = "";
        private string _username = "";
        private string _password = "";
        private string _database = "";
        private bool _useWindowsAuth = true;
        private bool _isConnecting = false;
        private ObservableCollection<ConnectionInfo> _savedConnections = new();

        public ConnectionViewModel(IConnectionService connectionService)
        {
            _connectionService = connectionService;
            LoadSavedConnections();
        }

        public string ServerName
        {
            get => _serverName;
            set => SetProperty(ref _serverName, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string Database
        {
            get => _database;
            set => SetProperty(ref _database, value);
        }

        public bool UseWindowsAuth
        {
            get => _useWindowsAuth;
            set => SetProperty(ref _useWindowsAuth, value);
        }

        public bool IsConnecting
        {
            get => _isConnecting;
            set => SetProperty(ref _isConnecting, value);
        }

        public ObservableCollection<ConnectionInfo> SavedConnections
        {
            get => _savedConnections;
            set => SetProperty(ref _savedConnections, value);
        }

        private async void LoadSavedConnections()
        {
            var connections = await _connectionService.GetSavedConnectionsAsync();
            SavedConnections = new ObservableCollection<ConnectionInfo>(connections);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
