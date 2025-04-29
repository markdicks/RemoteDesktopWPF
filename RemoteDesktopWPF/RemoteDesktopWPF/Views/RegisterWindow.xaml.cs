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
        public RegisterWindow()
        {
            InitializeComponent();
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

            var success = await userService.RegisterUserAsync(newUser);

            if (success)
                MessageBox.Show("Registration successful!");
            else
                MessageBox.Show("Registration failed.");
        }

    }
}
