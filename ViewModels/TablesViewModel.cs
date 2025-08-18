using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using SqlServerManager.Mobile.Models;
using SqlServerManager.Mobile.Services;

namespace SqlServerManager.Mobile.ViewModels
{
    public class TablesViewModel : INotifyPropertyChanged
    {
        private readonly IDatabaseService _databaseService;
        private ObservableCollection<TableInfo> _tables = new();
        private ObservableCollection<ColumnInfo> _columns = new();
        private string _selectedDatabase = "";
        private TableInfo? _selectedTable;
        private bool _isLoadingTables = false;
        private bool _isLoadingColumns = false;
        private string _errorMessage = "";

        public TablesViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public ObservableCollection<TableInfo> Tables
        {
            get => _tables;
            set => SetProperty(ref _tables, value);
        }

        public ObservableCollection<ColumnInfo> Columns
        {
            get => _columns;
            set => SetProperty(ref _columns, value);
        }

        public string SelectedDatabase
        {
            get => _selectedDatabase;
            set => SetProperty(ref _selectedDatabase, value);
        }

        public TableInfo? SelectedTable
        {
            get => _selectedTable;
            set
            {
                SetProperty(ref _selectedTable, value);
                if (value != null)
                    _ = LoadColumnsAsync(value.TableName);
            }
        }

        public bool IsLoadingTables
        {
            get => _isLoadingTables;
            set => SetProperty(ref _isLoadingTables, value);
        }

        public bool IsLoadingColumns
        {
            get => _isLoadingColumns;
            set => SetProperty(ref _isLoadingColumns, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public async Task LoadTablesAsync(string databaseName)
        {
            SelectedDatabase = databaseName;
            IsLoadingTables = true;
            ErrorMessage = "";
            
            try
            {
                var tables = await _databaseService.GetTablesAsync(databaseName);
                Tables = new ObservableCollection<TableInfo>(tables);
                Columns.Clear();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load tables: {ex.Message}";
            }
            finally
            {
                IsLoadingTables = false;
            }
        }

        private async Task LoadColumnsAsync(string tableName)
        {
            if (string.IsNullOrEmpty(SelectedDatabase))
                return;
                
            IsLoadingColumns = true;
            ErrorMessage = "";
            
            try
            {
                var columns = await _databaseService.GetColumnsAsync(SelectedDatabase, tableName);
                Columns = new ObservableCollection<ColumnInfo>(columns);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load columns: {ex.Message}";
            }
            finally
            {
                IsLoadingColumns = false;
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
