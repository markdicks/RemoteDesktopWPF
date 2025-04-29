using System.Windows;

namespace RemoteDesktopWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new Views.LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new Views.RegisterWindow();
            registerWindow.Show();
            this.Close();
        }
    }
}
