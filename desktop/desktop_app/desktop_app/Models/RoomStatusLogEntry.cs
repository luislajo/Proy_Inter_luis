using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace desktop_app.Models
{
    /// <summary>Etiquetas en español para códigos de estado de habitación (API).</summary>
    public static class RoomStatusLabels
    {
        public static string ToSpanish(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "—";
            return code.Trim().ToLowerInvariant() switch
            {
                "available" => "Disponible",
                "occupied" => "Ocupada",
                "cleaning" => "Limpieza",
                "maintenance" => "Mantenimiento",
                "blocked" => "Bloqueada",
                _ => code.Trim()
            };
        }
    }

    public class RoomStatusLogEntry
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("room_id")]
        public string RoomId { get; set; } = "";

        [JsonPropertyName("previous_status")]
        public string? PreviousStatus { get; set; }

        [JsonPropertyName("new_status")]
        public string NewStatus { get; set; } = "";

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("estimated_minutes")]
        public int? EstimatedMinutes { get; set; }

        /// <summary>ObjectId del usuario en API (string); no se muestra en la tabla.</summary>
        [JsonPropertyName("changed_by")]
        public string? ChangedBy { get; set; }

        [JsonPropertyName("changed_by_role")]
        public string ChangedByRole { get; set; } = "";

        [JsonPropertyName("changed_at")]
        public DateTime ChangedAt { get; set; }

        [JsonIgnore]
        public string PreviousStatusSpanish => RoomStatusLabels.ToSpanish(PreviousStatus);

        [JsonIgnore]
        public string NewStatusSpanish => RoomStatusLabels.ToSpanish(NewStatus);

        [JsonIgnore]
        public string ReasonDisplay => string.IsNullOrWhiteSpace(Reason) ? "—" : Reason.Trim();

        [JsonIgnore]
        public string EstimatedMinutesDisplay =>
            EstimatedMinutes.HasValue ? EstimatedMinutes.Value.ToString(CultureInfo.InvariantCulture) : "—";

        [JsonIgnore]
        public string ChangedAtText =>
            ChangedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-ES"));

    }
}

