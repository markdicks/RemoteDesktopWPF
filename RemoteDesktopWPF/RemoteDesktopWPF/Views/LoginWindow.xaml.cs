using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public LoginWindow()
        {
            InitializeComponent();
        }
        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            var userService = new UserService();
            if (!long.TryParse(UserIdBox.Text, out long id))
            {
                MessageBox.Show("Invalid user ID");
                return;
            }

            var user = await userService.GetUserAsync(id);

            if (user != null && user.Password == PasswordBox.Password)
            {
                MessageBox.Show($"Welcome {user.UserName}!");
                // Proceed to open RemoteDesktopWindow
            }
            else
            {
                MessageBox.Show("Login failed.");
            }
        }

    }
}
