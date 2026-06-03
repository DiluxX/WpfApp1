using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WpfApp1;

namespace WpfApp1
{
    public partial class news_main : Window
    {
        private Database database = new Database();
        private NewsApiService newsApiService = new NewsApiService(); // Изменил на NewsApiService
        private string currentRole;
        private string currentUsername;
        private int currentUserId;
        private Dictionary<string, bool> permissions;
        private List<NewsArticle> currentNews = new List<NewsArticle>();
        private string currentCategory = "Все новости";

        public news_main(string role, string username, int userId)
        {
            InitializeComponent();
            currentRole = role;
            currentUsername = username;
            currentUserId = userId;

            permissions = database.GetRolePermissions(currentRole);

            // Автоматическая загрузка при создании окна
            InitializeWindow();
        }

        public news_main() : this("guest", "Гость", 0)
        {
        }

        private async void InitializeWindow()
        {
            this.Title = $"Новостной портал - {currentUsername} ({currentRole})";
            WelcomeTextBlock.Text = $"Добро пожаловать, {currentUsername}!";
            RoleTextBlock.Text = $"Роль: {currentRole}";

            UpdateUIForRole();
            LoadCategories();

            // Автоматическая загрузка при старте
            await LoadNewsAsync("Все новости");
        }

        private void UpdateUIForRole()
        {
            if (currentRole == "admin")
            {
                ManageUsersButton.Visibility = Visibility.Visible;
                ModerateCommentsButton.Visibility = Visibility.Visible;
            }
            else if (currentRole == "manager")
            {
                ManageUsersButton.Visibility = Visibility.Visible;
                ModerateCommentsButton.Visibility = Visibility.Visible;
            }
            else if (currentRole == "user")
            {

                ManageUsersButton.Visibility = Visibility.Collapsed;
                ModerateCommentsButton.Visibility = Visibility.Collapsed;
                if (AddCommentButton != null)
                    AddCommentButton.IsEnabled = true;
            }
            else // guest
            {

                ManageUsersButton.Visibility = Visibility.Collapsed;
                ModerateCommentsButton.Visibility = Visibility.Collapsed;
                if (AddCommentButton != null)
                    AddCommentButton.IsEnabled = false;
            }
        }

        private void LoadCategories()
        {
            if (CategoriesListBox == null) return;

            var categories = new List<string>
            {
                "Все новости",
                "Политика",
                "Технологии",
                "Спорт",
                "Бизнес",
                "Здоровье",
                "Наука",
                "Культура",
                "Развлечения"
            };

            CategoriesListBox.ItemsSource = categories;
            CategoriesListBox.SelectedIndex = 0;
        }

