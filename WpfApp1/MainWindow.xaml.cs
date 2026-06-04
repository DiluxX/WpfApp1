using System;
using System.Windows;
using System.Windows.Input;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private Database database = new Database();

        public MainWindow()
        {
            InitializeComponent();
            database.InitializeDatabaseStructure();
            UsernameTextBox.Focus();
        }

        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectionWindow("guest", "Гость", 0);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username))
            {
                ShowError("Введите имя пользователя");
                UsernameTextBox.Focus();
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                ShowError("Введите пароль");
                PasswordBox.Focus();
                return;
            }

            SetControlsEnabled(false);
            ShowError("Проверка данных...", false);

            try
            {
                var result = database.AuthenticateUser(username, password);
                if (result.success)
                    OpenSelectionWindow(result.role, result.username, result.userId);
                else
                {
                    ShowError("Неверное имя пользователя или пароль");
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка подключения: {ex.Message}");
            }
            finally
            {
                SetControlsEnabled(true);
            }
        }

        private void OpenSelectionWindow(string role, string username, int userId)
        {
            var win = new SelectionWindow(role, username, userId);
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();
            Close();
        }

        private void ShowError(string message, bool isError = true)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Foreground = isError
                ? System.Windows.Media.Brushes.IndianRed
                : System.Windows.Media.Brushes.Gray;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void SetControlsEnabled(bool enabled)
        {
            LoginButton.IsEnabled = enabled;
            GuestButton.IsEnabled = enabled;
            UsernameTextBox.IsEnabled = enabled;
            PasswordBox.IsEnabled = enabled;
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) LoginButton_Click(sender, e);
        }

        private void UsernameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) PasswordBox.Focus();
        }
    }
}