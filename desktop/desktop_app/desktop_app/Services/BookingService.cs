using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using desktop_app.Models;
using Newtonsoft.Json;

namespace desktop_app.Services
{
    public static class BookingService
    {
        public class PayBookingResponse
        {
            public string? status { get; set; }
            public BookingModel? booking { get; set; }
        }

        /// <summary>
        /// Obtiene las reservas de la API
        /// </summary>
        /// <returns>
        /// Lista de las reservas en la base de datos
        /// Una lista en blanco en caso de no poder leer el contenido de la respuesta de la API
        /// </returns>
        public static async Task<List<BookingModel>> GetAllBookingsAsync()
        {
            return (
                await (
                    await CreateResponse("", new Object(), HttpMethod.Get)).Content.ReadFromJsonAsync<List<BookingModel>>()
                ) ?? new List<BookingModel>();
        }

        
        /// <summary>
        /// Borra una reserva
        /// </summary>
        /// 
        /// <param name="bookingId">
        /// ID de la reserva a eliminar
        /// </param>
        /// 
        /// <returns>
        /// Valor booleando:
        ///     - true si se ha eliminado
        ///     - false si no se ha podido eliminar
        /// </returns>
        public static async Task<bool> DeleteBooking(string bookingId)
        {
            return (
                await CreateResponse(bookingId, new Object(), HttpMethod.Delete)
                ).IsSuccessStatusCode;
        }

        public static async Task DownloadInvoicePdfAsync(string bookingId, string filePath)
        {
            var url = $"{ApiService.BaseUrl}booking/{bookingId}/invoice";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/pdf"));

            using var response = await ApiService._httpClient.SendAsync(request);
            await HandleError(response);

            var bytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(filePath, bytes);
        }

        /// <summary>Obtiene una reserva por id (incluye invoice_number, invoiceIssuer si existen).</summary>
        public static async Task<BookingModel?> GetBookingByIdAsync(string bookingId)
        {
            string url = $"{ApiService.BaseUrl}booking/{bookingId}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await ApiService._httpClient.SendAsync(request);
            await HandleError(response);
            var booking = await response.Content.ReadFromJsonAsync<BookingModel>();
            if (booking == null) return null;
            booking.CheckInDate = DateTime.SpecifyKind(booking.CheckInDate, DateTimeKind.Utc).ToLocalTime();
            booking.CheckOutDate = DateTime.SpecifyKind(booking.CheckOutDate, DateTimeKind.Utc).ToLocalTime();
            booking.PayDate = DateTime.SpecifyKind(booking.PayDate, DateTimeKind.Utc).ToLocalTime();
            return booking;
        }

        /// <summary>
        /// Registra un "pago" para una reserva y genera la factura (invoice_number).
        /// </summary>
        public static async Task<BookingModel?> PayBookingAsync(string bookingId, object payload)
        {
            var response = await CreateResponse($"{bookingId}/pay", payload, HttpMethod.Post);
            var data = await response.Content.ReadFromJsonAsync<PayBookingResponse>();
            return data?.booking;
        }

        /// <summary>Actualiza datos de factura ya emitida (PDF).</summary>
        public static async Task<BookingModel?> PatchBookingInvoiceAsync(string bookingId, object payload)
        {
            var response = await CreateResponse($"{bookingId}/invoice", payload, HttpMethod.Patch);
            return await response.Content.ReadFromJsonAsync<BookingModel>();
        }

        
        
