using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using desktop_app.Commands;
using desktop_app.Events;
using desktop_app.Models;
using desktop_app.Services;
using desktop_app.Views;

namespace desktop_app.ViewModels
{
    public class FormBookingViewModel : ViewModelBase
    {
        private static FormBookingViewModel? _instance;
        /// <summary>
        /// Obtiene la instancia única del ViewModel (patrón Singleton).
        /// </summary>
        public static FormBookingViewModel Instance => _instance ??= new FormBookingViewModel();

        /// <summary>
        /// Obtiene el título de la ventana basado en si la reserva es nueva o existente.
        /// </summary>
        public string Title => string.IsNullOrEmpty(Booking.Id) ? "Crear reserva" : "Actualizar reserva";

        /// <summary>
        /// Indica si los campos del formulario deben estar habilitados.
        /// Devuelve true para nuevas reservas, false para reservas existentes.
        /// </summary>
        public bool Enabled => string.IsNullOrEmpty(Booking.Id);
        
        /// <summary>
        /// Indica si los campos del formulario deben estar deshabilitados.
        /// </summary>
        public bool Disabled => !Enabled;


        private BookingModel _booking;
        /// <summary>
        /// Obtiene o establece el modelo de reserva actual.
        /// </summary>
        public BookingModel Booking
        {
            get => _booking;
            set
            {
                _booking = value;
                OnPropertyChanged();
                RefreshAll();
                RefreshStayUi();
                _ = LoadBookingStayStateAsync();
            }
        }

        private bool _showCheckInPanel;
        public bool ShowCheckInPanel
        {
            get => _showCheckInPanel;
            private set => SetProperty(ref _showCheckInPanel, value);
        }

        private bool _canRegisterCheckIn;
        public bool CanRegisterCheckIn
        {
            get => _canRegisterCheckIn;
            private set => SetProperty(ref _canRegisterCheckIn, value);
        }

        private string _checkInStatusText = "";
        public string CheckInStatusText
        {
            get => _checkInStatusText;
            private set => SetProperty(ref _checkInStatusText, value);
        }

        public string? DisplayCheckInCode => Booking.CheckInCode;

        private bool _showCheckOutPanel;
        public bool ShowCheckOutPanel
        {
            get => _showCheckOutPanel;
            private set => SetProperty(ref _showCheckOutPanel, value);
        }

        private bool _canRegisterCheckOut;
        public bool CanRegisterCheckOut
        {
            get => _canRegisterCheckOut;
            private set => SetProperty(ref _canRegisterCheckOut, value);
        }

        private string _checkOutStatusText = "";
        public string CheckOutStatusText
        {
            get => _checkOutStatusText;
            private set => SetProperty(ref _checkOutStatusText, value);
        }

        private ObservableCollection<UserModel> _clients = new();
        /// <summary>
        /// Colección observable de clientes disponibles para seleccionar en el formulario.
        /// </summary>
        public ObservableCollection<UserModel> Clients
        {
            get => _clients;
            set => SetProperty(ref _clients, value);
        }

        /// <summary>
        /// Carga asincrónicamente la lista de clientes desde el servicio de usuarios.
        /// </summary>
        private async void LoadClients()
        {
            Clients.Clear();
            var users = await UserService.GetAllUsersAsync();
            foreach (var user in users)
                if (user.Rol == "Usuario") Clients.Add(user);
        }

        /// <summary>
        /// Obtiene o establece el DNI del cliente asociado a la reserva.
        /// </summary>
        public string ClientDni
        {
            get => Booking.ClientDni;
            set
            {
                Booking.ClientDni = value;
                OnPropertyChanged();
            }
        }


        private ObservableCollection<RoomModel> _rooms = new();
        /// <summary>
        /// Colección observable de habitaciones disponibles para seleccionar en el formulario.
        /// </summary>
        public ObservableCollection<RoomModel> Rooms
        {
            get => _rooms;
            set => SetProperty(ref _rooms, value);
        }

        /// <summary>
        /// Carga asincrónicamente la lista de habitaciones filtradas desde el servicio de habitaciones.
        /// </summary>
        private async void LoadRooms()
        {
            Rooms.Clear();
            var rooms = (await RoomService.GetRoomsFilteredAsync()).Items;
            foreach (var room in rooms)
                Rooms.Add(room);
        }

