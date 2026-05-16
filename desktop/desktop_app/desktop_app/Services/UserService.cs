using System.Net.Http;
using System.Net.Http.Json;
using desktop_app.Models;

namespace desktop_app.Services;

public class UserService
{
    public static async Task<List<UserModel>> GetUsersByRolAsync(string rol)
    {
        string url = $"{ApiService.BaseUrl}user/rol/{rol}";
        var users = await ApiService._httpClient.GetFromJsonAsync<List<UserModel>>(url);
        return users ?? new List<UserModel>();
    }

    public static async Task<List<UserModel>> GetAllUsersAsync()
    {
        string url = $"{ApiService.BaseUrl}user/";

        var users = await ApiService._httpClient.GetFromJsonAsync<List<UserModel>>(url);

        return users ?? new List<UserModel>();
    }

    public static async Task<UserModel> GetClientByIdAsync(string id)
    {
        string url = $"{ApiService.BaseUrl}user/getOne";

        var payload = new {searchData = id, searchProperty = "id"};

        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Content = JsonContent.Create(payload)
        };

        var response = await ApiService._httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
            return new UserModel();
        }

        UserModel? content = await response.Content.ReadFromJsonAsync<UserModel>();

        return content ?? new UserModel();
    }
    
    public static async Task<string> GetUserDniByIdAsync(string id)
    {
        string url = $"{ApiService.BaseUrl}user/getOne";

        var payload = new {searchData = id, searchProperty = "id"};

        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Content = JsonContent.Create(payload)
        };

        var response = await ApiService._httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
            return "Error";
        }

        UserModel? content = await response.Content.ReadFromJsonAsync<UserModel>();

        return content?.Dni ?? "Error";
    }
    
    public static async Task<string> GetUserIdByDniAsync(string dni)
    {
        string url = $"{ApiService.BaseUrl}user/getOne";

        var payload = new {searchData = dni, searchProperty = "dni"};

        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Content = JsonContent.Create(payload)
        };

        var response = await ApiService._httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
            return "Error";
        }

        UserModel? content = await response.Content.ReadFromJsonAsync<UserModel>();

        return content?.Id ?? "Error";
    }
    
    
}