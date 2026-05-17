using desktop_app.Commands;
using desktop_app.Models;
using desktop_app.Services;
using desktop_app.ViewModels;
using desktop_app.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Data;

namespace desktop_app.ViewModels.Room
{
    /// <summary>
    /// ViewModel para la vista de lista de habitaciones.
    /// </summary>
    public class RoomViewModel : ViewModelBase
    {
        public class StatusOption
        {
            public string Value { get; set; } = "";
            public string Label { get; set; } = "";
        }
        #region Propiedades

        /// <summary>Colección de habitaciones mostradas.</summary>
        private ObservableCollection<RoomModel> _rooms = new();
        public ObservableCollection<RoomModel> Rooms
        {
            get => _rooms;
            set => SetProperty(ref _rooms, value);
        }

        /// <summary>Texto de estado (resultados, cargando, error).</summary>
        private string _statusText = "";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>Habitación seleccionada.</summary>
        private RoomModel? _selectedRoom;
        public RoomModel? SelectedRoom
        {
            get => _selectedRoom;
            set => SetProperty(ref _selectedRoom, value);
        }

        private RoomsFilter _lastFilter = new RoomsFilter();

        #endregion

        #region Tablero (Status board)

        private ObservableCollection<RoomModel> _boardRooms = new();
        public ObservableCollection<RoomModel> BoardRooms
        {
            get => _boardRooms;
            set
            {
                if (SetProperty(ref _boardRooms, value))
                {
                    BoardRoomsView = CollectionViewSource.GetDefaultView(_boardRooms);
                    BoardRoomsView.Filter = BoardRoomFilter;
                    BoardRoomsView.Refresh();
                }
            }
        }

        private ICollectionView? _boardRoomsView;
        public ICollectionView? BoardRoomsView
        {
            get => _boardRoomsView;
            private set => SetProperty(ref _boardRoomsView, value);
        }

        private RoomModel? _selectedBoardRoom;
        public RoomModel? SelectedBoardRoom
        {
            get => _selectedBoardRoom;
            set => SetProperty(ref _selectedBoardRoom, value);
        }

        /// <summary>Valores del formulario; el filtrado usa los aplicados al pulsar Buscar.</summary>
        private string _boardSearchText = "";
        public string BoardSearchText
        {
            get => _boardSearchText;
            set => SetProperty(ref _boardSearchText, value);
        }

        private string _boardStatusFilter = "";
        public string BoardStatusFilter
        {
            get => _boardStatusFilter;
            set => SetProperty(ref _boardStatusFilter, value);
        }

        /// <summary>Criterios activos en la tabla (se actualizan solo con Buscar en el tablero).</summary>
        private string _appliedBoardSearchText = "";
        private string _appliedBoardStatusFilter = "";

        public List<StatusOption> RoomStatusOptions { get; } = new()
        {
            new StatusOption{ Value = "", Label = "Todos" },
            new StatusOption{ Value = "available", Label = "Disponible" },
            new StatusOption{ Value = "occupied", Label = "Ocupada" },
            new StatusOption{ Value = "cleaning", Label = "Limpieza" },
            new StatusOption{ Value = "maintenance", Label = "Mantenimiento" },
            new StatusOption{ Value = "blocked", Label = "Bloqueada" }
        };

        public AsyncRelayCommand LoadStatusBoardCommand { get; }
        public RelayCommand OpenChangeStatusCommand { get; }

        #endregion

        #region Incidencias

        private ObservableCollection<IncidentModel> _incidents = new();
        public ObservableCollection<IncidentModel> Incidents
        {
            get => _incidents;
            set => SetProperty(ref _incidents, value);
        }

        private IncidentModel? _selectedIncident;
        public IncidentModel? SelectedIncident
        {
            get => _selectedIncident;
            set
            {
                if (SetProperty(ref _selectedIncident, value))
                {
                    _ = LoadIncidentDetailAsync();
                }
            }
        }

        private IncidentModel? _incidentDetail;
        public IncidentModel? IncidentDetail
        {
            get => _incidentDetail;
            set => SetProperty(ref _incidentDetail, value);
        }