        /// <summary>
        /// Actualiza una reserva
        /// </summary>
        /// 
        /// <param name="booking">
        /// Reserva con la información modificada
        /// </param>
        /// 
        /// <returns>
        /// Devuelve la reserva modificada en la API
        /// </returns>
        public static async Task<BookingModel?> UpdateBookingAsync(BookingModel booking)
        {
            var payload = new {
                checkInDate = DateTime.SpecifyKind(booking.CheckInDate, DateTimeKind.Utc),
                checkOutDate = DateTime.SpecifyKind(booking.CheckOutDate, DateTimeKind.Utc),
                guests = booking.Guests};

            var response = await CreateResponse(booking.Id, payload, HttpMethod.Patch);
            
            var updatedBooking = await response.Content.ReadFromJsonAsync<BookingModel>();

            if (updatedBooking != null)
            {
                updatedBooking.CheckInDate = DateTime.SpecifyKind(updatedBooking.CheckInDate, DateTimeKind.Utc).ToLocalTime();
                updatedBooking.CheckOutDate = DateTime.SpecifyKind(updatedBooking.CheckOutDate, DateTimeKind.Utc).ToLocalTime();
                updatedBooking.PayDate = DateTime.SpecifyKind(updatedBooking.PayDate, DateTimeKind.Utc).ToLocalTime();
            }
            
            return updatedBooking;
        }

        
        /// <summary>
        /// Crea una reserva
        /// </summary>
        /// 
        /// <param name="booking">
        /// Reserva a añadir a la base de datos
        /// </param>
        /// 
        /// <returns>
        /// La reserva nueva devuelta por la base de datos
        /// </returns>
        public static async Task<BookingModel?> CreateBookingAsync(BookingModel booking)
        {
            var payload = new {client = booking.Client, room = booking.Room, checkInDate = booking.CheckInDate, checkOutDate = booking.CheckOutDate, guests = booking.Guests};

            var response = await CreateResponse("", payload, HttpMethod.Post);
            
            var createdBooking = await response.Content.ReadFromJsonAsync<BookingModel>();

            return createdBooking;
        }

        
        /// <summary>
        /// Cancela una reserva
        /// </summary>
        /// 
        /// <param name="bookingId">
        /// ID de la reserva a cancelar
        /// </param>
        /// 
        /// <returns>
        /// Devuelve la reserva cancelada devuelta por la base de datos
        /// </returns>
        public static async Task<BookingModel?> CancelBookingAsync(string bookingId)
        {
            var response = await CreateResponse($"{bookingId}/cancel", new Object(), HttpMethod.Patch);
            
            var cancelBooking = await response.Content.ReadFromJsonAsync<BookingModel>();

            return cancelBooking;
        }

        
        /// <summary>
        /// Método que crea la solicitud, obtiene la respuesta y verifica los errores
        /// </summary>
        /// 
        /// <param name="endpoint">
        /// String del endpoint al que se debe comunicar
        /// Como este es el manejador de reservas ya empieza la URL con el acceso al router de reservas de la API
        /// </param>
        /// <param name="payload">
        /// Objeto con los datos que se deben de enviar en el body de la solicitud a la API
        /// </param>
        /// <param name="method">
        /// Método de la solicitud HTTP
        /// </param>
        /// 
        /// <returns>
        /// Devuelve la respuesta del servidor en caso de que no haya error
        /// </returns>
        private static async Task<HttpResponseMessage> CreateResponse(string endpoint, object payload, HttpMethod method)
        {
            string url = $"{ApiService.BaseUrl}booking/{endpoint}";
            
            var request = new HttpRequestMessage(method, url)
            {
                Content = JsonContent.Create(payload)
            };

            var response = await ApiService._httpClient.SendAsync(request);

            await HandleError(response);
            
            return response;
        }

        /// <summary>
        /// Manejador de errores en la comunicación con el servidor
        /// Recibe una respuesta y verifica si tiene errores
        /// En caso de haberlos los maneja
        /// </summary>
        /// 
        /// <param name="response">
        /// Recibe la respuesta de la API 
        /// </param>
        /// 
        /// <returns>
        /// Indica como completada la tarea en caso de que no haya error en la respuesta
        /// </returns>
        /// 
        /// <exception cref="Exception">
        /// Lanza excepciones con los errores personalizados que provienen de la API en caso de error
        /// </exception>
        private static Task HandleError (HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string error = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("Error en la API de booking: " + error);
                var value = JsonConvert.DeserializeObject<Dictionary<string, string>>(error);
                if (value != null)
                {
                    var errors = value["error"];
                    string errString = String.Join("\n", errors.Split(", "));
                    throw new Exception(errString);
                }
                throw new Exception(error);
            }
            return Task.CompletedTask;
        }
    }
}