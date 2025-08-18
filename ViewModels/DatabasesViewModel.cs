using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using SqlServerManager.Mobile.Models;
using SqlServerManager.Mobile.Services;

namespace SqlServerManager.Mobile.ViewModels
{
    public class DatabasesViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private ObservableCollection<DatabaseInfo> _databases = new();
        private bool _isLoading = false;
        private string _errorMessage = "";

        public DatabasesViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public ObservableCollection<DatabaseInfo> Databases
        {
            get => _databases;
            set => SetProperty(ref _databases, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public async Task LoadDatabasesAsync()
        {
            IsLoading = true;
            ErrorMessage = "";
            
            try
            {
                var databases = await _databaseService.GetDatabasesAsync();
                Databases = new ObservableCollection<DatabaseInfo>(databases);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load databases: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
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
