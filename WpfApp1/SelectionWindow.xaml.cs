using System.Windows;

namespace WpfApp1
{
    public partial class SelectionWindow : Window
    {
        private string _role;
        private string _username;
        private int _userId;
        private RegionInfo _selectedRegion;

        public SelectionWindow(string role, string username, int userId)
        {
            InitializeComponent();
            _role = role;
            _username = username;
            _userId = userId;
            _selectedRegion = RegionSelectWindow.SelectedRegion;

            WelcomeTextBlock.Text = $"Добро пожаловать, {username}!";
            RoleTextBlock.Text = GetRoleDisplayName(role);

            if (_selectedRegion != null)
            {
                RegionTextBlock.Text = $"📍 {_selectedRegion.Name}";
                RegionBadge.Visibility = Visibility.Visible;
            }
            else
            {
                RegionBadge.Visibility = Visibility.Collapsed;
            }
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

        private void NewsButton_Click(object sender, RoutedEventArgs e)
        {
            var newsWindow = new news_main(_role, _username, _userId);
            newsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            newsWindow.Show();
            Close();
        }

        private void WeatherButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRegion == null)
            {
                MessageBox.Show("Город не выбран. Пожалуйста, выберите город в окне выбора местности.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var weatherWindow = new WeatherWindow(_role, _username, _userId, _selectedRegion);
            weatherWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            weatherWindow.Show();
            Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            RegionSelectWindow.SelectedRegion = null;
            var mainWindow = new MainWindow();
            mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            mainWindow.Show();
            Close();
        }
    }
}