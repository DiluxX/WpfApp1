using System;
using System.Data.Entity;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private Database database = new Database();

        public MainWindow()
        {
            InitializeComponent();

            // Инициализируем структуру БД
            database.InitializeDatabaseStructure();

            // Устанавливаем фокус на поле логина
            UsernameTextBox.Focus();
        }

        // Вход как гость
        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            OpenNewsMainWindow("guest", "Гость", 0);
        }

        // Вход с авторизацией
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Валидация
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

            // Блокируем кнопки во время проверки
            SetControlsEnabled(false);
            ErrorTextBlock.Text = "Проверка данных...";

            try
            {
                // Проверяем аутентификацию
                var result = database.AuthenticateUser(username, password);

                if (result.success)
                {
                    // Успешный вход
                    OpenNewsMainWindow(result.role, result.username, result.userId);
                }
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

        // Открытие окна новостей
        private void OpenNewsMainWindow(string role, string username, int userId)
        {
            var newsMainWindow = new news_main(role, username, userId);
            newsMainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            newsMainWindow.Show();
            this.Close();
        }

        // Показать ошибку
        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
        }

        // Блокировка/разблокировка элементов управления
        private void SetControlsEnabled(bool enabled)
        {
            LoginButton.IsEnabled = enabled;
            GuestButton.IsEnabled = enabled;
            UsernameTextBox.IsEnabled = enabled;
            PasswordBox.IsEnabled = enabled;
        }

        // Обработка нажатия Enter
        private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        private void UsernameTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                PasswordBox.Focus();
            }
        }
    }
}