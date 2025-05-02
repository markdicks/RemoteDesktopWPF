using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.IO.Compression;
using System.Windows;
using System.Linq;

namespace RemoteDesktopWPF.Views
{
    public partial class DashboardWindow : Window
    {
        private readonly string _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RemoteDesktopWPF", "config.json");

        private readonly string _logDirPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RemoteDesktopWPF", "Logs");

        private AppConfig _config;

        public DashboardWindow()
        {
            InitializeComponent();
            this.Closed += OnWindowClosed;
            LoadConfig();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Log("Connecting to host...");
            // Connection logic here
        }

        private void HostToggle_Checked(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            Log("Hosting enabled.");
        }

        private void HostToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            Log("Hosting disabled.");
        }

        private void SaveConnectionSettings_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            Log("Connection settings saved.");
        }

        private async void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveConfig();
                var currentUser = CurrentUserManager.Instance.CurrentUserConfig;

                var userId = currentUser.UserId;

                var userData = new UpdateUserDto
                {
                    UserName = currentUser.Username,
                    Email = currentUser.Email,
                    Password = "ignored",
                    UserProfile = new UserProfileDto
                    {
                        FirstName = currentUser.FirstName,
                        LastName = currentUser.LastName,
                        Address = currentUser.Address
                    }
                };

                var userService = new UserService();
                var response = await userService.UpdateUserAsync(userId, userData);

                if (response.Success)
                    Log("User profile updated.");
                else
                    Log($"Failed to update profile: {response.ErrorMessage}");
            }
            catch (Exception ex)
            {
                Log("Error updating profile: " + ex.Message);
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            CurrentUserManager.Instance.CurrentUsername = null;
            CurrentUserManager.Instance.CurrentUserConfig = null;

            var welcome = new MainWindow();
            welcome.Show();

            foreach (Window window in Application.Current.Windows)
            {
                if (window != welcome)
                    window.Close();
            }
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            if (Application.Current.Windows.Count == 0)
            {
                Application.Current.Shutdown();
            }
        }

        private void LoadConfig()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_configPath));
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<AppConfig>(json);
                }
                else
                {
                    _config = new AppConfig();
                }


                var cusrrentuser = _config.Users[CurrentUserManager.Instance.CurrentUsername];
                if (cusrrentuser != null)
                {
                    UsernameBox.Text = cusrrentuser.Username;
                    EmailBox.Text = cusrrentuser.Email;
                    FirstNameBox.Text = cusrrentuser.FirstName;
                    LastNameBox.Text = cusrrentuser.LastName;
                    AddressBox.Text = cusrrentuser.Address;

                    ResolutionComboBox.SelectedIndex = cusrrentuser.IndexResolution;
                    BitrateBox.Text = cusrrentuser.Bitrate.ToString();
                    FramerateBox.Text = cusrrentuser.Framerate.ToString();

                    HostToggle.IsChecked = cusrrentuser.IsHosting;
                    HostPasswordBox.Password = cusrrentuser.HostPassword;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load config: " + ex.Message);
            }
        }

        private void SaveConfig()
        {
            try
            {
                var currentUser = CurrentUserManager.Instance.CurrentUserConfig;
                currentUser.Username = UsernameBox.Text;
                currentUser.Email = EmailBox.Text;
                currentUser.FirstName = FirstNameBox.Text;
                currentUser.LastName = LastNameBox.Text;
                currentUser.Address = AddressBox.Text;

                if (int.TryParse(BitrateBox.Text, out int bitrate))
                    currentUser.Bitrate = bitrate;

                if (int.TryParse(FramerateBox.Text, out int framerate))
                    currentUser.Framerate = framerate;

                currentUser.IndexResolution = ResolutionComboBox.SelectedIndex;
                currentUser.IsHosting = HostToggle.IsChecked == true;
                currentUser.HostPassword = HostPasswordBox.Password;

                _config.Users[currentUser.Username] = currentUser;

                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save config: " + ex.Message);
            }
        }

        private void Log(string message)
        {
            var currentUser = CurrentUserManager.Instance.CurrentUserConfig;
            string logFilePath = Path.Combine(_logDirPath, $"{currentUser.Username}_log.txt");

            Directory.CreateDirectory(_logDirPath);

            string timestampedMessage = $"{DateTime.Now:T} - {message}";

            File.AppendAllText(logFilePath, $"{timestampedMessage}{Environment.NewLine}");

            LogsBox.Items.Add(timestampedMessage);
            LogsBox.ScrollIntoView(LogsBox.Items[LogsBox.Items.Count - 1]);

            FileInfo logFile = new FileInfo(logFilePath);
            if (logFile.Length > 1024 * 1024)
            {
                string zipFilePath = Path.Combine(_logDirPath, $"{currentUser.Username}_log_{DateTime.Now:yyyyMMddHHmmss}.zip");
                CompressLogFile(logFilePath, zipFilePath);
                File.WriteAllText(logFilePath, string.Empty);
            }
        }

        private void CompressLogFile(string logFilePath, string zipFilePath)
        {
            try
            {
                using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(logFilePath, Path.GetFileName(logFilePath));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to compress log file: " + ex.Message);
            }
        }

    }

    public class AppConfig
    {
        public Dictionary<string, UserConfig> Users { get; set; } = new Dictionary<string, UserConfig>();
    }

    public class UserConfig
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public bool IsHosting { get; set; } = false;
        public string HostPassword { get; set; } = "";
        public int IndexResolution { get; set; } = 1;
        public int Framerate { get; set; } = 30;
        public int Bitrate { get; set; } = 2500;
    }
}
