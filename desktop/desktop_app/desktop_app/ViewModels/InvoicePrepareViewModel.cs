using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

    /// <summary>Fila del panel resumen (previsualización del importe de factura).</summary>
    public sealed class InvoicePreviewLine : ViewModelBase
    {
        public InvoicePreviewLine(string concept, string amountText, bool isTotalRow = false)
        {
            Concept = concept;
            AmountText = amountText;
            IsTotalRow = isTotalRow;
        }

        public string Concept { get; }
        public string AmountText { get; }
        public bool IsTotalRow { get; }
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

        private int _snapshotTotalNights;
        private decimal _snapshotChargedPricePerNight;
        private decimal? _snapshotRoomCatalogPricePerNight;
        private decimal _snapshotOfferPercent;

        private readonly HashSet<InvoiceExtraRow> _extraRowsSubscribed = new();

        private readonly AsyncRelayCommand _applyAndDownloadCommand;
        private readonly AsyncRelayCommand _downloadOnlyCommand;

        public InvoicePrepareViewModel(string bookingId, bool returnToFormBooking = false)
        {
            _bookingId = bookingId;
            _returnToFormBooking = returnToFormBooking;
            ExtraRows = new ObservableCollection<InvoiceExtraRow>();
            InvoicePreviewLines = new ObservableCollection<InvoicePreviewLine>();
            ExtraRows.CollectionChanged += ExtraRowsOnCollectionChanged;

            BackCommand = new RelayCommand(_ => NavigateBack());
            _applyAndDownloadCommand = new AsyncRelayCommand(ApplyAndDownloadAsync, () => !IsLoading);
            _downloadOnlyCommand = new AsyncRelayCommand(DownloadOnlyAsync, () => !IsLoading && HasExistingInvoice);
            ApplyAndDownloadCommand = _applyAndDownloadCommand;
            DownloadOnlyCommand = _downloadOnlyCommand;

            _ = LoadAsync();
        }

        public ObservableCollection<InvoiceExtraRow> ExtraRows { get; }

        /// <summary>Líneas del resumen fiscal (misma base de cálculo que la API).</summary>
        public ObservableCollection<InvoicePreviewLine> InvoicePreviewLines { get; }

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

        private static decimal ToMoney(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private void ExtraRowsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (InvoiceExtraRow row in e.NewItems)
                            AttachExtraRow(row);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (InvoiceExtraRow row in e.OldItems)
                            DetachExtraRow(row);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null)
                    {
                        foreach (InvoiceExtraRow row in e.OldItems)
                            DetachExtraRow(row);
                    }
                    if (e.NewItems != null)
                    {
                        foreach (InvoiceExtraRow row in e.NewItems)
                            AttachExtraRow(row);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var row in _extraRowsSubscribed.ToList())
                        DetachExtraRow(row);
                    foreach (var row in ExtraRows)
                        AttachExtraRow(row);
                    break;
            }

            RecalculateInvoicePreview();
        }

        private void AttachExtraRow(InvoiceExtraRow row)
        {
            if (!_extraRowsSubscribed.Add(row)) return;
            row.PropertyChanged += ExtraRowOnPropertyChanged;
        }

        private void DetachExtraRow(InvoiceExtraRow row)
        {
            if (!_extraRowsSubscribed.Remove(row)) return;
            row.PropertyChanged -= ExtraRowOnPropertyChanged;
        }

        private void ExtraRowOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(InvoiceExtraRow.Include)
                or nameof(InvoiceExtraRow.Quantity)
                or nameof(InvoiceExtraRow.UnitPrice)
                or nameof(InvoiceExtraRow.Name))
                RecalculateInvoicePreview();
        }

        /// <summary>Réplica de <c>applyInvoiceMath</c> del servidor para mostrar el mismo total que el PDF.</summary>
        private void RecalculateInvoicePreview()
        {
            InvoicePreviewLines.Clear();

            decimal extrasSubtotal = 0m;
            foreach (var r in ExtraRows)
            {
                if (!r.Include || string.IsNullOrWhiteSpace(r.Name)) continue;
                extrasSubtotal += ToMoney(r.Quantity * r.UnitPrice);
            }

            extrasSubtotal = ToMoney(extrasSubtotal);

            decimal offerPercent = Math.Min(100m, Math.Max(0m, _snapshotOfferPercent));

            decimal listPricePerNight = _snapshotRoomCatalogPricePerNight is decimal catPn && catPn >= 0
                ? ToMoney(catPn)
                : offerPercent > 0 && offerPercent < 100
                    ? ToMoney(_snapshotChargedPricePerNight / (1 - offerPercent / 100))
                    : ToMoney(_snapshotChargedPricePerNight);

            decimal nightsListSubtotal = ToMoney(_snapshotTotalNights * listPricePerNight);
            decimal nightsSubtotal = ToMoney(_snapshotTotalNights * _snapshotChargedPricePerNight);
            decimal discountAmount =
                offerPercent > 0 ? ToMoney(Math.Max(0, nightsListSubtotal - nightsSubtotal)) : 0m;

            bool showDiscountBreakdown =
                discountAmount > 0.005m && nightsListSubtotal > nightsSubtotal + 0.005m;

            decimal taxableBase = Math.Max(0, nightsSubtotal + extrasSubtotal);
            decimal taxAmount = ToMoney(taxableBase * (TaxRate / 100m));
            decimal grandTotal = ToMoney(taxableBase + taxAmount);

            string eur(decimal v) => $"{ToMoney(v):N2} EUR";

            if (showDiscountBreakdown)
            {
                InvoicePreviewLines.Add(new InvoicePreviewLine($"Precio catálogo ({_snapshotTotalNights} noches)", eur(nightsListSubtotal)));
                InvoicePreviewLines.Add(new InvoicePreviewLine($"Descuento habitación ({offerPercent:N0} %)", $"- {eur(discountAmount)}"));
                InvoicePreviewLines.Add(new InvoicePreviewLine("Subtotal noches", eur(nightsSubtotal)));
            }
            else
                InvoicePreviewLines.Add(new InvoicePreviewLine($"Subtotal noches", eur(nightsSubtotal)));

            if (extrasSubtotal > 0)
                InvoicePreviewLines.Add(new InvoicePreviewLine("Subtotal extras", eur(extrasSubtotal)));

            InvoicePreviewLines.Add(new InvoicePreviewLine($"IVA ({TaxRate:N0} %)", eur(taxAmount)));
            InvoicePreviewLines.Add(new InvoicePreviewLine("Total", eur(grandTotal), isTotalRow: true));
        }

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

                _snapshotTotalNights = booking.TotalNights;
                _snapshotChargedPricePerNight = booking.PricePerNight ?? 0m;

                if (booking.InvoiceCompany != null)
                {
                    CompanyName = booking.InvoiceCompany.Name ?? "";
                    CompanyTaxId = booking.InvoiceCompany.TaxId ?? "";
                    CompanyAddress = booking.InvoiceCompany.Address ?? "";
                }

                var room = await RoomService.GetRoomByIdAsync(booking.Room);
                _snapshotRoomCatalogPricePerNight = room?.PricePerNight;
                _snapshotOfferPercent = Math.Min(100m, Math.Max(0m, room?.Offer ?? booking.Offer ?? 0m));

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
                    RoomSummary = string.IsNullOrWhiteSpace(RoomSummary)
                        ? $"Reserva · {_snapshotTotalNights} noches"
                        : RoomSummary;
                    // Si no se puede cargar la habitación, igualmente permitimos continuar.
                }

                OnPropertyChanged(nameof(ShowExtrasEmptyHint));
                RecalculateInvoicePreview();
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
