using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WpfApp1;

namespace WpfApp1
{
    public partial class ProfileWindow : Window
    {
        private Database _database = new Database();
        private int _userId;
        private UserProfile _currentProfile;
        private string _newAvatarPath;

        public ProfileWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadUserProfile();
        }

        private void LoadUserProfile()
        {
            _currentProfile = _database.GetUserProfile(_userId);

            if (_currentProfile != null)
            {
                // Заполняем поля
                UsernameTextBox.Text = _currentProfile.Username;
                DisplayNameTextBox.Text = _currentProfile.DisplayName;
                EmailTextBox.Text = _currentProfile.Email;
                BioTextBox.Text = _currentProfile.Bio;
                RoleTextBlock.Text = _currentProfile.Role;

                // Форматируем даты
                CreatedAtTextBlock.Text = _currentProfile.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                LastLoginTextBlock.Text = _currentProfile.LastLogin.ToString("dd.MM.yyyy HH:mm");

                // Загружаем аватар
                LoadAvatar(_currentProfile.AvatarUrl);
            }
            else
            {
                ShowError("Не удалось загрузить профиль пользователя");
                Close();
            }
        }

        private void LoadAvatar(string avatarUrl)
        {
            try
            {
                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    if (avatarUrl.StartsWith("http"))
                    {
                        // Загрузка из интернета
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(avatarUrl, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        AvatarImage.Source = bitmap;
                    }
                    else if (avatarUrl.StartsWith("avatars/"))
                    {
                        // Загрузка локального файла из папки приложения
                        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string fullPath = System.IO.Path.Combine(appDataPath, "NewsPortal", avatarUrl);

                        if (System.IO.File.Exists(fullPath))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            AvatarImage.Source = bitmap;
                            return;
                        }
                    }
                    else if (System.IO.File.Exists(avatarUrl))
                    {
                        // Загрузка локального файла по полному пути
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(avatarUrl, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        AvatarImage.Source = bitmap;
                        return;
                    }
                }

                // Если аватар не загружен, устанавливаем стандартный
                SetDefaultAvatar();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки аватара: {ex.Message}");
                SetDefaultAvatar();
            }
        }

        private void SetDefaultAvatar()
        {
            try
            {
                // Пробуем загрузить из ресурсов
                AvatarImage.Source = new BitmapImage(
                    new Uri("pack://application:,,,/Resources/default-avatar.png"));
            }
            catch
            {
                // Создаем простой аватар программно
                var drawingVisual = new System.Windows.Media.DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawEllipse(
                        System.Windows.Media.Brushes.LightGray,
                        new System.Windows.Media.Pen(System.Windows.Media.Brushes.DarkGray, 2),
                        new System.Windows.Point(75, 75),
                        70, 70);

                    drawingContext.DrawText(
                        new System.Windows.Media.FormattedText(
                            "Аватар",
                            System.Globalization.CultureInfo.CurrentCulture,
                            System.Windows.FlowDirection.LeftToRight,
                            new System.Windows.Media.Typeface("Arial"),
                            20,
                            System.Windows.Media.Brushes.Gray),
                        new System.Windows.Point(40, 65));
                }

                var bitmap = new RenderTargetBitmap(150, 150, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                bitmap.Render(drawingVisual);
                AvatarImage.Source = bitmap;
            }
        }

        // Обработчики событий для аватара
        private void AvatarBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            AvatarOverlay.Visibility = Visibility.Visible;
        }

        private void AvatarBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            AvatarOverlay.Visibility = Visibility.Hidden;
        }

        private void ChangeAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.jpg; *.jpeg; *.png; *.bmp)|*.jpg; *.jpeg; *.png; *.bmp",
                Title = "Выберите изображение для аватара"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _newAvatarPath = openFileDialog.FileName;

                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_newAvatarPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    AvatarImage.Source = bitmap;

                    ShowError("", false); // Скрываем ошибку
                }
                catch (Exception ex)
                {
                    ShowError($"Не удалось загрузить изображение: {ex.Message}");
                }
            }
        }

        private void RemoveAvatarButton_Click(object sender, RoutedEventArgs e)
        {
            _newAvatarPath = "";
            SetDefaultAvatar();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(DisplayNameTextBox.Text))
            {
                ShowError("Введите отображаемое имя");
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                ShowError("Введите email");
                return;
            }

            if (!IsValidEmail(EmailTextBox.Text))
            {
                ShowError("Введите корректный email");
                return;
            }

            // Проверка паролей
            bool changingPassword = !string.IsNullOrEmpty(NewPasswordBox.Password);

            if (changingPassword)
            {
                if (string.IsNullOrEmpty(CurrentPasswordBox.Password))
                {
                    ShowError("Для смены пароля введите текущий пароль");
                    return;
                }

                if (NewPasswordBox.Password.Length < 6)
                {
                    ShowError("Новый пароль должен содержать минимум 6 символов");
                    return;
                }

                if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    ShowError("Новые пароли не совпадают");
                    return;
                }
            }

            // Подготавливаем запрос
            var request = new UpdateProfileRequest
            {
                DisplayName = DisplayNameTextBox.Text.Trim(),
                Bio = BioTextBox.Text.Trim(),
                AvatarUrl = _newAvatarPath,
                CurrentPassword = CurrentPasswordBox.Password,
                NewPassword = changingPassword ? NewPasswordBox.Password : null
            };

            // Сохраняем изменения
            bool success = _database.UpdateUserProfile(_userId, request);

            if (success)
            {
                MessageBox.Show("Профиль успешно обновлен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Очищаем поля паролей
                CurrentPasswordBox.Clear();
                NewPasswordBox.Clear();
                ConfirmPasswordBox.Clear();

                // Перезагружаем профиль
                LoadUserProfile();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void ShowError(string message, bool show = true)
        {
            if (show)
            {
                ErrorBorder.Visibility = Visibility.Visible;
                ErrorTextBlock.Text = message;
            }
            else
            {
                ErrorBorder.Visibility = Visibility.Collapsed;
            }
        }

        // Обработчики событий для сброса ошибок
        private void DisplayNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ShowError("", false);
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ShowError("", false);
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ShowError("", false);
        }
    }
}