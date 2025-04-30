using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using RemoteDesktopWPF.Services;

public class UserService
{
    public class Result
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
    public async Task<Result> RegisterUserAsync(CreateUserDto user)
    {
        try
        {
            var json = JsonConvert.SerializeObject(user);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await ApiService.Client.PostAsync("/api/Users", content);
            if (response.IsSuccessStatusCode)
            {
                return new Result { Success = true, ErrorMessage = string.Empty };
            }
            else
            {
                string errorMessage = "Registration failed.";
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();

                    dynamic errorJson = JsonConvert.DeserializeObject(errorResponse);
                    if (errorJson != null && errorJson.message != null)
                    {
                        errorMessage = errorJson.message;
                    }
                }

                return new Result
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }
        }
        catch (Exception ex)
        {
            return new Result
            {
                Success = false,
                ErrorMessage = "Unable to connect to server: " + ex.Message
            };
        }
    }

    public async Task<UserDto> GetUserAsync(long id)
    {
        var response = await ApiService.Client.GetAsync($"/api/Users/{id}");
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<UserDto>(json);
    }
    
    public async Task<UserDto> GetUserByUsernameAsync(string username)
    {
        var response = await ApiService.Client.GetAsync($"/api/Users/username/{username}");
        var json = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<UserDto>(json);
    }

    public async Task<Result> UpdateUserAsync(long id, UpdateUserDto user)
    {
        var json = JsonConvert.SerializeObject(user);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await ApiService.Client.PutAsync($"/api/Users/{id}", content);
        return new Result{Success = response.IsSuccessStatusCode, ErrorMessage = response.ReasonPhrase };
    }
}
