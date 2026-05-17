using System.Text.Json.Serialization;
    
namespace desktop_app.Models
{
    public class BookingModel
    {
        [JsonPropertyName("_id")] 
        public string Id { get; set; } = "";
        
        [JsonPropertyName("room")]
        public string Room { get; set; }
        
        [JsonPropertyName("client")]
        public string Client { get; set; }

        [JsonPropertyName("checkInDate")]
        public DateTime CheckInDate { get; set; } = DateTime.Now;
        
        [JsonPropertyName("checkOutDate")]
        public  DateTime CheckOutDate { get; set; } = DateTime.Now;

        [JsonPropertyName("payDate")] public DateTime PayDate { get; set; } = DateTime.Now;
        
        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("pricePerNight")]
        public decimal? PricePerNight { get; set; }

        [JsonPropertyName("offer")]
        public decimal? Offer { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("guests")] 
        public int Guests { get; set; } = 0;

        [JsonPropertyName("totalNights")]
        public int TotalNights { get; set; }

        [JsonPropertyName("invoice_number")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("checkInCode")]
        public string? CheckInCode { get; set; }

        [JsonPropertyName("checkedIn")]
        public bool CheckedIn { get; set; }

        [JsonPropertyName("canSubmitCheckIn")]
        public bool CanSubmitCheckIn { get; set; }

        [JsonPropertyName("checkInCodeSent")]
        public bool CheckInCodeSent { get; set; }

        [JsonPropertyName("checkedOut")]
        public bool CheckedOut { get; set; }

        [JsonPropertyName("canSubmitCheckOut")]
        public bool CanSubmitCheckOut { get; set; }

        [JsonPropertyName("isCheckInDayToday")]
        public bool IsCheckInDayToday { get; set; }

        [JsonPropertyName("isCheckOutDayToday")]
        public bool IsCheckOutDayToday { get; set; }

        [JsonPropertyName("stayWindowOpen")]
        public bool StayWindowOpen { get; set; }

        [JsonPropertyName("invoiceIssuer")]
        public InvoiceIssuerDto? InvoiceIssuer { get; set; }

        [JsonPropertyName("invoiceCompany")]
        public InvoiceIssuerDto? InvoiceCompany { get; set; }

        [JsonPropertyName("invoiceBreakdown")]
        public InvoiceBreakdownDto? InvoiceBreakdown { get; set; }

        [JsonIgnore] 
        public string RoomNumber { get; set; } = "";

        [JsonIgnore] 
        public string ClientName { get; set; } = "Por conseguir";

        [JsonIgnore] 
        public string ClientDni { get; set; } = "";
        
        public BookingModel Clone()
        {
            return new BookingModel
            {
                Id = Id,
                Room = Room,
                Client = Client,
                CheckInDate = CheckInDate,
                CheckOutDate = CheckOutDate,
                PayDate = PayDate,
                TotalPrice = TotalPrice,
                PricePerNight = PricePerNight,
                Offer = Offer,
                Status = Status,
                Guests = Guests,
                TotalNights = TotalNights,
                RoomNumber = RoomNumber,
                ClientName = ClientName,
                ClientDni = ClientDni,
                CheckInCode = CheckInCode,
                CheckedIn = CheckedIn,
                CanSubmitCheckIn = CanSubmitCheckIn,
                CheckInCodeSent = CheckInCodeSent,
                CheckedOut = CheckedOut,
                CanSubmitCheckOut = CanSubmitCheckOut,
                IsCheckInDayToday = IsCheckInDayToday,
                IsCheckOutDayToday = IsCheckOutDayToday,
                StayWindowOpen = StayWindowOpen,
            };
        }

        public override string ToString()
        {
            return "ID: " + this.Id + "\n"
                   + "ID del cliente: " + this.Client + "\n"
                   + "\tDNI del cliente: " + this.ClientDni + "\n"
                   + "\tNombre del cliente: " + this.ClientName + "\n"
                   + "ID de la habitación: " + this.Room + "\n"
                   + "\tNúmero de la habitación: " + this.RoomNumber + "\n"
                   + "Fecha de inicio: " + this.CheckInDate.ToString("d") + "\n"
                   + "Fecha de fin: " + this.CheckOutDate.ToString("d") + "\n"
                   + "Fecha de reserva y pago: " + this.PayDate.ToString("d") + "\n"
                   + "Oferta: " + this.Offer + "%\n"
                   + "Precio total: " + this.TotalPrice + "€\n"
                   + "Precio por noche: " + this.PricePerNight + "€\n"
                   + "Total de noches: " + this.TotalNights + "\n"
                   + "Cantidad de huéspedes: " + this.Guests;
        }
    }

    /// <summary>Datos del emisor en la factura (cabecera PDF).</summary>
    public class InvoiceIssuerDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("taxId")]
        public string? TaxId { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }
    }

    public class InvoiceBreakdownDto
    {
        [JsonPropertyName("extras")]
        public List<InvoiceExtraLineDto> Extras { get; set; } = new();
    }

    public class InvoiceExtraLineDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; } = 1;

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }
    }
}
