using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfApp1;

namespace WpfApp1
{
    public partial class news_main : Window
    {
        private Database database = new Database();
        private NewsApiService newsApiService = new NewsApiService();
        private string currentRole;
        private string currentUsername;
        private int currentUserId;
        private Dictionary<string, bool> permissions;
        private int currentPage = 1;
        private int totalResults = 0;
        private bool isLoadingMore = false;
        private bool hasMorePages = true;
        private bool isSearchMode = false;   // true – режим поиска
        private string activeSearchQuery = "";

        // Полный загруженный пул — всегда "Все новости", фильтруем локально
        private List<NewsArticle> allNews = new List<NewsArticle>();

        // То, что сейчас показывается
        private List<NewsArticle> currentNews = new List<NewsArticle>();

        private string currentCategory = "Все новости";
        private string currentSearchQuery = "";

        private List<Button> categoryButtons = new List<Button>();

        private static readonly string[] Categories = new[]
        {
            "Все новости", "Политика", "Технологии", "Спорт",
            "Бизнес", "Здоровье", "Наука", "Культура", "Развлечения"
        };

        public news_main(string role, string username, int userId)
        {
            InitializeComponent();
            currentRole = role;
            currentUsername = username;
            currentUserId = userId;

            permissions = database.GetRolePermissions(currentRole);
            InitializeWindow();
        }

        public news_main() : this("guest", "Гость", 0) { }

        private async void InitializeWindow()
        {
            this.Title = $"Новостной портал — {currentUsername}";
            WelcomeTextBlock.Text = $"Добро пожаловать, {currentUsername}!";
            RoleTextBlock.Text = GetRoleDisplayName(currentRole);

            // Отображаем выбранный регион
            DisplaySelectedRegion();

            UpdateUIForRole();
            BuildCategoryButtons();

            // Загружаем весь пул новостей один раз
            await LoadAllNewsAsync();
        }

        private string GetRoleDisplayName(string role)
        {
            switch (role?.ToLower())
            {
                case "admin": return "👑 Администратор";
                case "manager": return "🔧 Менеджер";
                case "user": return "👤 Пользователь";
                default: return "🚶 Гость";
            }
        }

        // ═══════════════════════════════════════════════════════
        //  Отображение выбранного региона
        // ═══════════════════════════════════════════════════════

