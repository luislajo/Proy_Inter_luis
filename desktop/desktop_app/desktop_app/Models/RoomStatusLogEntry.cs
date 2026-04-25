using System;
using System.Text.Json.Serialization;

namespace desktop_app.Models
{
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

        [JsonPropertyName("changed_by_role")]
        public string ChangedByRole { get; set; } = "";

        [JsonPropertyName("changed_at")]
        public DateTime ChangedAt { get; set; }
    }
}

