using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using desktop_app.Commands;
using desktop_app.Events;
using desktop_app.Models;
using desktop_app.Services;
using desktop_app.Views;

namespace desktop_app.ViewModels
{
    public class BookingViewModel : ViewModelBase
    {
        /// <summary>
        /// Colección de las reservas que se conecta a la tabla de View
        /// </summary>
        public ObservableCollection<BookingModel> Bookings { get; }
        private readonly List<BookingModel> _allBookings = new();

        public ObservableCollection<InvoiceModel> Invoices { get; } = new();

        private readonly List<InvoiceModel> _invoiceCache = new();

        // Filtros de reservas
        private string _filterClientText = string.Empty;
        public string FilterClientText
        {
            get => _filterClientText;
            set { _filterClientText = value; OnPropertyChanged(); }
        }

        private string _filterStatus = "Todas";
        public string FilterStatus
        {
            get => _filterStatus;
            set { _filterStatus = value; OnPropertyChanged(); }
        }

        public IReadOnlyList<string> BookingStatusOptions { get; } = new List<string> { "Todas", "Abierta", "Finalizada", "Cancelada" };

        public IReadOnlyList<string> InvoiceStatusFilterOptions { get; } = new List<string> { "Todas", "Abierta", "Finalizada", "Cancelada" };

        private DateTime? _filterFromDate;
        public DateTime? FilterFromDate
        {
            get => _filterFromDate;
            set { _filterFromDate = value; OnPropertyChanged(); }
        }

        private DateTime? _filterToDate;
        public DateTime? FilterToDate
        {
            get => _filterToDate;
            set { _filterToDate = value; OnPropertyChanged(); }
        }
        
        
        /// <summary>
        /// Comando para el botón de eliminar reserva
        /// </summary>
        public ICommand DeleteBookingCommand { get; }
        
        
        /// <summary>
        /// Comando para el botón de editar reserva
        /// </summary>
        public ICommand EditBookingCommand { get; }
        
        
        /// <summary>
        /// Comando para el botón de crear reserva
        /// </summary>
        public ICommand CreateBookingCommand { get; }
        
        
        /// <summary>
        /// Comando para el botón de recargar
        /// </summary>
        public ICommand ReloadBookingCommand { get; }

        public ICommand SearchBookingsCommand { get; }

        public ICommand ClearBookingFiltersCommand { get; }

        public ICommand LoadInvoicesCommand { get; }

        public ICommand SearchInvoicesCommand { get; }

        public ICommand ClearInvoiceFiltersCommand { get; }

        public ICommand DownloadInvoiceRowCommand { get; }


        private string _invoiceDocumentFilter = string.Empty;
        public string InvoiceDocumentFilter
        {
            get => _invoiceDocumentFilter;
            set
            {
                _invoiceDocumentFilter = value;
                OnPropertyChanged();
            }
        }

        private DateTime? _invoiceFilterFromDate;
        public DateTime? InvoiceFilterFromDate
        {
            get => _invoiceFilterFromDate;
            set => SetProperty(ref _invoiceFilterFromDate, value);
        }

        private DateTime? _invoiceFilterToDate;
        public DateTime? InvoiceFilterToDate
        {
            get => _invoiceFilterToDate;
            set => SetProperty(ref _invoiceFilterToDate, value);
        }

        private string _invoiceFilterStatus = "Todas";
        public string InvoiceFilterStatus
        {
            get => _invoiceFilterStatus;
            set => SetProperty(ref _invoiceFilterStatus, value);
        }

        private string _invoiceStatusText = "Cargando facturas...";
        public string InvoiceStatusText
        {
            get => _invoiceStatusText;
            set => SetProperty(ref _invoiceStatusText, value);
        }
        
