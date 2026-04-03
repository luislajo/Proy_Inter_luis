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
            }
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
        /// Comando para emular el checkout/pago y generar factura en el backend.
        /// </summary>
        public ICommand CheckoutPayCommand { get; }

        public ICommand DownloadInvoiceCommand { get; }


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
            CheckoutPayCommand = new RelayCommand(async _ => await CheckoutPay());
            DownloadInvoiceCommand = new RelayCommand(async _ => await DownloadInvoice());

            LoadClients();
            LoadRooms();
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

        private async Task DownloadInvoice()
        {
            if (string.IsNullOrEmpty(Booking.Id))
            {
                MessageBox.Show("Primero guarda la reserva antes de descargar la factura.",
                    "Factura no disponible", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var tempPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"factura_{Booking.Id}.pdf");

                await BookingService.DownloadInvoicePdfAsync(Booking.Id, tempPath);

                var psi = new System.Diagnostics.ProcessStartInfo(tempPath)
                {
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error al descargar la factura",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Emula el pago: llama al backend y genera invoice_number.
        /// Luego (opcionalmente) permite descargar la factura.
        /// </summary>
        private async Task CheckoutPay()
        {
            if (string.IsNullOrEmpty(Booking.Id))
            {
                MessageBox.Show("Guarda la reserva antes de pagar.",
                    "Pago no disponible", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("¿Deseas realizar el pago y generar la factura?", "Confirmar pago",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var updated = await BookingService.PayBookingAsync(Booking.Id);
                if (updated == null)
                {
                    MessageBox.Show("No se pudo registrar el pago.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Actualizamos sólo lo necesario para la UI del formulario.
                PayDate = updated.PayDate;
                if (updated.TotalPrice != 0) TotalPrice = updated.TotalPrice;
                if (updated.TotalNights != 0) TotalNights = updated.TotalNights;
                if (updated.PricePerNight.HasValue) PricePerNight = updated.PricePerNight.Value;
                if (updated.Offer.HasValue) Offer = updated.Offer.Value;

                var openInvoice = MessageBox.Show("Pago realizado correctamente. ¿Descargar factura ahora?",
                    "Factura lista", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (openInvoice == MessageBoxResult.Yes)
                {
                    await DownloadInvoice();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error al pagar",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        
    }
}
