using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SqlServerManager.Mobile.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _title = "SQL Server Manager";
        private bool _isConnected = false;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
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
