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

        [JsonIgnore]
        public string StatusLabel => Status switch
        {
            "open" => "Abierta",
            "resolved" => "Resuelta",
            _ => Status
        };
    }

    public class IncidentNote
    {
        [JsonPropertyName("note")]
        public string Note { get; set; } = "";

        [JsonPropertyName("author_id")]
        public string AuthorId { get; set; } = "";

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class IncidentHistoryEntry
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = "";

        [JsonPropertyName("by")]
        public string? By { get; set; }

        [JsonPropertyName("at")]
        public DateTime At { get; set; }
    }

    public class IncidentsResponse
    {
        [JsonPropertyName("items")]
        public List<IncidentModel> Items { get; set; } = new();
    }
}

