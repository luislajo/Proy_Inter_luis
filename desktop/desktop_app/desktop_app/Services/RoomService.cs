using desktop_app.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace desktop_app.Services
{
    /// <summary>
    /// Servicio para operaciones CRUD de habitaciones contra la API REST.
    /// </summary>
    public static class RoomService
    {
        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        public static async Task<List<RoomModel>?> GetRoomsStatusBoardAsync()
        {
            try
            {
                var url = ApiService.BaseUrl + "room/status-board";
                var resp = await ApiService._httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return new List<RoomModel>();
                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<RoomModel>>(json, _jsonOptions) ?? new List<RoomModel>();
            }
            catch
            {
                return null;
            }
        }

        public static async Task<(bool Ok, string? Error)> PatchRoomStatusAsync(string roomId, string status, string? reason, int? estimatedMinutes)
        {
            try
            {
                var url = ApiService.BaseUrl + $"room/{Uri.EscapeDataString(roomId)}/status";
                var payload = new
                {
                    status,
                    reason,
                    estimatedMinutes
                };
                var json = JsonSerializer.Serialize(payload, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var req = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
                var resp = await ApiService._httpClient.SendAsync(req);
                if (resp.IsSuccessStatusCode) return (true, null);

                var body = await resp.Content.ReadAsStringAsync();
                var msg = $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}";
                return (false, msg);
            }
            catch
            {
                return (false, "Error de conexión con API.");
            }
        }

        public static async Task<List<RoomStatusLogEntry>?> GetRoomStatusLogAsync(string roomId)
        {
            try
            {
                var url = ApiService.BaseUrl + $"room/{Uri.EscapeDataString(roomId)}/status-log";
                var resp = await ApiService._httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return new List<RoomStatusLogEntry>();
                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<RoomStatusLogEntry>>(json, _jsonOptions) ?? new List<RoomStatusLogEntry>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Obtiene habitaciones aplicando un filtro opcional.
        /// </summary>
        /// <param name="f">Filtro de búsqueda. Si es null, devuelve todas.</param>
        /// <returns>Respuesta con lista de habitaciones, o null si hay error de conexión.</returns>
        public static async Task<RoomsResponse?> GetRoomsFilteredAsync(RoomsFilter? f = null)
        {
            try
            {
                f ??= new RoomsFilter();

                var parameters = new Dictionary<string, string?>();

                if (!string.IsNullOrWhiteSpace(f.Type))
                    parameters["type"] = f.Type;

                if (f.IsAvailable.HasValue)
                    parameters["isAvailable"] = f.IsAvailable.Value.ToString().ToLowerInvariant();

                if (f.MinPrice.HasValue)
                    parameters["minPrice"] = f.MinPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

                if (f.MaxPrice.HasValue)
                    parameters["maxPrice"] = f.MaxPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

                if (f.Guests.HasValue)
                    parameters["guests"] = f.Guests.Value.ToString();

                if (f.HasExtraBed.HasValue)
                    parameters["hasExtraBed"] = f.HasExtraBed.Value.ToString().ToLowerInvariant();

                if (f.HasCrib.HasValue)
                    parameters["hasCrib"] = f.HasCrib.Value.ToString().ToLowerInvariant();

                if (f.HasOffer.HasValue)
                    parameters["hasOffer"] = f.HasOffer.Value.ToString().ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(f.RoomNumber))
                    parameters["roomNumber"] = f.RoomNumber;

                if (f.Extras != null && f.Extras.Count > 0)
                    parameters["extras"] = string.Join(",", f.Extras);

                if (!string.IsNullOrWhiteSpace(f.SortBy))
                    parameters["sortBy"] = f.SortBy;

                if (!string.IsNullOrWhiteSpace(f.SortOrder))
                    parameters["sortOrder"] = f.SortOrder;

                string url = ApiService.BaseUrl + "room" + BuildQuery(parameters);

                var respuesta = await ApiService._httpClient.GetAsync(url);

                if (!respuesta.IsSuccessStatusCode)
                    return new RoomsResponse();

                string contenido = await respuesta.Content.ReadAsStringAsync();
                var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var data = JsonSerializer.Deserialize<RoomsResponse>(contenido, opciones);

                return data ?? new RoomsResponse();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Obtiene una habitación por su ID.
        /// </summary>
        /// <param name="roomId">ID de la habitación.</param>
        /// <returns>La habitación encontrada, o null si no existe.</returns>
        public static async Task<RoomModel?> GetRoomByIdAsync(string roomId)
        {
            try
            {
                var url = ApiService.BaseUrl + $"room/{Uri.EscapeDataString(roomId)}";
                var resp = await ApiService._httpClient.GetAsync(url);

                if (!resp.IsSuccessStatusCode) return null;

                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<RoomModel>(json, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Crea una nueva habitación.
        /// </summary>
        /// <param name="room">Datos de la habitación a crear.</param>
        /// <returns>La habitación creada con su ID asignado.</returns>
        /// <exception cref="HttpRequestException">Si la API responde con error.</exception>
        /// <exception cref="Exception">Si no se puede deserializar la respuesta.</exception>
        public static async Task<RoomModel> CreateRoomAsync(RoomModel room)
        {
            var url = ApiService.BaseUrl + "room";
            var json = JsonSerializer.Serialize(room, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await ApiService._httpClient.PostAsync(url, content);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"No se pudo crear la habitación. HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}\n" +
                    $"Respuesta API:\n{body}\n\nPayload enviado:\n{json}"
                );

            var created = JsonSerializer.Deserialize<RoomModel>(body, _jsonOptions);

            if (created is null)
                throw new Exception(
                    $"La API respondió OK, pero no se pudo deserializar.\nBody:\n{body}"
                );

            return created;
        }

        /// <summary>
        /// Actualiza una habitación existente.
        /// </summary>
        /// <param name="roomId">ID de la habitación a actualizar.</param>
        /// <param name="room">Datos actualizados.</param>
        /// <returns>True si se actualizó correctamente, false en caso contrario.</returns>
        public static async Task<bool> UpdateRoomAsync(string roomId, RoomModel room)
        {
            try
            {
                var url = ApiService.BaseUrl + $"room/{Uri.EscapeDataString(roomId)}";
                var json = JsonSerializer.Serialize(room, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var req = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                {
                    Content = content
                };

                var resp = await ApiService._httpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Elimina una habitación.
        /// </summary>
        /// <param name="roomId">ID de la habitación a eliminar.</param>
        /// <returns>True si se eliminó correctamente, false en caso contrario.</returns>
        public static async Task<bool> DeleteRoomAsync(string roomId)
        {
            try
            {
                var url = ApiService.BaseUrl + $"room/{Uri.EscapeDataString(roomId)}";
                var resp = await ApiService._httpClient.DeleteAsync(url);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Construye una query string desde un diccionario.
        /// </summary>
        /// <param name="parameters">Diccionario clave-valor.</param>
        /// <returns>Query string formateada (?key1=value1&amp;key2=value2).</returns>
        private static string BuildQuery(Dictionary<string, string?> parameters)
        {
            var sb = new StringBuilder();
            bool first = true;

            foreach (var kv in parameters)
            {
                if (string.IsNullOrWhiteSpace(kv.Value))
                    continue;

                sb.Append(first ? "?" : "&");
                first = false;

                sb.Append(WebUtility.UrlEncode(kv.Key));
                sb.Append("=");
                sb.Append(WebUtility.UrlEncode(kv.Value));
            }

            return sb.ToString();
        }
    }
}