        private ObservableCollection<RoomStatusLogEntry> _incidentRoomStatusHistory = new();
        public ObservableCollection<RoomStatusLogEntry> IncidentRoomStatusHistory
        {
            get => _incidentRoomStatusHistory;
            set => SetProperty(ref _incidentRoomStatusHistory, value);
        }

        private string _incidentSeverityFilter = "";
        public string IncidentSeverityFilter
        {
            get => _incidentSeverityFilter;
            set => SetProperty(ref _incidentSeverityFilter, value);
        }

        private string _incidentStatusFilter = "";
        public string IncidentStatusFilter
        {
            get => _incidentStatusFilter;
            set => SetProperty(ref _incidentStatusFilter, value);
        }

        private string _incidentRoomFilter = "";
        public string IncidentRoomFilter
        {
            get => _incidentRoomFilter;
            set => SetProperty(ref _incidentRoomFilter, value);
        }

        private DateTime? _incidentFilterFromDate;
        public DateTime? IncidentFilterFromDate
        {
            get => _incidentFilterFromDate;
            set => SetProperty(ref _incidentFilterFromDate, value);
        }

        private DateTime? _incidentFilterToDate;
        public DateTime? IncidentFilterToDate
        {
            get => _incidentFilterToDate;
            set => SetProperty(ref _incidentFilterToDate, value);
        }

        public List<StatusOption> IncidentSeverityOptions { get; } = new()
        {
            new StatusOption{ Value = "", Label = "Todas" },
            new StatusOption{ Value = "low", Label = "Baja" },
            new StatusOption{ Value = "medium", Label = "Media" },
            new StatusOption{ Value = "high", Label = "Alta" }
        };

        public List<StatusOption> IncidentStatusOptions { get; } = new()
        {
            new StatusOption{ Value = "", Label = "Todos" },
            new StatusOption{ Value = "open", Label = "Abiertas" },
            new StatusOption{ Value = "in_progress", Label = "En proceso" },
            new StatusOption{ Value = "resolved", Label = "Resueltas" }
        };

        private string _assignEmployeeId = "";
        public string AssignEmployeeId
        {
            get => _assignEmployeeId;
            set => SetProperty(ref _assignEmployeeId, value);
        }

        private string _newInternalNote = "";
        public string NewInternalNote
        {
            get => _newInternalNote;
            set => SetProperty(ref _newInternalNote, value);
        }

        private string _incidentsStatusText = "";
        public string IncidentsStatusText
        {
            get => _incidentsStatusText;
            set => SetProperty(ref _incidentsStatusText, value);
        }

        private int _openLowCount;
        public int OpenLowCount { get => _openLowCount; set => SetProperty(ref _openLowCount, value); }
        private int _openMediumCount;
        public int OpenMediumCount { get => _openMediumCount; set => SetProperty(ref _openMediumCount, value); }
        private int _openHighCount;
        public int OpenHighCount { get => _openHighCount; set => SetProperty(ref _openHighCount, value); }
        private int _affectedRoomsCount;
        public int AffectedRoomsCount { get => _affectedRoomsCount; set => SetProperty(ref _affectedRoomsCount, value); }

        private string _incidentSummaryText = "";
        public string IncidentSummaryText
        {
            get => _incidentSummaryText;
            set => SetProperty(ref _incidentSummaryText, value);
        }

        public AsyncRelayCommand LoadIncidentsCommand { get; }
        public RelayCommand ClearIncidentFiltersCommand { get; }
        public AsyncRelayCommand ResolveIncidentCommand { get; }
        public AsyncRelayCommand AssignIncidentCommand { get; }
        public AsyncRelayCommand AddIncidentNoteCommand { get; }
        public RelayCommand OpenIncidentDetailCommand { get; }

        #endregion

        #region Propiedades de Filtro

