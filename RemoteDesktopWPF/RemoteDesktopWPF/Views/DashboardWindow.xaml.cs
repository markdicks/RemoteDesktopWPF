using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.IO.Compression;
using System.Windows;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Media;

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

        private TcpListener _listener;
        private string _hostPassword = "";
        private CancellationTokenSource _listenerCancellationTokenSource;
        private bool _isLoadingConfig;

        public DashboardWindow()
        {
            InitializeComponent();
            this.Closed += OnWindowClosed;
            LoadConfig();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string targetIp = TargetIpBox.Text.Trim();
            string connectionPassword = TargetPasswordBox.Password;
            ShowInfo("Connecting to host...");
            Log("Connecting to host...");

            if (string.IsNullOrWhiteSpace(targetIp))
            {
                ShowError("Enter a target IP address before connecting.");
                Log("Connection failed: target IP address is required.");
                return;
            }

            if (!IPAddress.TryParse(targetIp, out IPAddress ipAddress))
            {
                ShowError("Enter a valid IP address.");
                Log("Invalid IP address format.");
                return;
            }

            const int port = 49152;
            TcpClient client = new TcpClient();
            ConnectButton.IsEnabled = false;

            try
            {
                var connectTask = client.ConnectAsync(ipAddress, port);
                if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                {
                    throw new TimeoutException("Connection timed out.");
                }

                NetworkStream stream = client.GetStream();

                // Send password
                byte[] passwordBytes = Encoding.UTF8.GetBytes(connectionPassword);
                await stream.WriteAsync(passwordBytes, 0, passwordBytes.Length);

                // Expect an authentication result (e.g., 1 byte: 1 = success, 0 = failure)
                byte[] response = new byte[1];
                int bytesRead = await stream.ReadAsync(response, 0, 1);
                if (bytesRead == 0 || response[0] != 1)
                {
                    throw new UnauthorizedAccessException("Host rejected the connection (wrong password).");
                }

                // Launch remote session window
                RemoteSessionWindow remoteSession = new RemoteSessionWindow(client);
                remoteSession.Show();

                ShowSuccess("Connected to host successfully.");
                Log("Connected to host.");
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowError("Authentication failed. Check the password and try again.");
                Log("Authentication failed: " + ex.Message);
                TryCloseClient(client);
            }
            catch (TimeoutException ex)
            {
                ShowError("The connection attempt timed out.");
                Log("Timeout: " + ex.Message);
                TryCloseClient(client);
            }
            catch (SocketException ex)
            {
                ShowError("Unable to reach the host. Check the address and network connection.");
                Log($"Socket error: {ex.Message}");
                TryCloseClient(client);
            }
            catch (Exception ex)
            {
                ShowError("Connection failed. Review the logs for more details.");
                Log($"Connection failed: {ex.Message}");
                TryCloseClient(client);
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }
        }

        private void HostToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (_isLoadingConfig)
            {
                return;
            }

            _hostPassword = HostPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(_hostPassword))
            {
                ShowError("Enter an access password before enabling hosting.");
                Log("Failed to start hosting: access password is required.");
                HostToggle.IsChecked = false;
                return;
            }

            try
            {
                if (_listener != null)
                {
                    StopHosting();
                }

                string localIp = GetLocalIPAddress();
                int port = 49152;

                _listenerCancellationTokenSource = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Parse(localIp), port);
                _listener.Start();

                Task.Run(() => ListenForClients(_listenerCancellationTokenSource.Token));
                if (!SaveConfig())
                {
                    StopHosting();
                    HostToggle.IsChecked = false;
                    return;
                }

                ShowSuccess($"Hosting enabled on {localIp}:{port}.");
                Log($"Hosting enabled. IP: {localIp}:{port}");
            }
            catch (Exception ex)
            {
                ShowError("Failed to start hosting. Review the logs for more details.");
                Log($"Failed to start hosting: {ex.Message}");
                StopHosting();
                HostToggle.IsChecked = false;
            }
        }

        private void HostToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isLoadingConfig)
            {
                return;
            }

            StopHosting();
            SaveConfig();
            ShowInfo("Hosting disabled.");
            Log("Hosting disabled.");
        }

        private void SaveConnectionSettings_Click(object sender, RoutedEventArgs e)
        {
            if (SaveConfig())
            {
                ShowSuccess("Settings saved successfully.");
                Log("Connection settings saved.");
            }
        }

        private async void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!SaveConfig())
                {
                    return;
                }

                var currentUser = CurrentUserManager.Instance.CurrentUserConfig;
                if (currentUser == null)
                {
                    ShowError("No user is currently loaded.");
                    Log("Failed to update profile: current user configuration is missing.");
                    return;
                }

                var userId = currentUser.UserId;

                var userData = new UpdateUserDto
                {
                    UserName = currentUser.Username,
                    Email = currentUser.Email,
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
                {
                    ShowSuccess("User profile updated successfully.");
                    Log("User profile updated.");
                }
                else
                {
                    ShowError("Failed to update profile.");
                    Log($"Failed to update profile: {response.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                ShowError("An error occurred while updating the profile.");
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
            StopHosting();
            if (Application.Current.Windows.Count == 0)
            {
                Application.Current.Shutdown();
            }
        }

        private void LoadConfig()
        {
            try
            {
                _isLoadingConfig = true;

                string configDirectory = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrWhiteSpace(configDirectory))
                {
                    Directory.CreateDirectory(configDirectory);
                }

                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                else
                {
                    _config = new AppConfig();
                }

                if (_config.Users == null)
                {
                    _config.Users = new Dictionary<string, UserConfig>();
                }

                string currentUsername = CurrentUserManager.Instance.CurrentUsername;
                if (string.IsNullOrWhiteSpace(currentUsername))
                {
                    ShowInfo("No current user is loaded.");
                    return;
                }

                if (_config.Users.TryGetValue(currentUsername, out var cusrrentuser) && cusrrentuser != null)
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

                    ShowSuccess("Configuration loaded successfully.");
                }
                else
                {
                    ShowInfo("No saved configuration was found for the current user.");
                }
            }
            catch (Exception ex)
            {
                _config = new AppConfig();
                ShowError("Failed to load configuration.");
                Log("Failed to load config: " + ex.Message);
            }
            finally
            {
                _isLoadingConfig = false;
            }
        }

        private bool SaveConfig()
        {
            try
            {
                var currentUser = CurrentUserManager.Instance.CurrentUserConfig;
                if (currentUser == null)
                {
                    ShowError("No current user is available to save settings.");
                    return false;
                }

                if (!TryGetValidatedNumber(BitrateBox.Text, "bitrate", out int bitrate))
                {
                    return false;
                }

                if (!TryGetValidatedNumber(FramerateBox.Text, "frame rate", out int framerate))
                {
                    return false;
                }

                if (_config == null)
                {
                    _config = new AppConfig();
                }

                if (_config.Users == null)
                {
                    _config.Users = new Dictionary<string, UserConfig>();
                }

                currentUser.Username = UsernameBox.Text;
                currentUser.Email = EmailBox.Text;
                currentUser.FirstName = FirstNameBox.Text;
                currentUser.LastName = LastNameBox.Text;
                currentUser.Address = AddressBox.Text;
                currentUser.Bitrate = bitrate;
                currentUser.Framerate = framerate;

                currentUser.IndexResolution = ResolutionComboBox.SelectedIndex;
                currentUser.IsHosting = HostToggle.IsChecked == true;
                currentUser.HostPassword = HostPasswordBox.Password;

                _config.Users[currentUser.Username] = currentUser;

                string configDirectory = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrWhiteSpace(configDirectory))
                {
                    Directory.CreateDirectory(configDirectory);
                }

                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
                return true;
            }
            catch (Exception ex)
            {
                ShowError("Failed to save configuration.");
                Log("Failed to save config: " + ex.Message);
                return false;
            }
        }

        private void Log(string message)
        {
            string timestampedMessage = $"{DateTime.Now:T} - {message}";

            Application.Current.Dispatcher.Invoke(() =>
            {
                LogsBox.Items.Add(timestampedMessage);
                LogsBox.ScrollIntoView(LogsBox.Items[LogsBox.Items.Count - 1]);
            });

            try
            {
                var currentUser = CurrentUserManager.Instance.CurrentUserConfig;
                string username = string.IsNullOrWhiteSpace(currentUser?.Username) ? "application" : currentUser.Username;
                string logFilePath = Path.Combine(_logDirPath, $"{username}_log.txt");

                Directory.CreateDirectory(_logDirPath);
                File.AppendAllText(logFilePath, $"{DateTime.Now:yyyyMMdd}:{timestampedMessage}{Environment.NewLine}");

                FileInfo logFile = new FileInfo(logFilePath);
                if (logFile.Exists && logFile.Length > 1024 * 1024)
                {
                    string zipFilePath = Path.Combine(_logDirPath, $"{username}_log_{DateTime.Now:yyyyMMddHHmmss}.zip");
                    CompressLogFile(logFilePath, zipFilePath);
                    File.WriteAllText(logFilePath, string.Empty);
                }
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Unable to write to the log file: {ex.Message}", false, false);
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
                SetStatusMessage("Failed to archive the log file.", false, false);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LogsBox.Items.Add($"{DateTime.Now:T} - Failed to compress log file: {ex.Message}");
                });
            }
        }

        private void SetStatusMessage(string message, bool isSuccess, bool logMessage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessageTextBlock.Text = message;
                StatusMessageTextBlock.Foreground = isSuccess ? System.Windows.Media.Brushes.DarkGreen : System.Windows.Media.Brushes.DarkRed;
            });

            if (logMessage)
            {
                Log(message);
            }
        }

        private void ShowSuccess(string message)
        {
            SetStatusMessage(message, true, false);
        }

        private void ShowError(string message)
        {
            SetStatusMessage(message, false, false);
        }

        private void ShowInfo(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                StatusMessageTextBlock.Text = message;
                StatusMessageTextBlock.Foreground = System.Windows.Media.Brushes.DarkGoldenrod;
            });
        }

        private bool TryGetValidatedNumber(string value, string fieldName, out int parsedValue)
        {
            if (!int.TryParse(value, out parsedValue) || parsedValue <= 0)
            {
                ShowError($"Enter a valid {fieldName} greater than zero.");
                Log($"Failed to save config: invalid {fieldName} value '{value}'.");
                return false;
            }

            return true;
        }

        private void TryCloseClient(TcpClient client)
        {
            try
            {
                client?.Close();
            }
            catch (Exception ex)
            {
                Log("Failed to close client: " + ex.Message);
            }
        }

        #region Hosting/Connecting Logic
        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private void ListenForClients(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_listener.Pending())
                    {
                        TcpClient client = _listener.AcceptTcpClient();
                        Task.Run(() => HandleClient(client));
                    }
                    else
                    {
                        Thread.Sleep(100); // Avoid busy-wait
                    }
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                // Listener stopped
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log($"Error in listener: {ex.Message}"));
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                byte[] buffer = new byte[256];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string receivedPassword = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (receivedPassword == _hostPassword)
                {
                    stream.WriteByte(1); // success
                    Dispatcher.Invoke(() =>
                    {
                        Log("Client connected with valid password.");
                        Task.Run(() => SendVideoToClient(client, CurrentUserManager.Instance.CurrentUserConfig));
                    });
                }
                else
                {
                    stream.WriteByte(0); // failure
                    Log("Client provided invalid password.");
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                Log("Error in HandleClient: " + ex.Message);
                client?.Close();
            }
        }

        private void SendVideoToClient(TcpClient client, UserConfig config)
        {
            try
            {
                double dpiX = 1.0;
                double dpiY = 1.0;

                // SAFELY access DPI from the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dpiScale = VisualTreeHelper.GetDpi(Application.Current.MainWindow);
                    dpiX = dpiScale.DpiScaleX;
                    dpiY = dpiScale.DpiScaleY;
                });
                Task.Run(() =>
                {
                    try
                    {
                        var stream = client.GetStream();
                        int width = (int)(SystemParameters.PrimaryScreenWidth * dpiX);
                        int height = (int)(SystemParameters.PrimaryScreenHeight * dpiY);
                        var bounds = new System.Drawing.Rectangle(0, 0, width, height);

                        var timer = new System.Timers.Timer(1000.0 / config.Framerate);
                        timer.Elapsed += (s, e) =>
                        {
                            try
                            {
                                using (var bmp = new System.Drawing.Bitmap(width, height))
                                using (var g = System.Drawing.Graphics.FromImage(bmp))
                                using (var ms = new MemoryStream())
                                {
                                    g.CopyFromScreen(0, 0, 0, 0, bounds.Size);
                                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg); // Apply compression logic if needed

                                    byte[] data = ms.ToArray();
                                    byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

                                    stream.Write(lengthPrefix, 0, 4);
                                    stream.Write(data, 0, data.Length);
                                }
                            }
                            catch (Exception ex)
                            {
                                Dispatcher.Invoke(() => Log("Streaming error: " + ex.Message));
                            }
                        };

                        timer.Start();

                        // Keep alive while connected
                        while (client.Connected)
                            Thread.Sleep(100);

                        timer.Stop();
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => Log("Streaming error: " + ex.Message));
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Log("Streaming error: " + ex.Message));
            }
        }

        private void StopHosting()
        {
            try
            {
                _listenerCancellationTokenSource?.Cancel();
                _listener?.Stop();
                _listener = null;
                _listenerCancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                Log($"Error stopping host: {ex.Message}");
            }
        }
        #endregion

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
