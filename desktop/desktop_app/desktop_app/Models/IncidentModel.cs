using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace desktop_app.Models
{
    public class IncidentModel
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("room_id")]
        public string RoomId { get; set; } = "";

        /// <summary>Algunas respuestas JSON usan camelCase en lugar de snake_case.</summary>
        [JsonPropertyName("roomId")]
        public string? RoomIdCamel { get; set; }

        [JsonIgnore]
        public string RoomNumber { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("reported_by")]
        public string ReportedBy { get; set; } = "";

        [JsonPropertyName("assigned_to")]
        public string? AssignedTo { get; set; }

        [JsonPropertyName("assignedTo")]
        public string? AssignedToCamel { get; set; }

        /// <summary>Texto para grid (nombre o «Sin asignar»); se rellena al listar.</summary>
        [JsonIgnore]
        public string AssignedToDisplay { get; set; } = "Sin asignar";

        [JsonPropertyName("reported_at")]
        public DateTime ReportedAt { get; set; }

        [JsonPropertyName("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        [JsonPropertyName("internal_notes")]
        public List<IncidentNote> InternalNotes { get; set; } = new();

        [JsonPropertyName("history")]
        public List<IncidentHistoryEntry> History { get; set; } = new();

        [JsonIgnore]
        public string SeverityLabel => Severity switch
        {
            "low" => "Baja",
            "medium" => "Media",
            "high" => "Alta",
            _ => Severity
        };

        /// <summary>Hay empleado asignado (JSON puede venir en snake_case o camelCase).</summary>
        [JsonIgnore]
        public bool HasAssignee =>
            !string.IsNullOrWhiteSpace(AssignedTo) || !string.IsNullOrWhiteSpace(AssignedToCamel);

        [JsonIgnore]
        public string StatusLabel
        {
            get
            {
                if (string.Equals(Status, "resolved", StringComparison.OrdinalIgnoreCase))
                    return "Resuelta";
                if (HasAssignee)
                    return "En proceso";
                if (string.Equals(Status, "open", StringComparison.OrdinalIgnoreCase))
                    return "Comunicado";
                return Status;
            }
        }
    }

    public class IncidentNote
    {
        [JsonPropertyName("note")]
        public string Note { get; set; } = "";

        [JsonPropertyName("author_id")]
        public string AuthorId { get; set; } = "";

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>Rellenado en cliente a partir de <see cref="AuthorId"/> (no viene del JSON).</summary>
        [JsonIgnore]
        public string AuthorLabel { get; set; } = "";
    }

    public class IncidentHistoryEntry
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = "";

        [JsonPropertyName("by")]
        public string? By { get; set; }

        [JsonPropertyName("at")]
        public DateTime At { get; set; }

        /// <summary>Nombre mostrable; se rellena en cliente desde <see cref="By"/>.</summary>
        [JsonIgnore]
        public string ByLabel { get; set; } = "";
    }

    public class IncidentsResponse
    {
        [JsonPropertyName("items")]
        public List<IncidentModel> Items { get; set; } = new();
    }
}