        private async void CategoriesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoriesListBox.SelectedItem is string selectedCategory && !string.IsNullOrEmpty(selectedCategory))
            {
                currentCategory = selectedCategory;
                await LoadNewsAsync(selectedCategory);
            }
        }

        private async Task LoadNewsAsync(string category)
        {
            try
            {
                // Показываем индикатор
                if (ProgressBar != null)
                {
                    ProgressBar.Visibility = Visibility.Visible;
                    ProgressBar.IsIndeterminate = true;
                }

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = $"Загрузка {category}...";

                if (LoadButton != null)
                    LoadButton.IsEnabled = false;

                // Загружаем новости
                currentNews = await newsApiService.GetTopHeadlinesAsync("ru",
                    category == "Все новости" ? null : category,
                    15);

                // Обновляем UI
                if (NewsListView != null)
                {
                    NewsListView.ItemsSource = null; // Сбрасываем
                    NewsListView.ItemsSource = currentNews;
                    NewsListView.SelectedIndex = -1;
                }

                if (NewsCountTextBlock != null)
                    NewsCountTextBlock.Text = $"Загружено: {currentNews.Count}";

                if (ArticleDetailsPanel != null)
                    ArticleDetailsPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить новости: {ex.Message}\nПоказаны демонстрационные данные",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

                // Получаем локальные данные из сервиса
                currentNews = await newsApiService.GetTopHeadlinesAsync("ru", category, 15);

                if (NewsListView != null)
                {
                    NewsListView.ItemsSource = currentNews;
                }

                if (NewsCountTextBlock != null)
                    NewsCountTextBlock.Text = $"Демо-данные: {currentNews.Count}";
            }
            finally
            {
                // Скрываем индикатор
                if (ProgressBar != null)
                {
                    ProgressBar.Visibility = Visibility.Collapsed;
                    ProgressBar.IsIndeterminate = false;
                }

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = "Готово";

                if (LoadButton != null)
                    LoadButton.IsEnabled = true;
            }
        }
        // Метод для обновления информации пользователя
        public void UpdateUserInfo(string displayName)
        {
            if (!string.IsNullOrEmpty(displayName))
            {
                WelcomeTextBlock.Text = $"Добро пожаловать, {displayName}!";
                currentUsername = displayName;
            }
        }

        // Обработчик кнопки профиля
        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = new ProfileWindow(currentUserId);
            profileWindow.Owner = this;
            profileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            profileWindow.ShowDialog();
        }
        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox != null)
            {
                SearchTextBox.Text = "";
                LoadNewsAsync(currentCategory);
            }
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadNewsAsync(currentCategory);
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("Введите текст для поиска", "Поиск",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (ProgressBar != null)
                    ProgressBar.Visibility = Visibility.Visible;

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = $"Поиск: {searchText}...";

                if (LoadButton != null)
                    LoadButton.IsEnabled = false;

                // Используем метод поиска из сервиса
                currentNews = await newsApiService.SearchNewsAsync(searchText, "ru", 20);

                if (NewsListView != null)
                {
                    NewsListView.ItemsSource = currentNews;
                    NewsListView.SelectedIndex = -1;
                }

                if (NewsCountTextBlock != null)
                    NewsCountTextBlock.Text = $"Найдено: {currentNews.Count}";

                if (ArticleDetailsPanel != null)
                    ArticleDetailsPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (ProgressBar != null)
                    ProgressBar.Visibility = Visibility.Collapsed;

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = "Готово";

                if (LoadButton != null)
                    LoadButton.IsEnabled = true;
            }
        }

        private void NewsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NewsListView.SelectedItem is NewsArticle selectedArticle)
            {
                ShowArticleDetails(selectedArticle);
            }
        }

        private void ShowArticleDetails(NewsArticle article)
        {
            if (ArticleTitleTextBlock != null)
                ArticleTitleTextBlock.Text = article.Title;

            if (ArticleSourceTextBlock != null)
                ArticleSourceTextBlock.Text = $"Источник: {article.Source}";

            if (ArticleDateTextBlock != null)
                ArticleDateTextBlock.Text = $"Дата: {article.FormattedDate}";

            if (ArticleAuthorTextBlock != null)
                ArticleAuthorTextBlock.Text = $"Автор: {article.Author ?? "Не указан"}";

            if (ArticleDescriptionTextBlock != null)
                ArticleDescriptionTextBlock.Text = article.Description ?? "Нет описания";



            if (ArticleImage != null)
            {
                if (!string.IsNullOrEmpty(article.ImageUrl))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(article.ImageUrl, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        ArticleImage.Source = bitmap;
                        ArticleImage.Visibility = Visibility.Visible;
                    }
                    catch
                    {
                        ArticleImage.Source = null;
                        ArticleImage.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    ArticleImage.Source = null;
                    ArticleImage.Visibility = Visibility.Collapsed;
                }
            }

            if (ArticleDetailsPanel != null)
                ArticleDetailsPanel.Visibility = Visibility.Visible;
        }

        private void OpenInBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            if (NewsListView.SelectedItem is NewsArticle article && !string.IsNullOrEmpty(article.Url))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = article.Url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void ManageUsersButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция управления пользователями будет реализована позже", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ModerateCommentsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция модерации комментариев будет реализована позже", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddCommentButton_Click(object sender, RoutedEventArgs e)
        {
            if (CommentTextBox == null) return;

            if (string.IsNullOrEmpty(CommentTextBox.Text))
            {
                MessageBox.Show("Введите текст комментария", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Комментарий будет сохранен после реализации функции", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
            CommentTextBox.Text = "";
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            mainWindow.Show();
            this.Close();
        }
    }
}