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
        /// Texto visible en la columna "Resumen"
        /// </summary>
        [JsonIgnore]
        public string SummaryText
        {
            get
            {
                return Action switch
                {
                    "CREATE" => $"Se creó {EntityType}",
                    "DELETE" => $"Se eliminó {EntityType}",
                    "PAYMENT" => "Pago recibido",
                    "UPDATE" => $"Se modificó {EntityType}",
                    "CANCEL" => $"Se canceló {EntityType}",
                    _ => Action
                };
            }
        }

        /// <summary>
        /// Texto a mostrar en el Tooltip al poner el ratón encima
        /// </summary>
        [JsonIgnore]
        public string TooltipText
        {
            get
            {
                return SummaryText;
            }
        }

        private List<string> GetDifferences()
        {
            var diffs = new List<string>();
            try
            {
                if (!PreviousState.HasValue || PreviousState.Value.ValueKind != JsonValueKind.Object ||
                    !NewState.HasValue || NewState.Value.ValueKind != JsonValueKind.Object)
                    return diffs;

                var prevDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(PreviousState.Value.GetRawText());
                var newDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(NewState.Value.GetRawText());

                if (prevDict != null && newDict != null)
                {
                    foreach (var key in newDict.Keys)
                    {
                        if (key == "_id" || key == "__v" || key == "updatedAt" || key == "createdAt") continue;

                        var newVal = newDict[key].ToString();
                        var prevVal = prevDict.ContainsKey(key) ? prevDict[key].ToString() : "—";

                        if (prevVal != newVal)
                            diffs.Add($"{key}: {prevVal} => {newVal}");
                    }
                }
            }
            catch { }

            return diffs;

        }
    }
}
      