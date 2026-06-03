using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp1
{
    public partial class RegionSelectWindow : Window
    {
        private string currentRole;
        private string currentUsername;
        private int currentUserId;

        private List<RegionInfo> allRegions = new List<RegionInfo>();
        private List<RegionInfo> filteredRegions = new List<RegionInfo>();
        private RegionInfo selectedRegion = null;

        // Список UI-элементов карточек регионов
        private List<Border> regionCards = new List<Border>();

        public RegionSelectWindow(string role, string username, int userId)
        {
            InitializeComponent();
            currentRole = role;
            currentUsername = username;
            currentUserId = userId;

            InitializeWindow();
        }

        private void InitializeWindow()
        {
            // Приветствие пользователя
            UserGreetingTextBlock.Text = $"{currentUsername}";
            RoleTextBlock.Text = GetRoleDisplayName(currentRole);

            // Загружаем список регионов
            LoadRegions();
            BuildRegionCards();
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
        //  Данные регионов
        // ═══════════════════════════════════════════════════════

        private void LoadRegions()
        {
            allRegions = new List<RegionInfo>
            {
                new RegionInfo { Name = "Москва", Area = "Центральный федеральный округ",
                    Population = "~13 млн", Timezone = "UTC+3",
                    Description = "Столица России, крупнейший город страны. Политический, экономический и культурный центр.",
                    Icon = "🏙️" },

                new RegionInfo { Name = "Санкт-Петербург", Area = "Северо-Западный федеральный округ",
                    Population = "~5,4 млн", Timezone = "UTC+3",
                    Description = "Второй по величине город России. Важный экономический и культурный центр, порт на Балтике.",
                    Icon = "🌉" },

                new RegionInfo { Name = "Новосибирск", Area = "Сибирский федеральный округ",
                    Population = "~1,6 млн", Timezone = "UTC+7",
                    Description = "Крупнейший город Сибири, научный и промышленный центр.",
                    Icon = "🔬" },

                new RegionInfo { Name = "Екатеринбург", Area = "Уральский федеральный округ",
                    Population = "~1,5 млн", Timezone = "UTC+5",
                    Description = "Административный центр Уральского федерального округа. Крупный промышленный и культурный центр.",
                    Icon = "⛰️" },

                new RegionInfo { Name = "Казань", Area = "Приволжский федеральный округ",
                    Population = "~1,3 млн", Timezone = "UTC+3",
                    Description = "Столица Татарстана, крупный культурный и экономический центр Поволжья.",
                    Icon = "🕌" },

                new RegionInfo { Name = "Нижний Новгород", Area = "Приволжский федеральный округ",
                    Population = "~1,2 млн", Timezone = "UTC+3",
                    Description = "Крупный город на Волге, важный транспортный и экономический узел.",
                    Icon = "⚓" },

                new RegionInfo { Name = "Челябинск", Area = "Уральский федеральный округ",
                    Population = "~1,2 млн", Timezone = "UTC+5",
                    Description = "Крупный промышленный центр на Южном Урале.",
                    Icon = "🏭" },

                new RegionInfo { Name = "Омск", Area = "Сибирский федеральный округ",
                    Population = "~1,1 млн", Timezone = "UTC+6",
                    Description = "Крупный город в Западной Сибири, транспортный узел.",
                    Icon = "🚂" },

                new RegionInfo { Name = "Самара", Area = "Приволжский федеральный округ",
                    Population = "~1,1 млн", Timezone = "UTC+4",
                    Description = "Город на Волге, космическая столица России.",
                    Icon = "🚀" },

                new RegionInfo { Name = "Ростов-на-Дону", Area = "Южный федеральный округ",
                    Population = "~1,1 млн", Timezone = "UTC+3",
                    Description = "Административный центр Южного федерального округа, порт на Дону.",
                    Icon = "🌅" },

                new RegionInfo { Name = "Уфа", Area = "Приволжский федеральный округ",
                    Population = "~1,1 млн", Timezone = "UTC+5",
                    Description = "Столица Башкортостана, крупный промышленный центр.",
                    Icon = "🌳" },

                new RegionInfo { Name = "Красноярск", Area = "Сибирский федеральный округ",
                    Population = "~1,1 млн", Timezone = "UTC+7",
                    Description = "Крупнейший город Восточной Сибири, промышленный и транспортный центр.",
                    Icon = "🌲" },

                new RegionInfo { Name = "Воронеж", Area = "Центральный федеральный округ",
                    Population = "~1,0 млн", Timezone = "UTC+3",
                    Description = "Крупный город в центре европейской части России.",
                    Icon = "🏛️" },

                new RegionInfo { Name = "Пермь", Area = "Приволжский федеральный округ",
                    Population = "~1,0 млн", Timezone = "UTC+5",
                    Description = "Крупный город на Урале, промышленный и научный центр.",
                    Icon = "🎭" },

                new RegionInfo { Name = "Волгоград", Area = "Южный федеральный округ",
                    Population = "~1,0 млн", Timezone = "UTC+3",
                    Description = "Город-герой на Волге, важный промышленный центр.",
                    Icon = "🗿" }
            };

            filteredRegions = new List<RegionInfo>(allRegions);
            UpdateRegionsCount();
        }

        // ═══════════════════════════════════════════════════════
        //  Построение карточек регионов
        // ═══════════════════════════════════════════════════════

        private void BuildRegionCards()
        {
            RegionsPanel.Children.Clear();
            regionCards.Clear();

            foreach (var region in filteredRegions)
            {
                var card = CreateRegionCard(region);
                regionCards.Add(card);
                RegionsPanel.Children.Add(card);
            }
        }

        private Border CreateRegionCard(RegionInfo region)
        {
            var border = new Border
            {
                Style = (Style)FindResource("RegionCardStyle"),
                Tag = region
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Иконка
            var iconText = new TextBlock
            {
                Text = region.Icon,
                FontSize = 22,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(iconText, 0);
            grid.Children.Add(iconText);

            // Текст
            var stackPanel = new StackPanel();
            stackPanel.Children.Add(new TextBlock
            {
                Text = region.Name,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x7E))
            });
            stackPanel.Children.Add(new TextBlock
            {
                Text = region.Area,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0x90, 0xA4, 0xAE)),
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(stackPanel, 1);
            grid.Children.Add(stackPanel);

            border.Child = grid;
            border.MouseLeftButtonUp += RegionCard_Click;

            return border;
        }

        // ═══════════════════════════════════════════════════════
        //  Обработчики событий
        // ═══════════════════════════════════════════════════════

        private void RegionCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is RegionInfo region)
            {
                SelectRegion(region);
            }
        }

        private void SelectRegion(RegionInfo region)
        {
            selectedRegion = region;

            // Обновляем стили карточек
            foreach (var card in regionCards)
            {
                if (card.Tag is RegionInfo cardRegion && cardRegion.Name == region.Name)
                {
                    card.Style = (Style)FindResource("ActiveRegionCardStyle");
                }
                else
                {
                    card.Style = (Style)FindResource("RegionCardStyle");
                }
            }

            // Показываем детали
            ShowRegionDetails(region);
        }

        private void ShowRegionDetails(RegionInfo region)
        {
            // Заполняем данные
            RegionIconTextBlock.Text = region.Icon;
            SelectedRegionNameTextBlock.Text = region.Name;
            SelectedRegionDescriptionTextBlock.Text = region.Description;
            RegionAreaTextBlock.Text = region.Area;
            RegionPopulationTextBlock.Text = region.Population;
            RegionTimezoneTextBlock.Text = region.Timezone;

            // Показываем панель деталей
            EmptyDetailPanel.Visibility = Visibility.Collapsed;
            RegionDetailsScrollViewer.Visibility = Visibility.Visible;
        }

        private void HideRegionDetails()
        {
            selectedRegion = null;

            // Сбрасываем стили карточек
            foreach (var card in regionCards)
            {
                card.Style = (Style)FindResource("RegionCardStyle");
            }

            EmptyDetailPanel.Visibility = Visibility.Visible;
            RegionDetailsScrollViewer.Visibility = Visibility.Collapsed;
        }

        // В методе ConfirmRegionButton_Click заменить содержимое на:
        private void ConfirmRegionButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedRegion == null)
            {
                MessageBox.Show("Выберите город из списка", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedRegion = selectedRegion;

            // Открываем окно выбора (Новости/Погода)
            var selectionWindow = new SelectionWindow(currentRole, currentUsername, currentUserId);
            selectionWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            selectionWindow.Show();
            this.Close();
        }

        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            HideRegionDetails();
        }

        // ═══════════════════════════════════════════════════════
        //  Поиск
        // ═══════════════════════════════════════════════════════

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchTextBox.Text.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(query))
            {
                filteredRegions = new List<RegionInfo>(allRegions);
            }
            else
            {
                filteredRegions = allRegions.Where(r =>
                    r.Name.ToLowerInvariant().Contains(query) ||
                    r.Area.ToLowerInvariant().Contains(query)
                ).ToList();
            }

            UpdateRegionsCount();
            BuildRegionCards();

            // Если выбранный регион не в отфильтрованном списке — скрываем детали
            if (selectedRegion != null && !filteredRegions.Any(r => r.Name == selectedRegion.Name))
            {
                HideRegionDetails();
            }
        }

        private void UpdateRegionsCount()
        {
            int count = filteredRegions.Count;
            RegionsCountTextBlock.Text = count > 0
                ? $"{count} {GetNounForm(count, "город", "города", "городов")}"
                : "Ничего не найдено";
        }

        // ═══════════════════════════════════════════════════════
        //  Статическое свойство для передачи выбранного региона
        // ═══════════════════════════════════════════════════════

        public static RegionInfo SelectedRegion { get; set; }

        // ═══════════════════════════════════════════════════════
        //  Вспомогательное
        // ═══════════════════════════════════════════════════════

        private static string GetNounForm(int n, string one, string few, string many)
        {
            int m10 = n % 10, m100 = n % 100;
            if (m100 >= 11 && m100 <= 14) return $"{n} {many}";
            if (m10 == 1) return $"{n} {one}";
            if (m10 >= 2 && m10 <= 4) return $"{n} {few}";
            return $"{n} {many}";
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Модель региона
    // ═══════════════════════════════════════════════════════

    public class RegionInfo
    {
        public string Name { get; set; }
        public string Area { get; set; }
        public string Population { get; set; }
        public string Timezone { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
    }
}