        /// <summary>
        /// Constructor del ViewModel
        /// Se encarga de
        ///     - Cargar las reservas en la colección
        ///     - Crear los comandos
        ///     - Añadir el evento para volver a cargar
        /// </summary>
        public BookingViewModel()
        {
            Bookings = new ObservableCollection<BookingModel>();
            _ = LoadBookingsAsync();
            _ = LoadInvoicesAsync();
            DeleteBookingCommand = new AsyncRelayCommand<BookingModel>(DeleteBookingAsync);
            EditBookingCommand = new RelayCommand(EditBooking);
            CreateBookingCommand = new RelayCommand(CreateBooking);
            ReloadBookingCommand = new AsyncRelayCommand(LoadBookingsAsync);
            SearchBookingsCommand = new RelayCommand(_ => ApplyBookingFilters());
            ClearBookingFiltersCommand = new RelayCommand(_ => ClearBookingFilters());
            LoadInvoicesCommand = new AsyncRelayCommand(LoadInvoicesAsync);
            SearchInvoicesCommand = new RelayCommand(_ => SearchInvoices());
            ClearInvoiceFiltersCommand = new RelayCommand(_ => ClearInvoiceFilters());
            DownloadInvoiceRowCommand = new AsyncRelayCommand<InvoiceModel>(DownloadInvoiceRowAsync);
            BookingEvents.OnBookingChanged += async () => await LoadBookingsAsync();
        }

        
        /// <summary>
        /// Método que carga las reservas
        /// Obtiene todas las reservas y vacía la lista para evitar duplicados
        /// Obtiene los datos necesarios para mostrar la información en la UI
        /// Añade las reservas con los datos extra a la lista de reservas
        /// </summary>
        private async Task LoadBookingsAsync()
        {
            try
            {
                var list = await BookingService.GetAllBookingsAsync();
                _allBookings.Clear();
                foreach (var booking in list)
                {
                    UserModel u = await UserService.GetClientByIdAsync(booking.Client);
                    booking.ClientDni = u.Dni;
                    booking.ClientName = u.FirstName + " " + u.LastName;
                    RoomModel? room = await RoomService.GetRoomByIdAsync(booking.Room);
                    booking.RoomNumber = room != null ? room.RoomNumber : "Error";
                    _allBookings.Add(booking);
                }

                ApplyBookingFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        
        /// <summary>
        /// Método al que referencia el comando de eliminar
        /// Requiere de confirmación mediante MesageBox
        /// </summary>
        /// 
        /// <param name="booking">
        /// Recibe el modelo que debe eliminar
        /// </param>
        private async Task DeleteBookingAsync(BookingModel booking)
        {
            var result = MessageBox.Show("¿Seguro que quieres eliminar esta reserva?", "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                bool deleted = await BookingService.DeleteBooking(booking.Id);
                
                if (deleted)
                {
                    Bookings.Remove(booking);
                    _allBookings.RemoveAll(b => b.Id == booking.Id);
                }
                else
                {
                    MessageBox.Show("No se pudo eliminar la reserva", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        
        /// <summary>
        /// Método al que referencia el comando de editar
        /// </summary>
        /// 
        /// <param name="parameter">
        /// Parametro a editar que se revisa que debe ser una reserva
        /// </param>
        private void EditBooking(object? parameter)
        {
            if (parameter is not BookingModel booking) return;
            NavigationService.Instance.NavigateTo<FormBookingView>();
            FormBookingViewModel.Instance.Booking = booking.Clone();
        }

        
        /// <summary>
        /// Método al que hace referencia el comando de crear
        /// </summary>
        /// 
        /// <param name="parameter">
        /// Parametro necesario para los RelayCommand
        /// </param>
        private void CreateBooking(object? parameter)
        {
            NavigationService.Instance.NavigateTo<FormBookingView>();
            FormBookingViewModel.Instance.Booking = new BookingModel();
        }

        private void ClearBookingFilters()
        {
            FilterClientText = string.Empty;
            FilterStatus = "Todas";
            FilterFromDate = null;
            FilterToDate = null;
            ApplyBookingFilters();
        }

        private void ApplyBookingFilters()
        {
            IEnumerable<BookingModel> filtered = _allBookings;

            if (!string.IsNullOrWhiteSpace(FilterClientText))
            {
                var needle = FilterClientText.Trim();
                filtered = filtered.Where(b =>
                    (!string.IsNullOrWhiteSpace(b.ClientName) && b.ClientName.Contains(needle, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(b.ClientDni) && b.ClientDni.Contains(needle, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.Equals(FilterStatus, "Todas", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(b => string.Equals(b.Status, FilterStatus, StringComparison.OrdinalIgnoreCase));
            }

            if (FilterFromDate.HasValue)
            {
                var from = FilterFromDate.Value.Date;
                filtered = filtered.Where(b => b.CheckInDate.Date >= from);
            }

            if (FilterToDate.HasValue)
            {
                var to = FilterToDate.Value.Date;
                filtered = filtered.Where(b => b.CheckInDate.Date <= to);
            }

            Bookings.Clear();
            foreach (var booking in filtered.OrderByDescending(b => b.CheckInDate))
            {
                Bookings.Add(booking);
            }
        }

        private async Task LoadInvoicesAsync()
        {
            try
            {
                InvoiceStatusText = "Cargando facturas...";
                Invoices.Clear();
                _invoiceCache.Clear();

                var filter = string.IsNullOrWhiteSpace(InvoiceDocumentFilter)
                    ? null
                    : InvoiceDocumentFilter.Trim();
                var items = await InvoiceService.GetInvoicesAsync(filter);
                var clientCache = new Dictionary<string, UserModel>();
                foreach (var inv in items)
                {
                    NormalizeInvoiceDates(inv);
                    await EnrichInvoiceClientAsync(inv, clientCache);
                    _invoiceCache.Add(inv);
                }

                ApplyInvoiceFilters(filter);
            }
            catch (Exception ex)
            {
                InvoiceStatusText = "Error al cargar facturas.";
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void NormalizeInvoiceDates(InvoiceModel inv)
        {
            inv.CheckInDate = ToLocalDatePreserve(inv.CheckInDate);
            inv.CheckOutDate = ToLocalDatePreserve(inv.CheckOutDate);
            if (inv.InvoiceDate.HasValue)
                inv.InvoiceDate = ToLocalDatePreserve(inv.InvoiceDate.Value);
        }

        private static DateTime ToLocalDatePreserve(DateTime dt) =>
            dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;

        /// <summary>Fecha usada para filtrar: fecha de factura si existe; si no, fecha de entrada.</summary>
        private static DateTime InvoiceFilterDay(InvoiceModel inv)
        {
            var dt = inv.InvoiceDate ?? inv.CheckInDate;
            return dt.Date;
        }

        /// <param name="apiDocumentFilter">Texto DNI enviado a la API (null = todas).</param>
        private void SearchInvoices()
        {
            if (InvoiceFilterFromDate.HasValue && InvoiceFilterToDate.HasValue &&
                InvoiceFilterFromDate.Value.Date > InvoiceFilterToDate.Value.Date)
            {
                InvoiceStatusText = "La fecha «desde» no puede ser posterior a «hasta»";
                return;
            }

            if (_invoiceCache.Count > 0)
                ApplyInvoiceFilters();
            else
                _ = LoadInvoicesAsync();
        }

        private void ApplyInvoiceFilters(string? apiDocumentFilter = null)
        {
            apiDocumentFilter ??= string.IsNullOrWhiteSpace(InvoiceDocumentFilter)
                ? null
                : InvoiceDocumentFilter.Trim();

            if (InvoiceFilterFromDate.HasValue && InvoiceFilterToDate.HasValue &&
                InvoiceFilterFromDate.Value.Date > InvoiceFilterToDate.Value.Date)
            {
                InvoiceStatusText = "La fecha «desde» no puede ser posterior a «hasta»";
                return;
            }

            IEnumerable<InvoiceModel> query = _invoiceCache;

            if (InvoiceFilterFromDate.HasValue)
            {
                var from = InvoiceFilterFromDate.Value.Date;
                query = query.Where(i => InvoiceFilterDay(i) >= from);
            }

            if (InvoiceFilterToDate.HasValue)
            {
                var to = InvoiceFilterToDate.Value.Date;
                query = query.Where(i => InvoiceFilterDay(i) <= to);
            }

            if (!string.Equals(InvoiceFilterStatus, "Todas", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(i =>
                    string.Equals(i.Status?.Trim(), InvoiceFilterStatus, StringComparison.OrdinalIgnoreCase));
            }

            Invoices.Clear();
            foreach (var inv in query.OrderByDescending(InvoiceFilterDay))
                Invoices.Add(inv);

            if (_invoiceCache.Count == 0)
            {
                InvoiceStatusText = apiDocumentFilter == null
                    ? "No hay facturas en la base de datos."
                    : "Sin facturas para ese DNI/NIE.";
            }
            else if (Invoices.Count == 0)
            {
                InvoiceStatusText = "Ninguna factura coincide con los filtros aplicados.";
            }
            else
            {
                InvoiceStatusText = Invoices.Count == 1
                    ? "1 factura encontrada"
                    : $"{Invoices.Count} facturas encontradas";
            }
        }

        private static async Task EnrichInvoiceClientAsync(InvoiceModel inv, Dictionary<string, UserModel> cache)
        {
            if (string.IsNullOrWhiteSpace(inv.ClientId)) return;

            if (!cache.TryGetValue(inv.ClientId, out var user))
            {
                user = await UserService.GetClientByIdAsync(inv.ClientId);
                cache[inv.ClientId] = user;
            }

            inv.ClientDni = user.Dni ?? string.Empty;
            inv.ClientName = $"{user.FirstName} {user.LastName}".Trim();
        }

        private void ClearInvoiceFilters()
        {
            InvoiceDocumentFilter = string.Empty;
            InvoiceFilterFromDate = null;
            InvoiceFilterToDate = null;
            InvoiceFilterStatus = "Todas";
            if (_invoiceCache.Count > 0)
                ApplyInvoiceFilters();
            else
                _ = LoadInvoicesAsync();
        }

        private Task DownloadInvoiceRowAsync(InvoiceModel invoice)
        {
            if (string.IsNullOrWhiteSpace(invoice.Id))
                return Task.CompletedTask;

            NavigationService.Instance.NavigateTo(() => new InvoicePrepareView
            {
                DataContext = new InvoicePrepareViewModel(invoice.Id, returnToFormBooking: false)
            });
            return Task.CompletedTask;
        }

    }
}