        private string _type = "";
        /// <summary>Tipo de habitación para filtrar.</summary>
        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value == "Todos" ? "" : value);
        }

        private bool _onlyAvailable;
        /// <summary>Filtrar solo disponibles.</summary>
        public bool OnlyAvailable
        {
            get => _onlyAvailable;
            set => SetProperty(ref _onlyAvailable, value);
        }

        private string _guestsText = "";
        /// <summary>Número de huéspedes (texto).</summary>
        public string GuestsText
        {
            get => _guestsText;
            set => SetProperty(ref _guestsText, value);
        }

        private string _minPriceText = "";
        /// <summary>Precio mínimo (texto).</summary>
        public string MinPriceText
        {
            get => _minPriceText;
            set => SetProperty(ref _minPriceText, value);
        }

        private string _maxPriceText = "";
        /// <summary>Precio máximo (texto).</summary>
        public string MaxPriceText
        {
            get => _maxPriceText;
            set => SetProperty(ref _maxPriceText, value);
        }

        private bool _hasExtraBed;
        /// <summary>Filtrar con cama extra.</summary>
        public bool HasExtraBed
        {
            get => _hasExtraBed;
            set => SetProperty(ref _hasExtraBed, value);
        }

        private bool _hasCrib;
        /// <summary>Filtrar con cuna.</summary>
        public bool HasCrib
        {
            get => _hasCrib;
            set => SetProperty(ref _hasCrib, value);
        }

        private bool _hasOffer;
        /// <summary>Filtrar con oferta.</summary>
        public bool HasOffer
        {
            get => _hasOffer;
            set => SetProperty(ref _hasOffer, value);
        }

        private string _extrasText = "";
        /// <summary>Extras a buscar (CSV).</summary>
        public string ExtrasText
        {
            get => _extrasText;
            set => SetProperty(ref _extrasText, value);
        }

        private string _sortBy = "roomNumber";
        /// <summary>Campo de ordenamiento.</summary>
        public string SortBy
        {
            get => _sortBy;
            set => SetProperty(ref _sortBy, value);
        }

        private string _sortOrder = "asc";
        /// <summary>Dirección de ordenamiento.</summary>
        public string SortOrder
        {
            get => _sortOrder;
            set => SetProperty(ref _sortOrder, value);
        }

        #endregion

        #region Commands

        /// <summary>Comando para buscar habitaciones.</summary>
        public AsyncRelayCommand SearchCommand { get; }

        /// <summary>Comando para refrescar la lista.</summary>
        public AsyncRelayCommand RefreshCommand { get; }

        /// <summary>Comando para limpiar filtros.</summary>
        public RelayCommand ClearFiltersCommand { get; }

        /// <summary>Comando para eliminar una habitación.</summary>
        public RelayCommand DeleteRoomCommand { get; }

        /// <summary>Comando para ir a crear habitación.</summary>
        public ICommand GoCreateRoomCommand { get; }

        /// <summary>Comando para ir a editar habitación.</summary>
        public ICommand GoUpdateRoomCommand { get; }

        /// <summary>Comando para volver a la lista.</summary>
        public ICommand BackToRoomsCommand { get; }

        /// <summary>Comando para ver las reseñas de una habitación.</summary>
        public ICommand ShowReviewsCommand { get; }

        #endregion

        /// <summary>
        /// Inicializa el ViewModel y carga datos iniciales.
        /// </summary>
        public RoomViewModel()
        {
            SearchCommand = new AsyncRelayCommand(SearchAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            DeleteRoomCommand = new RelayCommand(async param => await DeleteRoomAsync(param as RoomModel));
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());

            LoadStatusBoardCommand = new AsyncRelayCommand(ApplyBoardFiltersAndReloadAsync);
            OpenChangeStatusCommand = new RelayCommand(param => OpenChangeStatus(param as RoomModel));

            BoardRoomsView = CollectionViewSource.GetDefaultView(BoardRooms);
            BoardRoomsView.Filter = BoardRoomFilter;

            LoadIncidentsCommand = new AsyncRelayCommand(LoadIncidentsAsync);
            ClearIncidentFiltersCommand = new RelayCommand(_ => ClearIncidentFilters());
            ResolveIncidentCommand = new AsyncRelayCommand(ResolveIncidentAsync);
            AssignIncidentCommand = new AsyncRelayCommand(AssignIncidentAsync);
            AddIncidentNoteCommand = new AsyncRelayCommand(AddIncidentNoteAsync);
            OpenIncidentDetailCommand = new RelayCommand(param => OpenIncidentDetail(param as IncidentModel));

            GoCreateRoomCommand = new RelayCommand(_ =>
                NavigationService.Instance.NavigateTo<CreateRoomWindow>());

            GoUpdateRoomCommand = new RelayCommand(param =>
            {
                if (param is not RoomModel room) return;
                NavigationService.Instance.NavigateTo<UpdateRoomWindow>();
                UpdateRoomViewModel.Instance.Room = room;
            });

            BackToRoomsCommand = new RelayCommand(_ =>
                NavigationService.Instance.NavigateTo<RoomView>());

            ShowReviewsCommand = new RelayCommand(param =>
            {
                if (param is not RoomModel room) return;
                NavigationService.Instance.NavigateTo(() => new ReviewsView(room));
            });

            _ = LoadInitialAsync();
            _ = LoadStatusBoardAsync();
            _ = LoadIncidentsAsync();
        }

        #region Métodos Privados

        /// <summary>
        /// Elimina una habitación tras confirmación.
        /// </summary>
        /// <param name="room">Habitación a eliminar.</param>
        private async Task DeleteRoomAsync(RoomModel? room)
        {
            if (room == null) return;

            var confirm = MessageBox.Show(
                $"¿Eliminar la habitación {room.RoomNumber}?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            var ok = await RoomService.DeleteRoomAsync(room.Id);

            if (!ok)
            {
                MessageBox.Show("No se pudo eliminar la habitación (API).");
                return;
            }

            Rooms.Remove(room);
            StatusText = $"Resultados: {Rooms.Count}";
        }

        private async Task LoadInitialAsync()
        {
            _lastFilter = new RoomsFilter();
            await LoadRoomsAsync(_lastFilter);
        }

        private async Task SearchAsync()
        {
            try
            {
                var filter = BuildFilterFromUi();
                _lastFilter = filter;
                await LoadRoomsAsync(filter);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RefreshAsync()
        {
            await LoadRoomsAsync(_lastFilter);
            await LoadIncidentsAsync();
            await LoadStatusBoardAsync();
        }

        private void ClearFilters()
        {
            Type = "";
            OnlyAvailable = false;
            GuestsText = "";
            MinPriceText = "";
            MaxPriceText = "";
            HasExtraBed = false;
            HasCrib = false;
            HasOffer = false;
            ExtrasText = "";
            SortBy = "roomNumber";
            SortOrder = "asc";
            _lastFilter = new RoomsFilter();
        }

        private async Task LoadRoomsAsync(RoomsFilter filter)
        {
            StatusText = "Cargando habitaciones...";

            var response = await RoomService.GetRoomsFilteredAsync(filter);

            if (response == null)
            {
                StatusText = "Error conectando con la API.";
                MessageBox.Show("No se pudo conectar con la API.");
                return;
            }

            // Cargar media de reseñas para cada habitación
            foreach (var room in response.Items)
            {
                room.ApplyStatusFromApi();
                var reviews = await Services.ReviewService.GetReviewsByRoomAsync(room.Id);
                if (reviews.Count > 0)
                {
                    room.AverageRating = reviews.Average(r => r.Rating);
                }
            }

            Rooms = new ObservableCollection<RoomModel>(response.Items);
            StatusText = $"Resultados: {Rooms.Count}";
        }

        /// <summary>Aplica estado y texto del tablero y recarga datos desde la API.</summary>
        private async Task ApplyBoardFiltersAndReloadAsync()
        {
            _appliedBoardSearchText = (BoardSearchText ?? "").Trim();
            _appliedBoardStatusFilter = BoardStatusFilter ?? "";
            await LoadStatusBoardAsync();
        }

        private async Task LoadStatusBoardAsync()
        {
            var items = await RoomService.GetRoomsStatusBoardAsync();
            if (items == null)
            {
                return;
            }

            foreach (var room in items)
                room.ApplyStatusFromApi();

            BoardRooms = new ObservableCollection<RoomModel>(items);
        }

        private bool BoardRoomFilter(object obj)
        {
            if (obj is not RoomModel r) return false;

            var statusOk = string.IsNullOrWhiteSpace(_appliedBoardStatusFilter) ||
                           string.Equals(r.Status ?? "", _appliedBoardStatusFilter, StringComparison.OrdinalIgnoreCase);

            if (!statusOk) return false;

            var q = _appliedBoardSearchText;
            if (q.Length == 0) return true;

            return (r.RoomNumber ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                   (r.Type ?? "").Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        private void OpenChangeStatus(RoomModel? room)
        {
            if (room == null) return;
            SelectedBoardRoom = room;
            NavigationService.Instance.NavigateTo(() => new ChangeRoomStatusView(room));
        }

        private static async Task<RoomModel?> FindRoomByRoomNumberTextAsync(string roomNumberText)
        {
            var want = roomNumberText.Trim();
            if (string.IsNullOrWhiteSpace(want)) return null;

            var filtered = await RoomService.GetRoomsFilteredAsync(new RoomsFilter { RoomNumber = want });
            if (filtered?.Items is { Count: > 0 } list)
            {
                var exact = list.FirstOrDefault(r => string.Equals(r.RoomNumber?.Trim(), want, StringComparison.OrdinalIgnoreCase));
                return exact ?? list[0];
            }

            var board = await RoomService.GetRoomsStatusBoardAsync();
            if (board == null) return null;
            return board.FirstOrDefault(r => string.Equals(r.RoomNumber?.Trim(), want, StringComparison.OrdinalIgnoreCase));
        }

        private async Task LoadIncidentsAsync()
        {
            IncidentsStatusText = "Cargando incidencias...";

            if (IncidentFilterFromDate.HasValue && IncidentFilterToDate.HasValue &&
                IncidentFilterFromDate.Value.Date > IncidentFilterToDate.Value.Date)
            {
                IncidentsStatusText = "La fecha «desde» no puede ser posterior a «hasta»";
                return;
            }

            string? roomIdParam = null;
            var roomFilterInput = (IncidentRoomFilter ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(roomFilterInput))
            {
                var room = await FindRoomByRoomNumberTextAsync(roomFilterInput);
                if (room == null)
                {
                    MessageBox.Show(
                        $"No existe habitación con el número «{roomFilterInput}».",
                        "Incidencias",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Incidents = new ObservableCollection<IncidentModel>();
                    IncidentsStatusText = "Número de habitación no encontrado.";
                    IncidentSummaryText = "Abiertas baja 0 · media 0 · alta 0 · afectadas 0";
                    return;
                }

                roomIdParam = room.Id;
            }

            var rawStatusFilter = (IncidentStatusFilter ?? "").Trim();
            var statusForApi = rawStatusFilter switch
            {
                "" => null,
                "in_progress" => "open",
                _ => rawStatusFilter
            };

            var items = await IncidentService.GetIncidentsAsync(
                severity: IncidentSeverityFilter,
                status: statusForApi,
                roomId: roomIdParam,
                from: IncidentFilterFromDate,
                to: IncidentFilterToDate
            );
            if (items == null)
            {
                IncidentsStatusText = "Error conectando con la API.";
                Incidents = new ObservableCollection<IncidentModel>();
                IncidentSummaryText = "Abiertas baja 0 · media 0 · alta 0 · afectadas 0";
                return;
            }

            foreach (var it in items)
            {
                if (string.IsNullOrWhiteSpace(it.AssignedTo) && !string.IsNullOrWhiteSpace(it.AssignedToCamel))
                    it.AssignedTo = it.AssignedToCamel;
            }

            if (string.Equals(rawStatusFilter, "in_progress", StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(i => i.HasAssignee).ToList();
            }

            items = ApplyIncidentDateFilter(items, IncidentFilterFromDate, IncidentFilterToDate);

            // Map room_id -> roomNumber for display
            var distinctRoomIds = items
                .Select(i => !string.IsNullOrWhiteSpace(i.RoomId) ? i.RoomId : i.RoomIdCamel)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            var map = new Dictionary<string, string>();
            foreach (var rid in distinctRoomIds)
            {
                var r = await RoomService.GetRoomByIdAsync(rid!);
                if (r != null && !string.IsNullOrWhiteSpace(r.RoomNumber))
                {
                    map[rid!] = r.RoomNumber;
                }
            }

            foreach (var it in items)
            {
                var rk = !string.IsNullOrWhiteSpace(it.RoomId) ? it.RoomId : it.RoomIdCamel;
                if (!string.IsNullOrWhiteSpace(rk) && map.TryGetValue(rk.Trim(), out var num))
                    it.RoomNumber = num;
            }

            var assigneeIds = items
                .Select(i => i.AssignedTo)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())
                .Distinct()
                .ToList();
            var assigneeNames = new Dictionary<string, string>();
            foreach (var aid in assigneeIds)
            {
                try
                {
                    var u = await UserService.GetClientByIdAsync(aid);
                    if (u != null && !string.IsNullOrWhiteSpace(u.Id))
                    {
                        var name = $"{u.FirstName} {u.LastName}".Trim();
                        assigneeNames[aid] = !string.IsNullOrWhiteSpace(name)
                            ? name
                            : (!string.IsNullOrWhiteSpace(u.Dni) ? u.Dni : u.Id);
                    }
                    else
                    {
                        assigneeNames[aid] = aid;
                    }
                }
                catch
                {
                    assigneeNames[aid] = aid;
                }
            }

            foreach (var it in items)
            {
                if (string.IsNullOrWhiteSpace(it.AssignedTo))
                    it.AssignedToDisplay = "Sin asignar";
                else if (assigneeNames.TryGetValue(it.AssignedTo.Trim(), out var nm))
                    it.AssignedToDisplay = nm;
                else
                    it.AssignedToDisplay = it.AssignedTo.Trim();
            }

            Incidents = new ObservableCollection<IncidentModel>(items);
            IncidentsStatusText = $"Resultados: {Incidents.Count}";

            var open = items.Where(i => i.Status == "open").ToList();
            OpenLowCount = open.Count(i => i.Severity == "low");
            OpenMediumCount = open.Count(i => i.Severity == "medium");
            OpenHighCount = open.Count(i => i.Severity == "high");
            AffectedRoomsCount = open
                .Select(i => !string.IsNullOrWhiteSpace(i.RoomId) ? i.RoomId : i.RoomIdCamel)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            IncidentSummaryText =
                $"Abiertas baja {OpenLowCount} · media {OpenMediumCount} · alta {OpenHighCount} · afectadas {AffectedRoomsCount}";
        }

        private void ClearIncidentFilters()
        {
            IncidentSeverityFilter = "";
            IncidentStatusFilter = "";
            IncidentRoomFilter = "";
            IncidentFilterFromDate = null;
            IncidentFilterToDate = null;
            _ = LoadIncidentsAsync();
        }

        private static List<IncidentModel> ApplyIncidentDateFilter(
            IEnumerable<IncidentModel> items,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = items;

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(i => IncidentReportDay(i) >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date;
                query = query.Where(i => IncidentReportDay(i) <= to);
            }

            return query.ToList();
        }

        private static DateTime IncidentReportDay(IncidentModel incident)
        {
            var dt = incident.ReportedAt;
            return (dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt).Date;
        }

        private void OpenIncidentDetail(IncidentModel? incident)
        {
            if (incident == null) return;
            NavigationService.Instance.NavigateTo(() => new IncidentDetailWindow(incident.Id, () => _ = LoadIncidentsAsync()));
        }

        private async Task LoadIncidentDetailAsync()
        {
            IncidentDetail = null;
            IncidentRoomStatusHistory = new ObservableCollection<RoomStatusLogEntry>();
            AssignEmployeeId = "";
            NewInternalNote = "";

            if (SelectedIncident == null) return;
            var detail = await IncidentService.GetIncidentByIdAsync(SelectedIncident.Id);
            if (detail == null) return;
            IncidentDetail = detail;
            AssignEmployeeId = detail.AssignedTo ?? "";

            if (!string.IsNullOrWhiteSpace(detail.RoomId))
            {
                var hist = await RoomService.GetRoomStatusLogAsync(detail.RoomId);
                if (hist != null)
                {
                    IncidentRoomStatusHistory = new ObservableCollection<RoomStatusLogEntry>(hist);
                }
            }
        }

        private async Task ResolveIncidentAsync()
        {
            if (SelectedIncident == null) return;
            var ok = await IncidentService.ResolveAsync(SelectedIncident.Id);
            if (!ok)
            {
                MessageBox.Show("No se pudo resolver incidencia (API).");
                return;
            }
            await LoadIncidentsAsync();
            await LoadIncidentDetailAsync();
        }

        private async Task AssignIncidentAsync()
        {
            if (SelectedIncident == null) return;
            if (string.IsNullOrWhiteSpace(AssignEmployeeId))
            {
                MessageBox.Show("employeeId requerido.");
                return;
            }
            var ok = await IncidentService.AssignAsync(SelectedIncident.Id, AssignEmployeeId.Trim());
            if (!ok)
            {
                MessageBox.Show("No se pudo asignar empleado (API).");
                return;
            }
            await LoadIncidentDetailAsync();
        }

        private async Task AddIncidentNoteAsync()
        {
            if (SelectedIncident == null) return;
            if (string.IsNullOrWhiteSpace(NewInternalNote))
            {
                MessageBox.Show("Nota requerida.");
                return;
            }
            var ok = await IncidentService.AddNoteAsync(SelectedIncident.Id, NewInternalNote.Trim());
            if (!ok)
            {
                MessageBox.Show("No se pudo guardar nota (API).");
                return;
            }
            NewInternalNote = "";
            await LoadIncidentDetailAsync();
        }

        /// <summary>
        /// Construye el filtro desde los valores de la UI.
        /// </summary>
        /// <returns>Filtro configurado.</returns>
        /// <exception cref="Exception">Si algún campo tiene formato inválido.</exception>
        private RoomsFilter BuildFilterFromUi()
        {
            var f = new RoomsFilter();

            if (!string.IsNullOrWhiteSpace(Type))
                f.Type = Type;

            if (OnlyAvailable)
                f.IsAvailable = true;

            if (!string.IsNullOrWhiteSpace(GuestsText))
            {
                if (int.TryParse(GuestsText, out var g) && g > 0)
                    f.Guests = g;
                else
                    throw new Exception("Guests debe ser un entero válido.");
            }

            if (!string.IsNullOrWhiteSpace(MinPriceText))
            {
                if (decimal.TryParse(MinPriceText, NumberStyles.Number, CultureInfo.CurrentCulture, out var min))
                    f.MinPrice = min;
                else
                    throw new Exception("MinPrice no es válido.");
            }

            if (!string.IsNullOrWhiteSpace(MaxPriceText))
            {
                if (decimal.TryParse(MaxPriceText, NumberStyles.Number, CultureInfo.CurrentCulture, out var max))
                    f.MaxPrice = max;
                else
                    throw new Exception("MaxPrice no es válido.");
            }

            if (HasExtraBed) f.HasExtraBed = true;
            if (HasCrib) f.HasCrib = true;
            if (HasOffer) f.HasOffer = true;

            if (!string.IsNullOrWhiteSpace(ExtrasText))
            {
                var extras = ExtrasText
                    .Split(',')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                if (extras.Count > 0)
                    f.Extras = extras;
            }

            if (!string.IsNullOrWhiteSpace(SortBy))
                f.SortBy = SortBy;

            if (!string.IsNullOrWhiteSpace(SortOrder))
                f.SortOrder = SortOrder;

            return f;
        }

        #endregion
    }
}