        /// <summary>
        /// Obtiene o establece el número de habitación seleccionado.
        /// </summary>
        public string RoomNumber
        {
            get => Booking.RoomNumber;
            set
            {
                Booking.RoomNumber = value;
                ChangeRoomData(Booking.RoomNumber);
                OnPropertyChanged();
            }
        }

        public void ChangeRoomData(String roomNumber)
        {
            PricePerNight = _rooms.First(room => room.RoomNumber == roomNumber).PricePerNight ?? 0;
            Offer = _rooms.First(room => room.RoomNumber == roomNumber).Offer ?? 0;
        }

        public DateTime CheckInDate
        {
            get => Booking.CheckInDate;
            set
            {
                Booking.CheckInDate = value;
                OnPropertyChanged();
                RecalculateTotal();
            }
        }

        /// <summary>
        /// Obtiene o establece la fecha de fin de la reserva.
        /// </summary>
        public DateTime CheckOutDate
        {
            get => Booking.CheckOutDate;
            set
            {
                Booking.CheckOutDate = value;
                OnPropertyChanged();
                RecalculateTotal();
            }
        }

        /// <summary>
        /// Obtiene o establece la fecha de pago de la reserva.
        /// </summary>
        public DateTime PayDate
        {
            get => Booking.PayDate;
            set
            {
                Booking.PayDate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Obtiene o establece el precio por noche de la habitación seleccionada.
        /// </summary>
        public decimal PricePerNight
        {
            get => Booking.PricePerNight ?? 0;
            set
            {
                Booking.PricePerNight = value;
                OnPropertyChanged();
                RecalculateTotal();
            }
        }

        /// <summary>
        /// Obtiene o establece el porcentaje de oferta aplicado a la reserva.
        /// </summary>
        public decimal Offer
        {
            get => Booking.Offer ?? 0;
            set
            {
                Booking.Offer = value;
                OnPropertyChanged();
                RecalculateTotal();
            }
        }

        /// <summary>
        /// Obtiene el precio total calculado de la reserva.
        /// </summary>
        public decimal TotalPrice
        {
            get => Booking.TotalPrice;
            private set
            {
                Booking.TotalPrice = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Obtiene el número total de noches calculado de la reserva.
        /// </summary>
        public int TotalNights
        {
            get => Booking.TotalNights;
            private set
            {
                Booking.TotalNights = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Comando para guardar la reserva.
        /// </summary>
        public ICommand SaveCommand { get; }
        
        /// <summary>
        /// Comando para cancelar la reserva.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Comando para descargar o preparar factura.
        /// </summary>
        public ICommand DownloadInvoiceCommand { get; }

        public ICommand RegisterCheckInCommand { get; }

        public ICommand RegisterCheckOutCommand { get; }


        public ICommand ReturnCommand { get; } =
            new RelayCommand(_ =>
                NavigationService.Instance.NavigateTo<BookingView>());

        /// <summary>
        /// Constructor privado que inicializa una nueva instancia del ViewModel.
        /// Configura los comandos y carga los datos iniciales.
        /// </summary>
        private FormBookingViewModel()
        {
            Booking = new BookingModel();

            SaveCommand = new RelayCommand(async _ => await Save());
            CancelCommand = new RelayCommand(async _ => await Cancel());
            DownloadInvoiceCommand = new RelayCommand(_ => NavigateToInvoicePrepare());
            RegisterCheckInCommand = new RelayCommand(async _ => await RegisterCheckInAsync(), _ => CanRegisterCheckIn);
            RegisterCheckOutCommand = new RelayCommand(async _ => await RegisterCheckOutAsync(), _ => CanRegisterCheckOut);

            LoadClients();
            LoadRooms();
        }

        private async Task LoadBookingStayStateAsync()
        {
            if (string.IsNullOrEmpty(Booking.Id))
            {
                RefreshStayUi();
                return;
            }

            try
            {
                var fresh = await BookingService.GetBookingByIdAsync(Booking.Id);
                if (fresh == null) return;

                var roomNumber = Booking.RoomNumber;
                var clientName = Booking.ClientName;
                var clientDni = Booking.ClientDni;

                Booking.CheckInCode = fresh.CheckInCode;
                Booking.CheckedIn = fresh.CheckedIn;
                Booking.CanSubmitCheckIn = fresh.CanSubmitCheckIn;
                Booking.CheckInCodeSent = fresh.CheckInCodeSent;
                Booking.CheckedOut = fresh.CheckedOut;
                Booking.CanSubmitCheckOut = fresh.CanSubmitCheckOut;
                Booking.Status = fresh.Status;
                Booking.IsCheckInDayToday = fresh.IsCheckInDayToday;
                Booking.IsCheckOutDayToday = fresh.IsCheckOutDayToday;
                Booking.StayWindowOpen = fresh.StayWindowOpen;
                Booking.CheckInDate = fresh.CheckInDate;
                Booking.CheckOutDate = fresh.CheckOutDate;

                Booking.RoomNumber = roomNumber;
                Booking.ClientName = clientName;
                Booking.ClientDni = clientDni;

                RefreshStayUi();
                OnPropertyChanged(nameof(DisplayCheckInCode));
            }
            catch (Exception ex)
            {
                var msg = ex.Message.Contains("401") || ex.Message.Contains("Token", StringComparison.OrdinalIgnoreCase)
                    ? "Sesión expirada. Cierra sesión y vuelve a entrar."
                    : ex.Message;
                CheckInStatusText = msg;
                CheckOutStatusText = msg;
                CanRegisterCheckIn = false;
                CanRegisterCheckOut = false;
            }
        }

        private void RefreshStayUi()
        {
            RefreshCheckInUi();
            RefreshCheckOutUi();
        }

        private void RefreshCheckOutUi()
        {
            var existing = !string.IsNullOrEmpty(Booking.Id);
            var open = Booking.Status == "Abierta";
            ShowCheckOutPanel = existing && open && Booking.IsCheckOutDayToday;

            if (!ShowCheckOutPanel)
            {
                CheckOutStatusText = "";
                CanRegisterCheckOut = false;
                return;
            }

            if (Booking.CheckedOut)
            {
                CheckOutStatusText = "Check-out completado.";
                CanRegisterCheckOut = false;
                return;
            }

            if (!Booking.CheckedIn)
            {
                CheckOutStatusText = "El huésped debe hacer check-in antes del check-out.";
                CanRegisterCheckOut = false;
                return;
            }

            if (Booking.CanSubmitCheckOut)
            {
                CheckOutStatusText = "Puedes registrar la salida del huésped (check-out).";
                CanRegisterCheckOut = true;
                return;
            }

            if (!Booking.StayWindowOpen)
            {
                CheckOutStatusText = "Disponible a partir de las 11:00 (hora del hotel).";
                CanRegisterCheckOut = false;
                return;
            }

            CheckOutStatusText = "Check-out disponible hoy tras las 11:00.";
            CanRegisterCheckOut = false;
        }

        private async Task RegisterCheckOutAsync()
        {
            if (string.IsNullOrEmpty(Booking.Id)) return;

            try
            {
                var updated = await BookingService.VerifyCheckOutAsync(Booking.Id);
                if (updated == null)
                {
                    MessageBox.Show("No se pudo registrar el check-out.", "Check-out", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                Booking.CheckedOut = updated.CheckedOut;
                Booking.CanSubmitCheckOut = updated.CanSubmitCheckOut;
                Booking.Status = updated.Status;
                await LoadBookingStayStateAsync();

                MessageBox.Show("Check-out registrado correctamente.", "Check-out", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Check-out", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshCheckInUi()
        {
            var existing = !string.IsNullOrEmpty(Booking.Id);
            var open = Booking.Status == "Abierta";
            ShowCheckInPanel = existing && open && Booking.IsCheckInDayToday;

            if (!ShowCheckInPanel)
            {
                CheckInStatusText = "";
                CanRegisterCheckIn = false;
                return;
            }

            if (Booking.CheckedIn)
            {
                CheckInStatusText = "Check-in completado.";
                CanRegisterCheckIn = false;
                return;
            }

            if (Booking.CanSubmitCheckIn)
            {
                CheckInStatusText = string.IsNullOrEmpty(Booking.CheckInCode)
                    ? "Puedes registrar el check-in del huésped."
                    : $"Código: {Booking.CheckInCode} — pulsa Registrar check-in.";
                CanRegisterCheckIn = true;
                return;
            }

            if (!Booking.StayWindowOpen)
            {
                CheckInStatusText = "Disponible a partir de las 11:00 (hora del hotel).";
                CanRegisterCheckIn = false;
                return;
            }

            CheckInStatusText = "Esperando código de check-in para este día de entrada.";
            CanRegisterCheckIn = false;
        }

        private async Task RegisterCheckInAsync()
        {
            if (string.IsNullOrEmpty(Booking.Id)) return;

            try
            {
                var updated = await BookingService.VerifyCheckInAsync(Booking.Id, Booking.CheckInCode);
                if (updated == null)
                {
                    MessageBox.Show("No se pudo registrar el check-in.", "Check-in", MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                Booking.CheckedIn = updated.CheckedIn;
                Booking.CanSubmitCheckIn = updated.CanSubmitCheckIn;
                Booking.CheckInCode = updated.CheckInCode;
                Booking.Status = updated.Status;
                await LoadBookingStayStateAsync();
                OnPropertyChanged(nameof(DisplayCheckInCode));

                MessageBox.Show("Check-in registrado correctamente.", "Check-in", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Check-in", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Recalcula el precio total y el número de noches basado en las fechas seleccionadas.
        /// </summary>
        private void RecalculateTotal()
        {
            if (CheckOutDate <= CheckInDate)
            {
                TotalNights = 0;
                TotalPrice = 0;
                return;
            }

            TotalNights = (CheckOutDate.Date - CheckInDate.Date).Days;

            var basePrice = TotalNights * PricePerNight;
            var discount = basePrice * (Offer / 100m);

            TotalPrice = basePrice - discount;
        }
        
        /// <summary>
        /// Actualiza todas las propiedades del ViewModel notificando cambios en la UI.
        /// </summary>
        private void RefreshAll()
        {
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Enabled));
            OnPropertyChanged(nameof(Disabled));

            OnPropertyChanged(nameof(ClientDni));
            OnPropertyChanged(nameof(RoomNumber));

            OnPropertyChanged(nameof(CheckInDate));
            OnPropertyChanged(nameof(CheckOutDate));
            OnPropertyChanged(nameof(PayDate));

            OnPropertyChanged(nameof(PricePerNight));
            OnPropertyChanged(nameof(Offer));
            OnPropertyChanged(nameof(TotalPrice));
            OnPropertyChanged(nameof(TotalNights));
            OnPropertyChanged(nameof(DisplayCheckInCode));
            RefreshStayUi();
        }

        /// <summary>
        /// Guarda la reserva actual (crea una nueva o actualiza una existente).
        /// </summary>
        private async Task Save()
        {
            try
            {
                if (string.IsNullOrEmpty(Booking.Id))
                {
                    var userId = await UserService.GetUserIdByDniAsync(ClientDni);
                    Booking.Client = userId;

                    var filter = new RoomsFilter { RoomNumber = RoomNumber };
                    Booking.Room =
                        (await RoomService.GetRoomsFilteredAsync(filter))
                        ?.Items.First().Id;

                    await BookingService.CreateBookingAsync(Booking);
                }
                else
                {
                    await BookingService.UpdateBookingAsync(Booking);
                }
                await BookingEvents.RaiseBookingChanged();
                NavigationService.Instance.NavigateTo<BookingView>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Cancela la reserva actual después de la confirmación del usuario.
        /// </summary>
        private async Task Cancel()
        {
            if (Booking.Status == "Cancelada")
            {
                MessageBox.Show("Esta reserva ya está cancelada");
                return;
            }

            if (MessageBox.Show("¿Cancelar reserva?", "Confirmación", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                await BookingService.CancelBookingAsync(Booking.Id);
                await BookingEvents.RaiseBookingChanged();
                NavigationService.Instance.NavigateTo<BookingView>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Abre el panel de configuración de factura (encabezado, extras) antes de pagar o descargar.
        /// </summary>
        private void NavigateToInvoicePrepare()
        {
            if (string.IsNullOrEmpty(Booking.Id))
            {
                MessageBox.Show("Primero guarda la reserva antes de facturar o descargar.",
                    "Factura", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            NavigationService.Instance.NavigateTo(() => new InvoicePrepareView
            {
                DataContext = new InvoicePrepareViewModel(Booking.Id, returnToFormBooking: true)
            });
        }

        
    }
}
