using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using desktop_app.Models;

namespace desktop_app.Services
{
    /// <summary>
    /// Servicio para obtener registros de auditoría desde la API.
    /// Sigue el mismo patrón que RoomService / BookingService.
    /// </summary>
    public static class AuditLogService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Obtiene todos los registros de auditoría del sistema.
        /// </summary>
        public static async Task<List<AuditLogModel>> GetAllAuditLogsAsync()
        {
            try
            {
                string url = ApiService.BaseUrl + "audit";
                var response = await ApiService._httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return new List<AuditLogModel>();

                string json = await response.Content.ReadAsStringAsync();
                var logs = JsonSerializer.Deserialize<List<AuditLogModel>>(json, _jsonOptions);

                return logs ?? new List<AuditLogModel>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuditLogService] Error: {ex.Message}");
                return new List<AuditLogModel>();
            }
        }
    }
}