        private void DisplaySelectedRegion()
        {
            if (RegionSelectWindow.SelectedRegion != null)
            {
                var region = RegionSelectWindow.SelectedRegion;
                RegionTextBlock.Text = $"📍 {region.Name}";
                RegionBadge.Visibility = Visibility.Visible;
            }
            else
            {
                RegionBadge.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateUIForRole()
        {
            bool isAdmin = currentRole == "admin";
            bool isManager = currentRole == "manager";
            bool isGuest = currentRole == "guest";

            ManageUsersButton.Visibility = (isAdmin || isManager) ? Visibility.Visible : Visibility.Collapsed;
            ModerateCommentsButton.Visibility = (isAdmin || isManager) ? Visibility.Visible : Visibility.Collapsed;

            if (AddCommentButton != null)
                AddCommentButton.IsEnabled = !isGuest;
        }

        // ── Кнопки категорий ──────────────────────────────────────────────

        private void BuildCategoryButtons()
        {
            // Очищаем только кнопки, оставляем заголовок из XAML
            // (заголовок уже прописан в XAML, добавляем только кнопки)
            categoryButtons.Clear();

            // Удаляем старые кнопки если они есть (при повторном вызове)
            var toRemove = CategoriesPanel.Children.OfType<Button>().ToList();
            foreach (var b in toRemove)
                CategoriesPanel.Children.Remove(b);

            foreach (var category in Categories)
            {
                var btn = new Button
                {
                    Content = category,
                    Tag = category,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };

                btn.Style = category == currentCategory
                    ? (Style)FindResource("ActiveCategoryButtonStyle")
                    : (Style)FindResource("CategoryButtonStyle");

                btn.Click += CategoryButton_Click;
                categoryButtons.Add(btn);
                CategoriesPanel.Children.Add(btn);
            }
        }

        private void UpdateActiveCategoryButton(string activeCategory)
        {
            foreach (var btn in categoryButtons)
            {
                btn.Style = (btn.Tag?.ToString() == activeCategory)
                    ? (Style)FindResource("ActiveCategoryButtonStyle")
                    : (Style)FindResource("CategoryButtonStyle");
            }
        }

        private async void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Если мы в режиме поиска – выходим из него и загружаем обычную ленту
            if (isSearchMode)
            {
                ClearSearchMode();
                await LoadAllNewsAsync(true);
            }

            if (!(sender is Button btn) || !(btn.Tag is string selectedCategory))
                return;

            currentCategory = selectedCategory;
            UpdateActiveCategoryButton(selectedCategory);
            CurrentCategoryTextBlock.Text = selectedCategory;
            ApplyFilters();
        }

        // ── Загрузка данных ───────────────────────────────────────────────

        /// <summary>
        /// Загружает полный пул новостей с сервиса (или fallback),
        /// затем применяет текущие фильтры.
        /// </summary>
        private async Task LoadAllNewsAsync(bool resetPagination = true)
        {
            try
            {
                if (resetPagination)
                {
                    currentPage = 1;
                    allNews.Clear();
                    hasMorePages = true;
                }

                ShowProgress(true);
                HideArticleDetails();
                StatusTextBlock.Text = "Загрузка новостей...";

                var result = await newsApiService.GetTopHeadlinesPageAsync("ru", null, currentPage, 20);
                if (resetPagination)
                    allNews = result.Articles;
                else
                    allNews.AddRange(result.Articles);

                totalResults = result.TotalResults;
                hasMorePages = allNews.Count < totalResults;

                EnsureCategoriesAssigned();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                ShowProgress(false);
                StatusTextBlock.Text = "Готово";
                isLoadingMore = false;
            }
        }

        // Новый метод подгрузки
        private async Task LoadMoreNewsAsync()
        {
            if (isLoadingMore || !hasMorePages) return;
            isLoadingMore = true;
            currentPage++;

            try
            {
                if (isSearchMode && !string.IsNullOrEmpty(activeSearchQuery))
                {
                    // Подгрузка результатов поиска
                    var result = await newsApiService.SearchNewsPageAsync(activeSearchQuery, "ru", currentPage, 20);
                    if (result.Articles.Count > 0)
                    {
                        allNews.AddRange(result.Articles);
                        ApplyFilters();
                    }
                    hasMorePages = allNews.Count < result.TotalResults;
                    totalResults = result.TotalResults;
                }
                else
                {
                    // Обычная подгрузка из ленты (как было)
                    var result = await newsApiService.GetTopHeadlinesPageAsync("ru", null, currentPage, 20);
                    if (result.Articles.Count > 0)
                    {
                        allNews.AddRange(result.Articles);
                        EnsureCategoriesAssigned();
                        ApplyFilters();
                    }
                    hasMorePages = allNews.Count < result.TotalResults;
                    totalResults = result.TotalResults;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка подгрузки: {ex.Message}");
                currentPage--;
            }
            finally
            {
                isLoadingMore = false;
            }
        }

        // Обработчик прокрутки (привяжите в XAML)
        private void NewsScrollViewer_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 50)
            {
                if (hasMorePages && !isLoadingMore)
                {
                    _ = LoadMoreNewsAsync();
                }
            }
        }

        // Новый метод для подгрузки следующей страницы
       
