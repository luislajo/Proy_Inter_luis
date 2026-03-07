using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace desktop_app.Models
{
    /// <summary>
    /// Modelo de datos para un registro de auditoría.
    /// Mapea directamente el JSON devuelto por GET /audit
    /// </summary>
    public class AuditLogModel
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("entity_type")]
        public string EntityType { get; set; } = string.Empty;

        [JsonPropertyName("entity_id")]
        public string EntityId { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("actor_id")]
        public string ActorId { get; set; } = string.Empty;

        [JsonPropertyName("actor_type")]
        public string ActorType { get; set; } = string.Empty;

        [JsonPropertyName("previous_state")]
        public JsonElement? PreviousState { get; set; }

        [JsonPropertyName("new_state")]
        public JsonElement? NewState { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("__v")]
        public int Version { get; set; }

        // ── Propiedades calculadas para la UI ──

        /// <summary>
        /// Resumen legible de los cambios detectados
        /// </summary>
        [JsonIgnore]
        public string DifferencesSummary
        {
            get
            {
                try
                {
                    if (Action == "CREATE")
                        return $"Se creó {EntityType}";

                    if (Action == "DELETE")
                        return $"Se eliminó {EntityType}";

                    if (Action == "PAYMENT")
                    {
                        if (NewState.HasValue && NewState.Value.ValueKind == JsonValueKind.Object)
                        {
                            var nights = NewState.Value.TryGetProperty("totalNights", out var n) ? n.ToString() : "?";
                            var price = NewState.Value.TryGetProperty("totalPrice", out var p) ? p.ToString() : "?";
                            return $"Pago por {nights} noches: {price}€";
                        }
                        return "Pago realizado";
                    }

                    if (Action == "UPDATE" || Action == "CANCEL")
                    {
                        if (!PreviousState.HasValue || PreviousState.Value.ValueKind != JsonValueKind.Object ||
                            !NewState.HasValue || NewState.Value.ValueKind != JsonValueKind.Object)
                            return "Cambios detectados";

                        var diffs = new List<string>();
                        var prevDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(PreviousState.Value.GetRawText());
                        var newDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(NewState.Value.GetRawText());

                        if (prevDict != null && newDict != null)
                        {
                            foreach (var key in newDict.Keys)
                            {
                                if (key == "_id" || key == "__v" || key == "updatedAt") continue;

                                var newVal = newDict[key].ToString();
                                var prevVal = prevDict.ContainsKey(key) ? prevDict[key].ToString() : "—";

                                if (prevVal != newVal)
                                    diffs.Add($"{key}: {prevVal} → {newVal}");
                            }
                        }

                        return diffs.Count > 0
                            ? string.Join(", ", diffs)
                            : "Sin cambios relevantes";
                    }

                    return Action;
                }
                catch
                {
                    return "Cambios detectados";
                }
            }
        }
    }
}
