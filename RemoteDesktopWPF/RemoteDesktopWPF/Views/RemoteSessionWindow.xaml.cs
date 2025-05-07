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
            _cts.Cancel();
            _stream?.Close();
            _client?.Close();
            this.Close();
        }

        private void ReceiveVideo(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    byte[] lengthBuffer = new byte[4];
                    _stream.Read(lengthBuffer, 0, 4);
                    int length = BitConverter.ToInt32(lengthBuffer, 0);

                    byte[] buffer = new byte[length];
                    int totalRead = 0;
                    while (totalRead < length)
                        totalRead += _stream.Read(buffer, totalRead, length - totalRead);

                    Application.Current.Dispatcher.Invoke(() =>
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
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Image load error: " + ex.Message);
                throw new Exception(ex.Message);
            }
        }
    }
}