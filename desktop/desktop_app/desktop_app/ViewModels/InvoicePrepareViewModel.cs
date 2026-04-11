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
    /// <summary>Fila editable de extra para la factura.</summary>
    public class InvoiceExtraRow : ViewModelBase
    {
        private bool _include = true;
        private string _name = "";
        private int _quantity = 1;
        private decimal _unitPrice = 15m;

        public bool Include
        {
            get => _include;
            set => SetProperty(ref _include, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetProperty(ref _unitPrice, value);
        }
    }

    /// <summary>
    /// Configuración de encabezado de factura y extras antes de registrar pago o actualizar PDF.
    /// </summary>
    public class InvoicePrepareViewModel : ViewModelBase
    {
        public const decimal FixedTaxRate = 21m;

        private readonly string _bookingId;
        private readonly bool _returnToFormBooking;

        private bool _isLoading = true;
        private bool _hasExistingInvoice;

        private string _companyName = "";
        private string _companyTaxId = "";
        private string _companyAddress = "";

        private string _roomSummary = "";

        private readonly AsyncRelayCommand _applyAndDownloadCommand;
        private readonly AsyncRelayCommand _downloadOnlyCommand;

        public InvoicePrepareViewModel(string bookingId, bool returnToFormBooking = false)
        {
            _bookingId = bookingId;
            _returnToFormBooking = returnToFormBooking;
            ExtraRows = new ObservableCollection<InvoiceExtraRow>();

            BackCommand = new RelayCommand(_ => NavigateBack());
            _applyAndDownloadCommand = new AsyncRelayCommand(ApplyAndDownloadAsync, () => !IsLoading);
            _downloadOnlyCommand = new AsyncRelayCommand(DownloadOnlyAsync, () => !IsLoading && HasExistingInvoice);
            ApplyAndDownloadCommand = _applyAndDownloadCommand;
            DownloadOnlyCommand = _downloadOnlyCommand;

            _ = LoadAsync();
        }

        public ObservableCollection<InvoiceExtraRow> ExtraRows { get; }

        public ICommand ApplyAndDownloadCommand { get; }
        public ICommand DownloadOnlyCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (!SetProperty(ref _isLoading, value)) return;
                _applyAndDownloadCommand.RaiseCanExecuteChanged();
                _downloadOnlyCommand.RaiseCanExecuteChanged();
            }
        }

        public bool HasExistingInvoice
        {
            get => _hasExistingInvoice;
            set
            {
                if (!SetProperty(ref _hasExistingInvoice, value)) return;
                _downloadOnlyCommand.RaiseCanExecuteChanged();
            }
        }

        public string RoomSummary
        {
            get => _roomSummary;
            set => SetProperty(ref _roomSummary, value);
        }

        public bool ShowExtrasEmptyHint => ExtraRows.Count == 0;

        public string CompanyName
        {
            get => _companyName;
            set => SetProperty(ref _companyName, value);
        }

        public string CompanyTaxId
        {
            get => _companyTaxId;
            set => SetProperty(ref _companyTaxId, value);
        }

        public string CompanyAddress
        {
            get => _companyAddress;
            set => SetProperty(ref _companyAddress, value);
        }

        public decimal TaxRate => FixedTaxRate;

        public ICommand BackCommand { get; }

        private void NavigateBack()
        {
            if (_returnToFormBooking)
                NavigationService.Instance.NavigateTo<FormBookingView>();
            else
                NavigationService.Instance.NavigateTo<BookingView>();
        }

        private async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                var booking = await BookingService.GetBookingByIdAsync(_bookingId);
                if (booking == null)
                {
                    MessageBox.Show("No se pudo cargar la reserva.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigateBack();
                    return;
                }

                HasExistingInvoice = !string.IsNullOrWhiteSpace(booking.InvoiceNumber);

                if (booking.InvoiceCompany != null)
                {
                    CompanyName = booking.InvoiceCompany.Name ?? "";
                    CompanyTaxId = booking.InvoiceCompany.TaxId ?? "";
                    CompanyAddress = booking.InvoiceCompany.Address ?? "";
                }

                var room = await RoomService.GetRoomByIdAsync(booking.Room);
                if (room != null)
                {
                    RoomSummary = $"Habitación {room.RoomNumber} ({room.Type})";
                    foreach (var ex in room.Extras)
                    {
                        if (string.IsNullOrWhiteSpace(ex)) continue;
                        ExtraRows.Add(new InvoiceExtraRow { Name = ex.Trim(), Quantity = 1, UnitPrice = 15m, Include = true });
                    }

                    if (room.ExtraBed)
                        ExtraRows.Add(new InvoiceExtraRow { Name = "Cama extra", Quantity = 1, UnitPrice = 15m, Include = true });
                    if (room.Crib)
                        ExtraRows.Add(new InvoiceExtraRow { Name = "Cuna", Quantity = 1, UnitPrice = 15m, Include = true });
                }
                else
                {
                    // Si no se puede cargar la habitación, igualmente permitimos continuar.
                }

                OnPropertyChanged(nameof(ShowExtrasEmptyHint));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al cargar datos", MessageBoxButton.OK, MessageBoxImage.Error);
                NavigateBack();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private object BuildPayload()
        {
            var extras = ExtraRows
                .Where(r => r.Include && !string.IsNullOrWhiteSpace(r.Name))
                .Select(r => new { name = r.Name.Trim(), quantity = r.Quantity, unitPrice = (double)r.UnitPrice })
                .ToList();

            return new
            {
                company = new
                {
                    name = string.IsNullOrWhiteSpace(CompanyName) ? null : CompanyName.Trim(),
                    taxId = string.IsNullOrWhiteSpace(CompanyTaxId) ? null : CompanyTaxId.Trim(),
                    address = string.IsNullOrWhiteSpace(CompanyAddress) ? null : CompanyAddress.Trim()
                },
                extras,
                taxRate = (double)FixedTaxRate
            };
        }

        private async Task ApplyAndDownloadAsync()
        {
            try
            {
                IsLoading = true;
                var payload = BuildPayload();

                if (HasExistingInvoice)
                    await BookingService.PatchBookingInvoiceAsync(_bookingId, payload);
                else
                    await BookingService.PayBookingAsync(_bookingId, payload);

                HasExistingInvoice = true;
                await BookingEvents.RaiseBookingChanged();
                await OpenPdfAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al generar la factura", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DownloadOnlyAsync()
        {
            try
            {
                IsLoading = true;
                await OpenPdfAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error al descargar la factura", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task OpenPdfAsync()
        {
            var tempPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"factura_{_bookingId}.pdf");

            await BookingService.DownloadInvoicePdfAsync(_bookingId, tempPath);

            var psi = new System.Diagnostics.ProcessStartInfo(tempPath)
            {
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
        }
    }
}
