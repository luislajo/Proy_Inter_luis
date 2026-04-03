using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using desktop_app.Models;

namespace desktop_app.Services
{
    public static class InvoiceService
    {
        public class InvoiceListResponse
        {
            public int Total { get; set; }
            public List<InvoiceModel> Invoices { get; set; } = new();
        }

        /// <summary>
        /// Obtiene facturas: sin filtro carga todas (API: Admin/Trabajador);
        /// con texto filtra por DNI/NIE del cliente.
        /// </summary>
        public static async Task<List<InvoiceModel>> GetInvoicesAsync(string? dniOrEmptyForAll)
        {
            string url;
            if (string.IsNullOrWhiteSpace(dniOrEmptyForAll))
                url = $"{ApiService.BaseUrl}invoices";
            else
                url = $"{ApiService.BaseUrl}invoices?dni={Uri.EscapeDataString(dniOrEmptyForAll.Trim())}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await ApiService._httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<InvoiceListResponse>();
            return data?.Invoices ?? new List<InvoiceModel>();
        }
    }
}
