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
            set
            {
                if (SetProperty(ref _selectedBoardRoom, value))
                {
                    _ = LoadSelectedRoomStatusHistoryAsync();
                }
            }
        }

        private ObservableCollection<RoomStatusLogEntry> _selectedRoomStatusHistory = new();
        public ObservableCollection<RoomStatusLogEntry> SelectedRoomStatusHistory
        {
            get => _selectedRoomStatusHistory;
            set => SetProperty(ref _selectedRoomStatusHistory, value);
        }

        private string _boardSearchText = "";
        public string BoardSearchText
        {
            get => _boardSearchText;
            set
            {
                if (SetProperty(ref _boardSearchText, value))
                {
                    BoardRoomsView?.Refresh();
                }
            }
        }

        private string _boardStatusFilter = "";
        public string BoardStatusFilter
        {
            get => _boardStatusFilter;
            set
            {
                if (SetProperty(ref _boardStatusFilter, value))
                {
                    BoardRoomsView?.Refresh();
                }
            }
        }

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

            LoadStatusBoardCommand = new AsyncRelayCommand(LoadStatusBoardAsync);
            OpenChangeStatusCommand = new RelayCommand(param => OpenChangeStatus(param as RoomModel));

            BoardRoomsView = CollectionViewSource.GetDefaultView(BoardRooms);
            BoardRoomsView.Filter = BoardRoomFilter;

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
                var reviews = await Services.ReviewService.GetReviewsByRoomAsync(room.Id);
                if (reviews.Count > 0)
                {
                    room.AverageRating = reviews.Average(r => r.Rating);
                }
            }

            Rooms = new ObservableCollection<RoomModel>(response.Items);
            StatusText = $"Resultados: {Rooms.Count}";
        }

        private async Task LoadStatusBoardAsync()
        {
            var items = await RoomService.GetRoomsStatusBoardAsync();
            if (items == null)
            {
                return;
            }
            BoardRooms = new ObservableCollection<RoomModel>(items);
        }

        private bool BoardRoomFilter(object obj)
        {
            if (obj is not RoomModel r) return false;

            var statusOk = string.IsNullOrWhiteSpace(BoardStatusFilter) ||
                           string.Equals(r.Status ?? "", BoardStatusFilter, StringComparison.OrdinalIgnoreCase);

            if (!statusOk) return false;

            var q = (BoardSearchText ?? "").Trim();
            if (q.Length == 0) return true;

            return (r.RoomNumber ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                   (r.Type ?? "").Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        private void OpenChangeStatus(RoomModel? room)
        {
            if (room == null) return;
            SelectedBoardRoom = room;
            var win = new ChangeRoomStatusWindow(room);
            var ok = win.ShowDialog();
            if (ok == true)
            {
                _ = LoadStatusBoardAsync();
            }
        }

        private async Task LoadSelectedRoomStatusHistoryAsync()
        {
            if (SelectedBoardRoom == null)
            {
                SelectedRoomStatusHistory = new ObservableCollection<RoomStatusLogEntry>();
                return;
            }

            var items = await RoomService.GetRoomStatusLogAsync(SelectedBoardRoom.Id);
            if (items == null)
            {
                SelectedRoomStatusHistory = new ObservableCollection<RoomStatusLogEntry>();
                return;
            }
            SelectedRoomStatusHistory = new ObservableCollection<RoomStatusLogEntry>(items);
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
