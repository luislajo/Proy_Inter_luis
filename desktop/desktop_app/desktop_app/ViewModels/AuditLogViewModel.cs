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

        private DateTime? _filterFromDate;
        public DateTime? FilterFromDate
        {
            get => _filterFromDate;
            set => SetProperty(ref _filterFromDate, value);
        }

        private DateTime? _filterToDate;
        public DateTime? FilterToDate
        {
            get => _filterToDate;
            set => SetProperty(ref _filterToDate, value);
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
        public ICommand ViewAuditDetailCommand { get; }

        public AuditLogViewModel()
        {
            RefreshCommand = new AsyncRelayCommand(LoadLogsAsync);
            SearchCommand = new RelayCommand(_ => ApplyFilter());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            ViewAuditDetailCommand = new RelayCommand<AuditLogModel>(ViewAuditDetail);

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
            if (FilterFromDate.HasValue && FilterToDate.HasValue &&
                FilterFromDate.Value.Date > FilterToDate.Value.Date)
            {
                StatusText = "La fecha «desde» no puede ser posterior a «hasta»";
                return;
            }

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

            if (FilterFromDate.HasValue)
            {
                var from = FilterFromDate.Value.Date;
                filtered = filtered.Where(l => AuditFilterDay(l) >= from);
            }

            if (FilterToDate.HasValue)
            {
                var to = FilterToDate.Value.Date;
                filtered = filtered.Where(l => AuditFilterDay(l) <= to);
            }

            foreach (var log in filtered.OrderByDescending(l => l.Timestamp))
                Logs.Add(log);

            StatusText = $"Mostrando {Logs.Count} de {_allLogs.Count} registro(s)";
        }

        private void ClearFilters()
        {
            SelectedFilter = "Todos";
            SelectedCollectionFilter = "Todas";
            FilterFromDate = null;
            FilterToDate = null;
            ApplyFilter();
        }

        private static DateTime AuditFilterDay(AuditLogModel log)
        {
            var dt = log.Timestamp;
            return (dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt).Date;
        }

        private void ViewAuditDetail(AuditLogModel? log)
        {
            if (log == null) return;
            NavigationService.Instance.NavigateTo(() => new AuditDetailView(log));
        }
    }
}
