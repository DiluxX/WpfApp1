using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        // Пагинация
        private int currentPage = 1;
        private int totalResults = 0;
        private bool isLoadingMore = false;
        private bool hasMorePages = true;

        // Режим поиска
        private bool isSearchMode = false;
        private string activeSearchQuery = "";

        // Режим региональных новостей
        private bool isRegionalMode = false;
        private string activeRegion = "";

        // Данные
        private List<NewsArticle> allNews = new List<NewsArticle>();
        private List<NewsArticle> currentNews = new List<NewsArticle>();

        // Текущие фильтры
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

            DisplaySelectedRegion();
            UpdateUIForRole();
            BuildCategoryButtons();

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

        // ── Регион ────────────────────────────────────────────────────────

        private void DisplaySelectedRegion()
        {
            var region = RegionSelectWindow.SelectedRegion;
            if (region != null)
            {
                RegionTextBlock.Text = $"📍 {region.Name}";
                RegionBadge.Visibility = Visibility.Visible;
            }
            else
            {
                RegionBadge.Visibility = Visibility.Collapsed;
            }
        }

        // ── Роли ──────────────────────────────────────────────────────────

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
            categoryButtons.Clear();
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

            // Кнопка региональных новостей (если выбран регион)
            var selectedRegion = RegionSelectWindow.SelectedRegion;
            if (selectedRegion != null)
            {
                var regionalBtn = new Button
                {
                    Content = $"📍 {selectedRegion.Name}",
                    Tag = $"__region__{selectedRegion.Name}",
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                regionalBtn.Style = (Style)FindResource("CategoryButtonStyle");
                regionalBtn.Click += RegionalNewsButton_Click;
                categoryButtons.Add(regionalBtn);
                CategoriesPanel.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)),
                    Margin = new Thickness(0, 4, 0, 4)
                });
                CategoriesPanel.Children.Add(regionalBtn);
            }
        }

        private void UpdateActiveCategoryButton(string activeCategory)
        {
            foreach (var btn in categoryButtons)
            {
                string tag = btn.Tag?.ToString() ?? "";
                bool isActive = tag == activeCategory ||
                                  (activeCategory != null && tag == $"__region__{activeCategory}");
                btn.Style = isActive
                    ? (Style)FindResource("ActiveCategoryButtonStyle")
                    : (Style)FindResource("CategoryButtonStyle");
            }
        }

        private async void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSearchMode) { ClearSearchMode(); }
            if (isRegionalMode) { ClearRegionalMode(); }

            if (!(sender is Button btn) || !(btn.Tag is string selectedCategory)) return;

            currentCategory = selectedCategory;
            UpdateActiveCategoryButton(selectedCategory);
            CurrentCategoryTextBlock.Text = selectedCategory;

            // Если при смене категории пула нет — перезагружаем
            if (allNews.Count == 0)
                await LoadAllNewsAsync();
            else
                ApplyFilters();
        }

        private async void RegionalNewsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;

            string tag = btn.Tag?.ToString() ?? "";
            string regionName = tag.Replace("__region__", "");

            isRegionalMode = true;
            activeRegion = regionName;
            currentCategory = "Все новости";
            if (isSearchMode) ClearSearchMode();

            UpdateActiveCategoryButton($"__region__{regionName}");
            CurrentCategoryTextBlock.Text = $"📍 {regionName}";

            await LoadRegionalNewsAsync(regionName);
        }

        // ── Загрузка данных ───────────────────────────────────────────────

        private async Task LoadAllNewsAsync(bool resetPagination = true)
        {
            try
            {
                if (resetPagination) { currentPage = 1; allNews.Clear(); hasMorePages = true; }

                ShowProgress(true);
                HideArticleDetails();
                StatusTextBlock.Text = "Загрузка новостей...";

                var result = await newsApiService.GetTopHeadlinesPageAsync("ru", null, currentPage, 20);
                if (resetPagination) allNews = result.Articles;
                else allNews.AddRange(result.Articles);

                totalResults = result.TotalResults;
                hasMorePages = allNews.Count < totalResults;

                EnsureCategoriesAssigned();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                ShowProgress(false);
                StatusTextBlock.Text = "Готово";
                isLoadingMore = false;
            }
        }

        private async Task LoadRegionalNewsAsync(string regionName, bool reset = true)
        {
            try
            {
                if (reset) { currentPage = 1; allNews.Clear(); hasMorePages = true; }

                ShowProgress(true);
                HideArticleDetails();
                StatusTextBlock.Text = $"Загрузка новостей: {regionName}...";

                var result = await newsApiService.GetTopHeadlinesPageAsync(
                    "ru", null, currentPage, 20, regionName);

                if (reset) allNews = result.Articles;
                else allNews.AddRange(result.Articles);

                totalResults = result.TotalResults;
                hasMorePages = allNews.Count < totalResults;

                ApplyFilters();
                CurrentCategoryTextBlock.Text = $"📍 {regionName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки региональных новостей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                ShowProgress(false);
                StatusTextBlock.Text = "Готово";
                isLoadingMore = false;
            }
        }

        private async Task LoadMoreNewsAsync()
        {
            if (isLoadingMore || !hasMorePages) return;
            isLoadingMore = true;
            currentPage++;

            try
            {
                if (isSearchMode && !string.IsNullOrEmpty(activeSearchQuery))
                {
                    var result = await newsApiService.SearchNewsPageAsync(activeSearchQuery, "ru", currentPage, 20);
                    if (result.Articles.Count > 0) { allNews.AddRange(result.Articles); ApplyFilters(); }
                    hasMorePages = allNews.Count < result.TotalResults;
                    totalResults = result.TotalResults;
                }
                else if (isRegionalMode && !string.IsNullOrEmpty(activeRegion))
                {
                    var result = await newsApiService.GetTopHeadlinesPageAsync("ru", null, currentPage, 20, activeRegion);
                    if (result.Articles.Count > 0) { allNews.AddRange(result.Articles); ApplyFilters(); }
                    hasMorePages = allNews.Count < result.TotalResults;
                    totalResults = result.TotalResults;
                }
                else
                {
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
            finally { isLoadingMore = false; }
        }

        private void NewsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 100)
                if (hasMorePages && !isLoadingMore)
                    _ = LoadMoreNewsAsync();
        }

        // ── Фильтры ───────────────────────────────────────────────────────

        private void EnsureCategoriesAssigned()
        {
            var contentCats = Categories.Skip(1).ToArray();
            bool needsAssignment = allNews.All(a =>
                string.IsNullOrEmpty(a.Category) ||
                string.Equals(a.Category, "Новости", StringComparison.OrdinalIgnoreCase));

            if (needsAssignment)
                for (int i = 0; i < allNews.Count; i++)
                    allNews[i].Category = contentCats[i % contentCats.Length];
        }

        private void ApplyFilters()
        {
            var filtered = allNews.AsEnumerable();

            if (!isRegionalMode && currentCategory != "Все новости")
                filtered = filtered.Where(a =>
                    string.Equals(a.Category, currentCategory, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(currentSearchQuery))
            {
                string q = currentSearchQuery.ToLowerInvariant();
                filtered = filtered.Where(a =>
                    (a.Title ?? "").ToLowerInvariant().Contains(q) ||
                    (a.Description ?? "").ToLowerInvariant().Contains(q) ||
                    (a.Content ?? "").ToLowerInvariant().Contains(q) ||
                    (a.Source ?? "").ToLowerInvariant().Contains(q));
            }

            currentNews = filtered.ToList();
            NewsItemsControl.ItemsSource = null;
            NewsItemsControl.ItemsSource = currentNews;

            int count = currentNews.Count;
            NewsCountTextBlock.Text = count > 0
                ? $"{count} {GetNounForm(count, "новость", "новости", "новостей")}"
                : "Ничего не найдено";

            if (CurrentCategoryTextBlock != null)
            {
                if (isRegionalMode)
                    CurrentCategoryTextBlock.Text = $"📍 {activeRegion}";
                else if (!string.IsNullOrWhiteSpace(currentSearchQuery))
                    CurrentCategoryTextBlock.Text = $"Поиск: «{currentSearchQuery}»";
                else
                    CurrentCategoryTextBlock.Text = currentCategory;
            }
        }

        // ── Вспомогательные методы режимов ───────────────────────────────

        private void ClearSearchMode()
        {
            isSearchMode = false;
            activeSearchQuery = "";
            currentSearchQuery = "";
            SearchTextBox.Text = "";
            currentPage = 1;
            hasMorePages = true;
        }

        private void ClearRegionalMode()
        {
            isRegionalMode = false;
            activeRegion = "";
            currentPage = 1;
            hasMorePages = true;
        }

        // ── UI ────────────────────────────────────────────────────────────

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
                catch { ArticleImage.Source = null; ArticleImageBorder.Visibility = Visibility.Collapsed; }
            }
            else { ArticleImage.Source = null; ArticleImageBorder.Visibility = Visibility.Collapsed; }

            EmptyDetailPanel.Visibility = Visibility.Collapsed;
            ArticleScrollViewer.Visibility = Visibility.Visible;
            ArticleScrollViewer.ScrollToTop();
        }

        // ── Обработчики кнопок ────────────────────────────────────────────

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRegionalMode && !string.IsNullOrEmpty(activeRegion))
                await LoadRegionalNewsAsync(activeRegion);
            else
            {
                if (isSearchMode) ClearSearchMode();
                UpdateActiveCategoryButton(currentCategory);
                await LoadAllNewsAsync(true);
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = SearchTextBox.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Введите текст для поиска", "Поиск",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            isSearchMode = true;
            activeSearchQuery = query;
            currentSearchQuery = query;
            currentPage = 1;
            hasMorePages = true;
            if (isRegionalMode) ClearRegionalMode();

            ShowProgress(true);
            StatusTextBlock.Text = $"Поиск: {query}...";

            try
            {
                var result = await newsApiService.SearchNewsPageAsync(query, "ru", currentPage, 20);
                allNews = result.Articles;
                totalResults = result.TotalResults;
                hasMorePages = allNews.Count < totalResults;

                ApplyFilters();
                HideArticleDetails();
                CurrentCategoryTextBlock.Text = $"Поиск: «{query}»";
                NewsCountTextBlock.Text = totalResults > 0
                    ? $"{allNews.Count} из {totalResults} результатов"
                    : "Ничего не найдено";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally { ShowProgress(false); StatusTextBlock.Text = "Готово"; }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SearchButton_Click(sender, e);
        }

        private async void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSearchMode();
            UpdateActiveCategoryButton(currentCategory);
            if (allNews.Count == 0) await LoadAllNewsAsync();
            else ApplyFilters();
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentRole == "guest" || currentUserId == 0)
            {
                MessageBox.Show("Просмотр профиля доступен только авторизованным пользователям.",
                    "Доступ ограничен", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var win = new ProfileWindow(currentUserId);
            win.Owner = this;
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            win.ShowDialog();
        }

        private void ChangeRegionButton_Click(object sender, RoutedEventArgs e)
        {
            RegionSelectWindow.SelectedRegion = null;
            var win = new RegionSelectWindow(currentRole, currentUsername, currentUserId);
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();
            Close();
        }

        private void OpenInBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ArticleTitleTextBlock.Text)) return;
            var article = currentNews.Find(a => a.Title == ArticleTitleTextBlock.Text);
            if (article?.Url == null) return;
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = article.Url, UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show($"Не удалось открыть ссылку: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ManageUsersButton_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show("Функция управления пользователями будет реализована позже.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

        private void ModerateCommentsButton_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show("Функция модерации комментариев будет реализована позже.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

        private void AddCommentButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CommentTextBox.Text))
            {
                MessageBox.Show("Введите текст комментария.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            MessageBox.Show("Комментарий будет сохранен после реализации функции.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            CommentTextBox.Text = "";
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            RegionSelectWindow.SelectedRegion = null;
            var win = new MainWindow();
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();
            Close();
        }

        public void UpdateUserInfo(string displayName)
        {
            if (!string.IsNullOrEmpty(displayName))
            {
                WelcomeTextBlock.Text = $"Добро пожаловать, {displayName}!";
                currentUsername = displayName;
            }
        }

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