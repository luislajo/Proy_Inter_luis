using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>Línea de concepto facturado (estancia o extra).</summary>
    public class InvoiceBilledRow : ViewModelBase
    {
        private string _name = "";
        private int _quantity = 1;
        private decimal _unitPrice;

        public bool IsStayLine { get; init; }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (!SetProperty(ref _quantity, value)) return;
                OnPropertyChanged(nameof(LineTotal));
                OnPropertyChanged(nameof(LineTotalDisplay));
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (!SetProperty(ref _unitPrice, value)) return;
                OnPropertyChanged(nameof(LineTotal));
                OnPropertyChanged(nameof(LineTotalDisplay));
            }
        }

        public decimal LineTotal => Math.Round(_quantity * _unitPrice, 2, MidpointRounding.AwayFromZero);

        public string LineTotalDisplay => $"{LineTotal:N2} €";
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

        private const string DefaultIssuerName = "Hotel Pere Maria";
        private const string DefaultIssuerTaxId = "B84729163";
        private const string DefaultIssuerAddress = "Av. de la Marina Baixa, 15, 03502 Benidorm, Alicante";

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

        private readonly HashSet<InvoiceBilledRow> _billedRowsSubscribed = new();

        private string _issuerName = DefaultIssuerName;
        private string _issuerTaxId = DefaultIssuerTaxId;
        private string _issuerAddress = DefaultIssuerAddress;
        private string _clientName = "";
        private string _clientDni = "";
        private string _clientEmail = "";
        private string _clientCity = "";
        private string _stayDatesText = "";

        private readonly AsyncRelayCommand _applyAndDownloadCommand;
        private readonly AsyncRelayCommand _downloadOnlyCommand;

        public InvoicePrepareViewModel(string bookingId, bool returnToFormBooking = false)
        {
            _bookingId = bookingId;
            _returnToFormBooking = returnToFormBooking;
            BilledRows = new ObservableCollection<InvoiceBilledRow>();
            InvoicePreviewLines = new ObservableCollection<InvoicePreviewLine>();
            BilledRows.CollectionChanged += BilledRowsOnCollectionChanged;

            BackCommand = new RelayCommand(_ => NavigateBack());
            _applyAndDownloadCommand = new AsyncRelayCommand(ApplyAndDownloadAsync, () => !IsLoading);
            _downloadOnlyCommand = new AsyncRelayCommand(DownloadOnlyAsync, () => !IsLoading && HasExistingInvoice);
            ApplyAndDownloadCommand = _applyAndDownloadCommand;
            DownloadOnlyCommand = _downloadOnlyCommand;

            _ = LoadAsync();
        }

        public ObservableCollection<InvoiceBilledRow> BilledRows { get; }

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

        public bool ShowExtrasEmptyHint => BilledRows.Count(r => !r.IsStayLine) == 0;

        public string IssuerName
        {
            get => _issuerName;
            set => SetProperty(ref _issuerName, value);
        }

        public string IssuerTaxId
        {
            get => _issuerTaxId;
            set => SetProperty(ref _issuerTaxId, value);
        }

        public string IssuerAddress
        {
            get => _issuerAddress;
            set => SetProperty(ref _issuerAddress, value);
        }

        public string ClientName
        {
            get => _clientName;
            set => SetProperty(ref _clientName, value);
        }

        public string ClientDni
        {
            get => _clientDni;
            set => SetProperty(ref _clientDni, value);
        }

        public string ClientEmail
        {
            get => _clientEmail;
            set => SetProperty(ref _clientEmail, value);
        }

        public string ClientCity
        {
            get => _clientCity;
            set => SetProperty(ref _clientCity, value);
        }

        public string StayDatesText
        {
            get => _stayDatesText;
            set => SetProperty(ref _stayDatesText, value);
        }

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

        private void BilledRowsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (InvoiceBilledRow row in e.NewItems)
                            AttachBilledRow(row);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (InvoiceBilledRow row in e.OldItems)
                            DetachBilledRow(row);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null)
                    {
                        foreach (InvoiceBilledRow row in e.OldItems)
                            DetachBilledRow(row);
                    }
                    if (e.NewItems != null)
                    {
                        foreach (InvoiceBilledRow row in e.NewItems)
                            AttachBilledRow(row);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var row in _billedRowsSubscribed.ToList())
                        DetachBilledRow(row);
                    foreach (var row in BilledRows)
                        AttachBilledRow(row);
                    break;
            }

            OnPropertyChanged(nameof(ShowExtrasEmptyHint));
            RecalculateInvoicePreview();
        }

        private void AttachBilledRow(InvoiceBilledRow row)
        {
            if (!_billedRowsSubscribed.Add(row)) return;
            row.PropertyChanged += BilledRowOnPropertyChanged;
        }

        private void DetachBilledRow(InvoiceBilledRow row)
        {
            if (!_billedRowsSubscribed.Remove(row)) return;
            row.PropertyChanged -= BilledRowOnPropertyChanged;
        }

        private void BilledRowOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is InvoiceBilledRow { IsStayLine: true }) return;
            if (e.PropertyName is nameof(InvoiceBilledRow.Quantity)
                or nameof(InvoiceBilledRow.UnitPrice)
                or nameof(InvoiceBilledRow.Name))
                RecalculateInvoicePreview();
        }

        /// <summary>Réplica de <c>applyInvoiceMath</c> del servidor para mostrar el mismo total que el PDF.</summary>
        private void RecalculateInvoicePreview()
        {
            InvoicePreviewLines.Clear();

            decimal extrasSubtotal = 0m;
            foreach (var r in BilledRows.Where(r => !r.IsStayLine))
            {
                if (string.IsNullOrWhiteSpace(r.Name)) continue;
                extrasSubtotal += ToMoney(r.Quantity * r.UnitPrice);
            }

            extrasSubtotal = ToMoney(extrasSubtotal);

            decimal nightsSubtotal = ToMoney(_snapshotTotalNights * _snapshotChargedPricePerNight);
            decimal taxableBase = Math.Max(0, nightsSubtotal + extrasSubtotal);
            decimal taxAmount = ToMoney(taxableBase * (TaxRate / 100m));
            decimal grandTotal = ToMoney(taxableBase + taxAmount);

            string eur(decimal v) => $"{ToMoney(v):N2} EUR";

            InvoicePreviewLines.Add(new InvoicePreviewLine("Base imponible", eur(taxableBase)));
            InvoicePreviewLines.Add(new InvoicePreviewLine($"IVA ({TaxRate:N0} %)", eur(taxAmount)));
            InvoicePreviewLines.Add(new InvoicePreviewLine("Total factura", eur(grandTotal), isTotalRow: true));
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

                var issuer = booking.InvoiceIssuer;
                IssuerName = string.IsNullOrWhiteSpace(issuer?.Name) ? DefaultIssuerName : issuer!.Name!;
                IssuerTaxId = string.IsNullOrWhiteSpace(issuer?.TaxId) ? DefaultIssuerTaxId : issuer!.TaxId!;
                IssuerAddress = string.IsNullOrWhiteSpace(issuer?.Address) ? DefaultIssuerAddress : issuer!.Address!;

                var client = await UserService.GetClientByIdAsync(booking.Client);
                ClientName = $"{client.FirstName} {client.LastName}".Trim();
                ClientDni = client.Dni ?? "";
                ClientEmail = client.Email ?? "";
                ClientCity = client.CityName ?? "";

                if (booking.InvoiceCompany != null)
                {
                    CompanyName = booking.InvoiceCompany.Name ?? "";
                    CompanyTaxId = booking.InvoiceCompany.TaxId ?? "";
                    CompanyAddress = booking.InvoiceCompany.Address ?? "";
                }

                var room = await RoomService.GetRoomByIdAsync(booking.Room);
                _snapshotRoomCatalogPricePerNight = room?.PricePerNight;
                _snapshotOfferPercent = Math.Min(100m, Math.Max(0m, room?.Offer ?? booking.Offer ?? 0m));

                var roomLabel = room != null ? $"Habitación {room.RoomNumber} - {room.Type}" : "Habitación";
                RoomSummary = roomLabel;
                StayDatesText =
                    $"{booking.CheckInDate:dd/MM/yyyy} – {booking.CheckOutDate:dd/MM/yyyy} · {_snapshotTotalNights} noches";

                BilledRows.Clear();
                var stayConcept = $"Alojamiento / {roomLabel}";
                if (_snapshotOfferPercent > 0.005m)
                    stayConcept += $"\nTarifa con descuento {_snapshotOfferPercent:N0} %";
                BilledRows.Add(new InvoiceBilledRow
                {
                    IsStayLine = true,
                    Name = stayConcept,
                    Quantity = Math.Max(1, _snapshotTotalNights),
                    UnitPrice = _snapshotChargedPricePerNight
                });

                var savedExtras = booking.InvoiceBreakdown?.Extras;
                if (savedExtras is { Count: > 0 })
                {
                    foreach (var ex in savedExtras)
                    {
                        if (string.IsNullOrWhiteSpace(ex.Name)) continue;
                        BilledRows.Add(new InvoiceBilledRow
                        {
                            Name = ex.Name.Trim(),
                            Quantity = Math.Max(1, ex.Quantity),
                            UnitPrice = ex.UnitPrice
                        });
                    }
                }
                else if (room != null)
                {
                    foreach (var ex in room.Extras)
                    {
                        if (string.IsNullOrWhiteSpace(ex)) continue;
                        BilledRows.Add(new InvoiceBilledRow { Name = ex.Trim(), Quantity = 1, UnitPrice = 15m });
                    }

                    if (room.ExtraBed)
                        BilledRows.Add(new InvoiceBilledRow { Name = "Cama extra", Quantity = 1, UnitPrice = 15m });
                    if (room.Crib)
                        BilledRows.Add(new InvoiceBilledRow { Name = "Cuna", Quantity = 1, UnitPrice = 15m });
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
            var extras = BilledRows
                .Where(r => !r.IsStayLine && !string.IsNullOrWhiteSpace(r.Name))
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
                await LoadAsync();
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
