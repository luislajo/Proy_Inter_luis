using System;
using System.Net.Http;

namespace desktop_app.Services
{
    /// <summary>
    /// Servicio centralizado para la configuración y gestión del cliente HTTP.
    /// </summary>
    public static class ApiService
    {
        /// <summary>
        /// URL Base en la que nos basamos.
        /// </summary>
        public const string BaseUrl = "http://localhost:3000/";

        /// <summary>
        /// Cliente HTTP reutilizable configurado con autenticación automática.
        /// </summary>
        public static readonly HttpClient _httpClient = CreateClient();

        /// <summary>
        /// Crea y configura la instancia de <see cref="HttpClient"/>.
        /// </summary>
        /// <returns>
        /// Una instancia de <see cref="HttpClient"/> con la URL base y el handler de autenticación configurados.
        /// </returns>
        private static HttpClient CreateClient()
        {
            var handler = new AuthHeaderHandler
            {
                InnerHandler = new HttpClientHandler()
            };

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl)
            };
            return client;
        }
    }
}
