using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace RemoteDesktopWPF.Views
{
    public partial class RemoteSessionWindow : Window
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private bool _isClosing;

        public RemoteSessionWindow(TcpClient client)
        {
            InitializeComponent();
            _client = client;
            _stream = client.GetStream();
            _cts = new CancellationTokenSource();
            Closed += RemoteSessionWindow_Closed;

            ShowStatus("Connected to remote session.", false, false);

            Task.Run(() => ReceiveVideo(_cts.Token));
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            CloseSession();
        }

        private void CloseSession()
        {
            if (_isClosing)
            {
                return;
            }

            _isClosing = true;

            try
            {
                _cts?.Cancel();
                _stream?.Close();
                _client?.Close();
            }
            catch (Exception ex)
            {
                ShowStatus("Error while closing the remote session.", true, true);
                WriteErrorLog("Error closing remote session", ex, false);
            }

            Dispatcher.Invoke(() =>
            {
                if (this.IsLoaded)
                    this.Close();
            });
        }

        private void RemoteSessionWindow_Closed(object sender, EventArgs e)
        {
            if (_isClosing)
            {
                return;
            }

            CloseSession();
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
                    ReadExact(_stream, lengthBuffer, 4);

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    int length = BitConverter.ToInt32(lengthBuffer, 0);
                    if (length <= 0)
                    {
                        throw new InvalidDataException("Received an invalid frame length.");
                    }

                    byte[] buffer = new byte[length];
                    ReadExact(_stream, buffer, length);

                    if (token.IsCancellationRequested)
                    {
                        break;
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
                                bitmap.Freeze();
                                VideoDisplay.Source = bitmap;
                            }

                            ShowStatus("Receiving video from the host.", false, false);
                        }
                        catch (Exception ex)
                        {
                            WriteErrorLog("Error decoding image", ex, false);
                        }
                    });
                }

                if (_isClosing)
                {
                    ShowStatus("Remote session disconnected.", false, false);
                }
            }
            catch (OperationCanceledException)
            {
                ShowStatus("Remote session disconnected.", false, false);
            }
            catch (IOException ex)
            {
                WriteErrorLog("Disconnected or error receiving video", ex, false);
                PromptReconnect();
            }
            catch (Exception ex)
            {
                WriteErrorLog("Unexpected remote session error", ex, false);
                PromptReconnect();
            }
        }

        private void PromptReconnect()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ShowStatus("Connection to the host was lost.", true, false);

                var result = MessageBox.Show("Connection lost. Reconnect?", "Disconnected", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    ShowStatus("Reconnect is not implemented yet.", true, false);
                    MessageBox.Show("Reconnect not implemented yet.", "Remote Session", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseSession();
                }
                else
                {
                    CloseSession();
                }
            });
        }

        private void ReadExact(NetworkStream stream, byte[] buffer, int length)
        {
            int totalRead = 0;
            while (totalRead < length)
            {
                int chunk = stream.Read(buffer, totalRead, length - totalRead);
                if (chunk == 0)
                {
                    throw new IOException("Stream ended unexpectedly.");
                }

                totalRead += chunk;
            }
        }

        private void WriteErrorLog(string context, Exception ex, bool showMessageBox)
        {
            string message = context + ": " + ex.Message;
            ShowStatus(message, true, showMessageBox);
        }

        private void ShowStatus(string message, bool isError, bool showMessageBox)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SessionStatusTextBlock.Text = message;
                SessionStatusTextBlock.Foreground = isError ? Brushes.DarkRed : Brushes.DarkGreen;
            });

            if (showMessageBox)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(message, isError ? "Remote Session Error" : "Remote Session", MessageBoxButton.OK, isError ? MessageBoxImage.Error : MessageBoxImage.Information);
                });
            }
        }
    }

}