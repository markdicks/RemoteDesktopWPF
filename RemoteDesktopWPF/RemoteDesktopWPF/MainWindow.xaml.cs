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
            var loginWindow = new Views.LoginWindow(this);
            loginWindow.Show();
            this.Hide();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new Views.RegisterWindow(this);
            registerWindow.Show();
            this.Hide();
        }
    }
}
