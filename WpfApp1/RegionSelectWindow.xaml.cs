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
        private string _role;
        private string _username;
        private int _userId;

        private List<RegionInfo> allRegions = new List<RegionInfo>();
        private List<RegionInfo> filteredRegions = new List<RegionInfo>();
        private RegionInfo selectedRegion = null;
        private List<Border> regionCards = new List<Border>();

        public static RegionInfo SelectedRegion { get; set; }

        public RegionSelectWindow(string role, string username, int userId)
        {
            InitializeComponent();
            _role = role;
            _username = username;
            _userId = userId;

            UserGreetingTextBlock.Text = username;
            RoleTextBlock.Text = GetRoleDisplayName(role);

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

        private void LoadRegions()
        {
            allRegions = new List<RegionInfo>();

            var r1 = new RegionInfo();
            r1.Name = "Москва"; r1.Area = "Центральный ФО"; r1.Population = "~13 млн";
            r1.Timezone = "UTC+3"; r1.Icon = "🏙️"; r1.Description = "Столица России, крупнейший город страны.";
            allRegions.Add(r1);

            var r2 = new RegionInfo();
            r2.Name = "Санкт-Петербург"; r2.Area = "Северо-Западный ФО"; r2.Population = "~5,4 млн";
            r2.Timezone = "UTC+3"; r2.Icon = "🌉"; r2.Description = "Культурная столица России, порт на Балтике.";
            allRegions.Add(r2);

            var r3 = new RegionInfo();
            r3.Name = "Новосибирск"; r3.Area = "Сибирский ФО"; r3.Population = "~1,6 млн";
            r3.Timezone = "UTC+7"; r3.Icon = "🔬"; r3.Description = "Крупнейший город Сибири, научный центр.";
            allRegions.Add(r3);

            var r4 = new RegionInfo();
            r4.Name = "Екатеринбург"; r4.Area = "Уральский ФО"; r4.Population = "~1,5 млн";
            r4.Timezone = "UTC+5"; r4.Icon = "⛰️"; r4.Description = "Промышленный и культурный центр Урала.";
            allRegions.Add(r4);

            var r5 = new RegionInfo();
            r5.Name = "Казань"; r5.Area = "Приволжский ФО"; r5.Population = "~1,3 млн";
            r5.Timezone = "UTC+3"; r5.Icon = "🕌"; r5.Description = "Столица Татарстана, центр Поволжья.";
            allRegions.Add(r5);

            var r6 = new RegionInfo();
            r6.Name = "Нижний Новгород"; r6.Area = "Приволжский ФО"; r6.Population = "~1,2 млн";
            r6.Timezone = "UTC+3"; r6.Icon = "⚓"; r6.Description = "Крупный город на Волге, транспортный узел.";
            allRegions.Add(r6);

            var r7 = new RegionInfo();
            r7.Name = "Челябинск"; r7.Area = "Уральский ФО"; r7.Population = "~1,2 млн";
            r7.Timezone = "UTC+5"; r7.Icon = "🏭"; r7.Description = "Промышленный центр на Южном Урале.";
            allRegions.Add(r7);

            var r8 = new RegionInfo();
            r8.Name = "Омск"; r8.Area = "Сибирский ФО"; r8.Population = "~1,1 млн";
            r8.Timezone = "UTC+6"; r8.Icon = "🚂"; r8.Description = "Крупный город в Западной Сибири.";
            allRegions.Add(r8);

            var r9 = new RegionInfo();
            r9.Name = "Самара"; r9.Area = "Приволжский ФО"; r9.Population = "~1,1 млн";
            r9.Timezone = "UTC+4"; r9.Icon = "🚀"; r9.Description = "Космическая столица России.";
            allRegions.Add(r9);

            var r10 = new RegionInfo();
            r10.Name = "Ростов-на-Дону"; r10.Area = "Южный ФО"; r10.Population = "~1,1 млн";
            r10.Timezone = "UTC+3"; r10.Icon = "🌅"; r10.Description = "Центр Южного ФО, порт на Дону.";
            allRegions.Add(r10);

            var r11 = new RegionInfo();
            r11.Name = "Уфа"; r11.Area = "Приволжский ФО"; r11.Population = "~1,1 млн";
            r11.Timezone = "UTC+5"; r11.Icon = "🌳"; r11.Description = "Столица Башкортостана.";
            allRegions.Add(r11);

            var r12 = new RegionInfo();
            r12.Name = "Красноярск"; r12.Area = "Сибирский ФО"; r12.Population = "~1,1 млн";
            r12.Timezone = "UTC+7"; r12.Icon = "🌲"; r12.Description = "Крупнейший город Восточной Сибири.";
            allRegions.Add(r12);

            var r13 = new RegionInfo();
            r13.Name = "Воронеж"; r13.Area = "Центральный ФО"; r13.Population = "~1,0 млн";
            r13.Timezone = "UTC+3"; r13.Icon = "🏛️"; r13.Description = "Крупный город центральной России.";
            allRegions.Add(r13);

            var r14 = new RegionInfo();
            r14.Name = "Пермь"; r14.Area = "Приволжский ФО"; r14.Population = "~1,0 млн";
            r14.Timezone = "UTC+5"; r14.Icon = "🎭"; r14.Description = "Промышленный и научный центр Урала.";
            allRegions.Add(r14);

            var r15 = new RegionInfo();
            r15.Name = "Волгоград"; r15.Area = "Южный ФО"; r15.Population = "~1,0 млн";
            r15.Timezone = "UTC+3"; r15.Icon = "🗿"; r15.Description = "Город-герой на Волге.";
            allRegions.Add(r15);

            var r16 = new RegionInfo();
            r16.Name = "Магнитогорск"; r16.Area = "Уральский ФО"; r16.Population = "~1,0 млн";
            r16.Timezone = "UTC+5"; r16.Icon = "🏭"; r16.Description = "Промышленный центр на Южном Урале.";
            allRegions.Add(r16);

            filteredRegions = new List<RegionInfo>(allRegions);
            UpdateRegionsCount();
        }

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

            var icon = new TextBlock
            {
                Text = region.Icon,
                FontSize = 22,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            var sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = region.Name,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x7E))
            });
            sp.Children.Add(new TextBlock
            {
                Text = region.Area,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0x90, 0xA4, 0xAE)),
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(sp, 1);
            grid.Children.Add(sp);

            border.Child = grid;
            border.MouseLeftButtonUp += RegionCard_Click;
            return border;
        }

        private void RegionCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is RegionInfo region)
                SelectRegion(region);
        }

        private void SelectRegion(RegionInfo region)
        {
            selectedRegion = region;
            foreach (var card in regionCards)
                card.Style = (card.Tag is RegionInfo r && r.Name == region.Name)
                    ? (Style)FindResource("ActiveRegionCardStyle")
                    : (Style)FindResource("RegionCardStyle");
            ShowRegionDetails(region);
        }

        private void ShowRegionDetails(RegionInfo region)
        {
            RegionIconTextBlock.Text = region.Icon;
            SelectedRegionNameTextBlock.Text = region.Name;
            SelectedRegionDescriptionTextBlock.Text = region.Description;
            RegionAreaTextBlock.Text = region.Area;
            RegionPopulationTextBlock.Text = region.Population;
            RegionTimezoneTextBlock.Text = region.Timezone;

            EmptyDetailPanel.Visibility = Visibility.Collapsed;
            RegionDetailsScrollViewer.Visibility = Visibility.Visible;
        }

        private void HideRegionDetails()
        {
            selectedRegion = null;
            foreach (var card in regionCards)
                card.Style = (Style)FindResource("RegionCardStyle");
            EmptyDetailPanel.Visibility = Visibility.Visible;
            RegionDetailsScrollViewer.Visibility = Visibility.Collapsed;
        }

        private void ConfirmRegionButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedRegion == null)
            {
                MessageBox.Show("Выберите город из списка", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectedRegion = selectedRegion;

            var weatherWin = new WeatherWindow(_role, _username, _userId, selectedRegion);
            weatherWin.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            weatherWin.Show();
            Close();
        }

        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            HideRegionDetails();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = SearchTextBox.Text.Trim().ToLowerInvariant();
            filteredRegions = string.IsNullOrEmpty(q)
                ? new List<RegionInfo>(allRegions)
                : allRegions.Where(r =>
                    r.Name.ToLowerInvariant().Contains(q) ||
                    r.Area.ToLowerInvariant().Contains(q)).ToList();

            UpdateRegionsCount();
            BuildRegionCards();

            if (selectedRegion != null && !filteredRegions.Any(r => r.Name == selectedRegion.Name))
                HideRegionDetails();
        }

        private void UpdateRegionsCount()
        {
            int n = filteredRegions.Count;
            RegionsCountTextBlock.Text = n > 0
                ? $"{n} {GetNounForm(n, "город", "города", "городов")}"
                : "Ничего не найдено";
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

    // Класс RegionInfo — единственное объявление в проекте.
    // Убедитесь что в старом RegionSelectWindow.xaml.cs его НЕТ.
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