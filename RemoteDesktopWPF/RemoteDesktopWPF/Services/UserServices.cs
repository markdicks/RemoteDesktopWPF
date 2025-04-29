using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RemoteDesktopWPF.Services;

public class UserService
{
    public async Task<bool> RegisterUserAsync(CreateUserDto user)
    {
        var json = JsonConvert.SerializeObject(user);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await ApiService.Client.PostAsync("/api/Users", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<UserDto> GetUserAsync(long id)
    {
        var response = await ApiService.Client.GetAsync($"/api/Users/{id}");
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<UserDto>(json);
    }

    public async Task<bool> UpdateUserAsync(long id, UpdateUserDto user)
    {
        var json = JsonConvert.SerializeObject(user);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await ApiService.Client.PutAsync($"/api/Users/{id}", content);
        return response.IsSuccessStatusCode;
    }
}