        /// <summary>
        /// Если все статьи пришли с одинаковой категорией "Новости" (из API)
        /// или вообще без категории — распределяем их по категориям циклически,
        /// чтобы фильтрация работала даже на fallback-данных.
        /// </summary>
        private void EnsureCategoriesAssigned()
        {
            var contentCategories = Categories.Skip(1).ToArray(); // без "Все новости"

            // Проверяем: если все статьи имеют одну и ту же (или пустую) категорию
            bool needsAssignment = allNews.All(a =>
                string.IsNullOrEmpty(a.Category) ||
                string.Equals(a.Category, "Новости", StringComparison.OrdinalIgnoreCase));

            if (needsAssignment)
            {
                for (int i = 0; i < allNews.Count; i++)
                    allNews[i].Category = contentCategories[i % contentCategories.Length];
            }
        }

        /// <summary>
        /// Применяет категорию и строку поиска к allNews,
        /// обновляет NewsItemsControl. Никаких сетевых запросов.
        /// </summary>
        private void ApplyFilters()
        {
            var filtered = allNews.AsEnumerable();

            // Фильтр по категории
            if (currentCategory != "Все новости")
            {
                filtered = filtered.Where(a =>
                    string.Equals(a.Category, currentCategory, StringComparison.OrdinalIgnoreCase));
            }

            // Фильтр по строке поиска
            if (!string.IsNullOrWhiteSpace(currentSearchQuery))
            {
                string q = currentSearchQuery.ToLowerInvariant();
                filtered = filtered.Where(a =>
                    (a.Title != null && a.Title.ToLowerInvariant().Contains(q)) ||
                    (a.Description != null && a.Description.ToLowerInvariant().Contains(q)) ||
                    (a.Content != null && a.Content.ToLowerInvariant().Contains(q)) ||
                    (a.Source != null && a.Source.ToLowerInvariant().Contains(q)) ||
                    (a.Author != null && a.Author.ToLowerInvariant().Contains(q)));
            }

            currentNews = filtered.ToList();

            NewsItemsControl.ItemsSource = null;
            NewsItemsControl.ItemsSource = currentNews;

            int count = currentNews.Count;
            NewsCountTextBlock.Text = count > 0
                ? $"{count} {GetNounForm(count, "новость", "новости", "новостей")}"
                : "Ничего не найдено";

            // Заголовок над списком
            if (CurrentCategoryTextBlock != null)
            {
                if (!string.IsNullOrWhiteSpace(currentSearchQuery) && currentCategory != "Все новости")
                    CurrentCategoryTextBlock.Text = $"{currentCategory} · «{currentSearchQuery}»";
                else if (!string.IsNullOrWhiteSpace(currentSearchQuery))
                    CurrentCategoryTextBlock.Text = $"Поиск: «{currentSearchQuery}»";
                else
                    CurrentCategoryTextBlock.Text = currentCategory;
            }
        }

        private void ShowProgress(bool show)
        {
            ProgressBar.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            LoadButton.IsEnabled = !show;
        }

        private void HideArticleDetails()
        {
            ArticleScrollViewer.Visibility = Visibility.Collapsed;
            EmptyDetailPanel.Visibility = Visibility.Visible;
        }

        // ── Карточка новости ──────────────────────────────────────────────

