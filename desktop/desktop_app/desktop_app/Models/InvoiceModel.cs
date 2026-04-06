using System;
using System.Text.Json.Serialization;

namespace desktop_app.Models
{
    public class InvoiceBreakdownModel
    {
        [JsonPropertyName("nightsSubtotal")]
        public decimal NightsSubtotal { get; set; }

        [JsonPropertyName("extrasSubtotal")]
        public decimal ExtrasSubtotal { get; set; }

        [JsonPropertyName("discountAmount")]
        public decimal DiscountAmount { get; set; }

        [JsonPropertyName("taxRate")]
        public decimal TaxRate { get; set; }

        [JsonPropertyName("taxAmount")]
        public decimal TaxAmount { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }
    }

    public class InvoiceModel
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("room")]
        public string RoomId { get; set; } = string.Empty;

        [JsonPropertyName("client")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("checkInDate")]
        public DateTime CheckInDate { get; set; }

        [JsonPropertyName("checkOutDate")]
        public DateTime CheckOutDate { get; set; }

        [JsonPropertyName("totalNights")]
        public int TotalNights { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("invoice_number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [JsonPropertyName("invoiceDate")]
        public DateTime? InvoiceDate { get; set; }

        [JsonPropertyName("invoiceBreakdown")]
        public InvoiceBreakdownModel? Breakdown { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        // Datos enriquecidos desde otros servicios
        [JsonIgnore]
        public string ClientName { get; set; } = string.Empty;

        [JsonIgnore]
        public string ClientDni { get; set; } = string.Empty;

        [JsonIgnore]
        public string RoomNumber { get; set; } = string.Empty;
    }
}

