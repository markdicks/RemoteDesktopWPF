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
    /// Interaction logic for RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        private Window _mainWindow;
        public RegisterWindow(Window mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            this.Closed += OnWindowClosed;
        }
        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            var userService = new UserService();

            var newUser = new CreateUserDto
            {
                UserName = UserNameBox.Text,
                Email = EmailBox.Text,
                Password = PasswordBox.Password,
                UserProfile = new UserProfileDto
                {
                    FirstName = FirstNameBox.Text,
                    LastName = LastNameBox.Text,
                    Address = AddressBox.Text
                }
            };

            var result = await userService.RegisterUserAsync(newUser);

            if (result.Success)
            {
                MessageBox.Show("Registration successful!");

                var loginWindow = new LoginWindow(_mainWindow);
                loginWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show(result.ErrorMessage ?? "Registration failed.", "Error", MessageBoxButton.OK , MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
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

    }
}
