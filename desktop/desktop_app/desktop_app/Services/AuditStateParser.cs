using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using desktop_app.Models;

namespace desktop_app.Services
{
    public static class AuditStateParser
    {
        private static readonly HashSet<string> SkipKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "_id", "__v", "updatedAt", "createdAt"
        };

        public static List<AuditStateFieldRow> BuildComparisonRows(JsonElement? previous, JsonElement? next)
        {
            var prevDict = ParseObject(previous);
            var nextDict = ParseObject(next);

            var keys = prevDict.Keys
                .Union(nextDict.Keys, StringComparer.OrdinalIgnoreCase)
                .Where(k => !SkipKeys.Contains(k))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var rows = new List<AuditStateFieldRow>();
            foreach (var key in keys)
            {
                prevDict.TryGetValue(key, out var prevEl);
                nextDict.TryGetValue(key, out var nextEl);

                var prevText = FormatValue(prevEl);
                var nextText = FormatValue(nextEl);

                if (prevText == "—" && nextText == "—")
                    continue;

                rows.Add(new AuditStateFieldRow
                {
                    FieldKey = key,
                    FieldLabel = ToLabel(key),
                    PreviousDisplay = prevText,
                    NewDisplay = nextText,
                    IsChanged = !string.Equals(prevText, nextText, StringComparison.Ordinal)
                });
            }

            return rows;
        }

        private static Dictionary<string, JsonElement> ParseObject(JsonElement? element)
        {
            var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            if (!element.HasValue || element.Value.ValueKind != JsonValueKind.Object)
                return dict;

            foreach (var prop in element.Value.EnumerateObject())
            {
                if (!SkipKeys.Contains(prop.Name))
                    dict[prop.Name] = prop.Value;
            }

            return dict;
        }

        private static string FormatValue(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Undefined)
                return "—";

            return el.ValueKind switch
            {
                JsonValueKind.Null => "—",
                JsonValueKind.True => "Sí",
                JsonValueKind.False => "No",
                JsonValueKind.String => FormatString(el.GetString()),
                JsonValueKind.Number => FormatNumber(el),
                JsonValueKind.Array => FormatArray(el),
                JsonValueKind.Object => FormatObject(el),
                _ => el.GetRawText()
            };
        }

        private static string FormatNumber(JsonElement el)
        {
            if (el.TryGetDecimal(out var dec) && dec == Math.Truncate(dec))
                return ((long)dec).ToString(CultureInfo.InvariantCulture);
            return el.GetRawText();
        }

        private static string FormatArray(JsonElement el)
        {
            var items = new List<string>();
            foreach (var item in el.EnumerateArray())
            {
                var text = FormatValue(item);
                if (text != "—")
                    items.Add(text);
            }

            if (items.Count == 0)
                return "—";

            if (items.Count == 1)
                return items[0];

            return string.Join(Environment.NewLine, items.Select(s => $"• {s}"));
        }

        private static string FormatObject(JsonElement el)
        {
            var lines = new List<string>();
            foreach (var prop in el.EnumerateObject())
            {
                if (SkipKeys.Contains(prop.Name))
                    continue;

                var value = FormatValue(prop.Value);
                if (value == "—")
                    continue;

                lines.Add($"{ToLabel(prop.Name)}: {value}");
            }

            if (lines.Count == 0)
                return "—";

            if (lines.Count == 1)
                return lines[0];

            return string.Join(Environment.NewLine, lines);
        }

        private static string FormatString(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "—";

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt) ||
                DateTime.TryParse(raw, CultureInfo.CurrentCulture, DateTimeStyles.None, out dt))
            {
                var local = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
                return local.ToString("dd/MM/yyyy HH:mm");
            }

            if (raw.Contains('/') || raw.Contains('\\'))
                return ShortenPath(raw);

            return raw;
        }

        private static string ShortenPath(string path)
        {
            var normalized = path.Trim().Replace('\\', '/');
            var last = normalized.LastIndexOf('/');
            return last >= 0 ? normalized[(last + 1)..] : normalized;
        }

        private static string ToLabel(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return key;

            var parts = key.Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', parts.Select(p =>
                p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
        }
    }
}
