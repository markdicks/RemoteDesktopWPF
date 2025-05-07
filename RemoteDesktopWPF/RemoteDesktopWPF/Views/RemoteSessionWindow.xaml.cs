using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace RemoteDesktopWPF.Views
{
    public partial class RemoteSessionWindow : Window
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;

        public RemoteSessionWindow(TcpClient client)
        {
            InitializeComponent();
            _client = client;
            _stream = client.GetStream();
            _cts = new CancellationTokenSource();

            Task.Run(() => ReceiveVideo(_cts.Token));
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            CloseSession();
        }

        private void CloseSession()
        {
            try
            {
                _cts?.Cancel();
                _stream?.Close();
                _client?.Close();
            }
            catch { /* swallow for now */ }

            Dispatcher.Invoke(() =>
            {
                if (this.IsLoaded)
                    this.Close();
            });
        }

        private void ReceiveVideo(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Detect socket disconnect
                    if (!_client.Connected || !_stream.CanRead)
                        throw new IOException("Disconnected from host.");

                    byte[] lengthBuffer = new byte[4];
                    int read = _stream.Read(lengthBuffer, 0, 4);
                    if (read == 0)
                        throw new IOException("Stream ended unexpectedly.");

                    int length = BitConverter.ToInt32(lengthBuffer, 0);

                    byte[] buffer = new byte[length];
                    int totalRead = 0;
                    while (totalRead < length)
                    {
                        int chunk = _stream.Read(buffer, totalRead, length - totalRead);
                        if (chunk == 0)
                            throw new IOException("Incomplete frame received.");
                        totalRead += chunk;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            using (var ms = new MemoryStream(buffer))
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.StreamSource = ms;
                                bitmap.EndInit();
                                bitmap.Freeze(); // Important for cross-thread
                                VideoDisplay.Source = bitmap;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogError("Error decoding image: " + ex.Message);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LogError("Disconnected or error receiving video: " + ex.Message);
                PromptReconnect();
            }
        }

        private void PromptReconnect()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show("Connection lost. Reconnect?", "Disconnected", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    // TODO: Implement reconnect logic (e.g., store IP/Password and retry)
                    MessageBox.Show("Reconnect not implemented yet.");
                }
                else
                {
                    CloseSession();
                }
            });
        }

        private void LogError(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, "Remote Session Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

}