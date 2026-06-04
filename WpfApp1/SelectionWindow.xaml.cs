using System.Windows;

namespace WpfApp1
{
    public partial class SelectionWindow : Window
    {
        private string _role;
        private string _username;
        private int _userId;

        public SelectionWindow(string role, string username, int userId)
        {
            InitializeComponent();
            _role = role;
            _username = username;
            _userId = userId;

            WelcomeTextBlock.Text = $"Добро пожаловать, {username}!";
            RoleTextBlock.Text = GetRoleDisplayName(role);
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

        // Новости — сразу открываем news_main (регион не нужен)
        private void NewsButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new news_main(_role, _username, _userId);
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();
            Close();
        }

        // Погода — выбираем город, RegionSelectWindow сам откроет WeatherWindow
        private void WeatherButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new RegionSelectWindow(_role, _username, _userId);
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();
            Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            RegionSelectWindow.SelectedRegion = null;
            var win = new MainWindow();
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();
            Close();
        }
    }
}