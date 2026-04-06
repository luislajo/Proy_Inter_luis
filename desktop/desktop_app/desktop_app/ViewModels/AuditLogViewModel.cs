using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using desktop_app.Commands;
using desktop_app.Models;
using desktop_app.Services;
using desktop_app.Views;

namespace desktop_app.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de historial de auditoría.
    /// </summary>
    public class AuditLogViewModel : ViewModelBase
    {
        // Almacén completo (sin filtrar)
        private readonly ObservableCollection<AuditLogModel> _allLogs = new();

        // Lo que la vista muestra
        public ObservableCollection<AuditLogModel> Logs { get; } = new();

        // Opciones del combo de filtro de acción
        public ObservableCollection<string> FilterOptions { get; } = new()
        {
            "Todos", "CREATE", "UPDATE", "CANCEL", "DELETE", "PAYMENT"
        };

        // Filtro seleccionado de acción
        private string _selectedFilter = "Todos";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set => SetProperty(ref _selectedFilter, value);
        }

        // Opciones del combo de filtro de colección
        public ObservableCollection<string> FilterCollectionOptions { get; } = new()
        {
            "Todas", "Room", "Booking"
        };

        // Filtro seleccionado de colección
        private string _selectedCollectionFilter = "Todas";
        public string SelectedCollectionFilter
        {
            get => _selectedCollectionFilter;
            set => SetProperty(ref _selectedCollectionFilter, value);
        }

        // Indicador de carga
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Texto de estado
        private string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        // Comandos
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public AuditLogViewModel()
        {
            RefreshCommand = new AsyncRelayCommand(LoadLogsAsync);
            SearchCommand = new RelayCommand(_ => ApplyFilter());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());

            _ = LoadLogsAsync();
        }

        private async Task LoadLogsAsync()
        {
            IsLoading = true;
            StatusText = "Cargando...";

            try
            {
                var logs = await AuditLogService.GetAllAuditLogsAsync();

                _allLogs.Clear();
                foreach (var log in logs)
                    _allLogs.Add(log);

                ApplyFilter();

                StatusText = $"{_allLogs.Count} registro(s) encontrado(s)";
            }
            catch (Exception ex)
            {
                StatusText = "Error al cargar";
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            Logs.Clear();

            var filtered = _allLogs.AsEnumerable();

            // Filtro por acción
            if (!string.IsNullOrEmpty(_selectedFilter) && _selectedFilter != "Todos")
            {
                filtered = filtered.Where(l => l.Action != null &&
                    l.Action.Equals(_selectedFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Filtro por colección
            if (!string.IsNullOrEmpty(_selectedCollectionFilter) && _selectedCollectionFilter != "Todas")
            {
                filtered = filtered.Where(l => l.EntityType != null &&
                    l.EntityType.Equals(_selectedCollectionFilter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var log in filtered)
                Logs.Add(log);

            StatusText = $"Mostrando {Logs.Count} de {_allLogs.Count} registro(s)";
        }

        private void ClearFilters()
        {
            SelectedFilter = "Todos";
            SelectedCollectionFilter = "Todas";
            ApplyFilter();
        }
    }
}