        private void NewsCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is NewsArticle article)
                ShowArticleDetails(article);
        }

        private void ShowArticleDetails(NewsArticle article)
        {
            ArticleTitleTextBlock.Text = article.Title ?? "";
            ArticleCategoryTextBlock.Text = article.Category ?? "Новости";
            ArticleSourceTextBlock.Text = article.Source ?? "Неизвестно";
            ArticleDateTextBlock.Text = article.FormattedDate;
            ArticleAuthorTextBlock.Text = article.Author ?? "Автор не указан";
            ArticleDescriptionTextBlock.Text = article.Description ?? "Нет описания";
            ArticleContentTextBlock.Text = article.Content ?? "";

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
                    ArticleImageBorder.Visibility = Visibility.Visible;
                }
                catch
                {
                    ArticleImage.Source = null;
                    ArticleImageBorder.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                ArticleImage.Source = null;
                ArticleImageBorder.Visibility = Visibility.Collapsed;
            }

            EmptyDetailPanel.Visibility = Visibility.Collapsed;
            ArticleScrollViewer.Visibility = Visibility.Visible;
            ArticleScrollViewer.ScrollToTop();
        }

        // ── Обработчики кнопок ────────────────────────────────────────────

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSearchMode)
            {
                ClearSearchMode();
                await LoadAllNewsAsync(true);
                UpdateActiveCategoryButton(currentCategory);
            }
            else
            {
                await LoadAllNewsAsync(true);
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchTextBox.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Введите текст для поиска", "Поиск", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Переключаемся в режим поиска
            isSearchMode = true;
            activeSearchQuery = query;
            currentSearchQuery = query;   // для отображения в заголовке
            currentPage = 1;
            hasMorePages = true;

            ShowProgress(true);
            StatusTextBlock.Text = $"Поиск: {query}...";

            try
            {
                var result = await newsApiService.SearchNewsPageAsync(query, "ru", currentPage, 20);
                allNews = result.Articles;
                totalResults = result.TotalResults;
                hasMorePages = allNews.Count < totalResults;

                ApplyFilters(); // Обновит ItemsControl
                HideArticleDetails();

                // Обновляем заголовок
                CurrentCategoryTextBlock.Text = $"Поиск: «{query}»";
                NewsCountTextBlock.Text = $"{allNews.Count} из {totalResults} результатов";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                ShowProgress(false);
                StatusTextBlock.Text = "Готово";
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchButton_Click(sender, e);
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            currentSearchQuery = "";
            UpdateActiveCategoryButton(currentCategory);
            ApplyFilters();
        }
        private void ClearSearchMode()
        {
            isSearchMode = false;
            activeSearchQuery = "";
            currentSearchQuery = "";
            SearchTextBox.Text = "";
            currentPage = 1;
            hasMorePages = true;
        }
        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentRole == "guest" || currentUserId == 0)
            {
                MessageBox.Show("Просмотр профиля доступен только авторизованным пользователям.",
                    "Доступ ограничен", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var profileWindow = new ProfileWindow(currentUserId);
            profileWindow.Owner = this;
            profileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            profileWindow.ShowDialog();
        }

        /// <summary>
        /// Смена региона — возврат к окну выбора местности
        /// </summary>
        private void ChangeRegionButton_Click(object sender, RoutedEventArgs e)
        {
            RegionSelectWindow.SelectedRegion = null;
            var regionSelectWindow = new RegionSelectWindow(currentRole, currentUsername, currentUserId);
            regionSelectWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            regionSelectWindow.Show();
            this.Close();
        }

        private void OpenInBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ArticleTitleTextBlock.Text)) return;

            var article = currentNews.Find(a => a.Title == ArticleTitleTextBlock.Text);
            if (article == null) return;

            if (!string.IsNullOrEmpty(article.Url))
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
            MessageBox.Show("Функция управления пользователями будет реализована позже.", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ModerateCommentsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция модерации комментариев будет реализована позже.", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddCommentButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CommentTextBox.Text))
            {
                MessageBox.Show("Введите текст комментария.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Комментарий будет сохранен после реализации функции.", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
            CommentTextBox.Text = "";
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Очищаем выбранный регион при выходе
            RegionSelectWindow.SelectedRegion = null;

            var mainWindow = new MainWindow();
            mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            mainWindow.Show();
            this.Close();
        }

        public void UpdateUserInfo(string displayName)
        {
            if (!string.IsNullOrEmpty(displayName))
            {
                WelcomeTextBlock.Text = $"Добро пожаловать, {displayName}!";
                currentUsername = displayName;
            }
        }

        // ── Вспомогательное ──────────────────────────────────────────────

        private static string GetNounForm(int n, string one, string few, string many)
        {
            int m10 = n % 10, m100 = n % 100;
            if (m100 >= 11 && m100 <= 14) return $"{n} {many}";
            if (m10 == 1) return $"{n} {one}";
            if (m10 >= 2 && m10 <= 4) return $"{n} {few}";
            return $"{n} {many}";
        }
    }
}