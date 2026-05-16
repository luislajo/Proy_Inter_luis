using desktop_app.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace desktop_app.Services
{
    public static class IncidentService
    {
        private static readonly JsonSerializerOptions _jsonOptions =
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

        public static async Task<List<IncidentModel>?> GetIncidentsAsync(
            string? severity = null,
            string? status = null,
            string? roomId = null,
            DateTime? from = null,
            DateTime? to = null
        )
        {
            try
            {
                var qs = new List<string>();
                if (!string.IsNullOrWhiteSpace(severity)) qs.Add($"severity={WebUtility.UrlEncode(severity)}");
                if (!string.IsNullOrWhiteSpace(status)) qs.Add($"status={WebUtility.UrlEncode(status)}");
                if (!string.IsNullOrWhiteSpace(roomId)) qs.Add($"roomId={WebUtility.UrlEncode(roomId)}");
                if (from.HasValue) qs.Add($"from={WebUtility.UrlEncode(from.Value.ToString("yyyy-MM-dd"))}");
                if (to.HasValue) qs.Add($"to={WebUtility.UrlEncode(to.Value.ToString("yyyy-MM-dd"))}");

                var url = ApiService.BaseUrl + "incidents" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
                var resp = await ApiService._httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return new List<IncidentModel>();

                var json = await resp.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<IncidentsResponse>(json, _jsonOptions);
                return data?.Items ?? new List<IncidentModel>();
            }
            catch
            {
                return null;
            }
        }

        public static async Task<IncidentModel?> GetIncidentByIdAsync(string id)
        {
            try
            {
                var url = ApiService.BaseUrl + $"incidents/{Uri.EscapeDataString(id)}";
                var resp = await ApiService._httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IncidentModel>(json, _jsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<bool> ResolveAsync(string id)
        {
            try
            {
                var url = ApiService.BaseUrl + $"incidents/{Uri.EscapeDataString(id)}/resolve";
                var req = new HttpRequestMessage(new HttpMethod("PATCH"), url);
                var resp = await ApiService._httpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> AssignAsync(string id, string employeeId)
        {
            try
            {
                var url = ApiService.BaseUrl + $"incidents/{Uri.EscapeDataString(id)}/assign";
                var payload = JsonSerializer.Serialize(new { employeeId }, _jsonOptions);
                var req = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                var resp = await ApiService._httpClient.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> AddNoteAsync(string id, string note)
        {
            try
            {
                var url = ApiService.BaseUrl + $"incidents/{Uri.EscapeDataString(id)}/notes";
                var payload = JsonSerializer.Serialize(new { note }, _jsonOptions);
                var resp = await ApiService._httpClient.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"));
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}

