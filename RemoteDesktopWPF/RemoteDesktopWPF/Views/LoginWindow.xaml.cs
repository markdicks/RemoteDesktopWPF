using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RemoteDesktopWPF.Views
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private Window _mainWindow;
        public LoginWindow(Window main)
        {
            InitializeComponent();
            _mainWindow = main;
            this.Closed += OnWindowClosed;
        }
        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            var userService = new UserService();
            var username = UsernameBox.Text.ToString();
            if (username == null || username == "")
            {
                MessageBox.Show("Invalid username");
                return;
            }

            var user = await userService.GetUserByUsernameAsync(username);

            if (user != null && user.Password == PasswordBox.Password)
            {
                SaveUserToAppConfig(user);
                MessageBox.Show($"Welcome {user.UserName}!");

                _mainWindow.Close();
                var dashboardWindow = new DashboardWindow();
                dashboardWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Login failed.");
            }
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.Show();
            this.Close();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            if (Application.Current.Windows.Count == 0)
            {
                Application.Current.Shutdown();
            }
        }

        private void SaveUserToAppConfig(UserDto user)
        {
            var configPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RemoteDesktopWPF", "config.json");
            var configDir = System.IO.Path.GetDirectoryName(configPath);
            Directory.CreateDirectory(configDir);

            AppConfig appConfig;

            // Load existing config or create a new one if not present
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                appConfig = JsonSerializer.Deserialize<AppConfig>(json);
            }
            else
            {
                appConfig = new AppConfig();
            }

            // Store user config under their username
            if (appConfig.Users == null)
            {
                appConfig.Users = new Dictionary<string, UserConfig>();
            }

            var existingUserConfig = appConfig.Users.ContainsKey(user.UserName) ? appConfig.Users[user.UserName] : null;

            appConfig.Users[user.UserName] = new UserConfig
            {
                UserId = user.Id,
                Username = user.UserName,
                Email = user.Email,
                FirstName = user.UserProfile?.FirstName,
                LastName = user.UserProfile?.LastName,
                Address = user.UserProfile?.Address,
                IsHosting = existingUserConfig?.IsHosting ?? false,
                HostPassword = existingUserConfig?.HostPassword ?? string.Empty,
                IndexResolution = existingUserConfig?.IndexResolution ?? 1,
                Framerate = existingUserConfig?.Framerate ?? 30,
                Bitrate = existingUserConfig?.Bitrate ?? 2500
            };

            // Save back to the config file
            var updatedJson = JsonSerializer.Serialize(appConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, updatedJson);
            CurrentUserManager.Instance.CurrentUsername = user.UserName;
            CurrentUserManager.Instance.CurrentUserConfig = appConfig.Users[user.UserName];
        }

    }
}
