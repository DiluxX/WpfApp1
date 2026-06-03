using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public partial class WeatherWindow : Window
    {
        private WeatherService _weatherService;
        private RegionInfo _selectedRegion;
        private string _role;
        private string _username;
        private int _userId;

        public WeatherWindow(string role, string username, int userId, RegionInfo region)
        {
            InitializeComponent();
            _role = role;
            _username = username;
            _userId = userId;
            _selectedRegion = region;
            _weatherService = new WeatherService();

            CityTextBlock.Text = _selectedRegion.Name;
            LoadWeather();
        }

        private async void LoadWeather()
        {
            LoadingProgress.Visibility = Visibility.Visible;
            WeatherCard.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Visibility = Visibility.Collapsed;

            var weather = await _weatherService.GetWeatherAsync(_selectedRegion.Name);

            LoadingProgress.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrEmpty(weather.Error))
            {
                ErrorTextBlock.Text = weather.Error;
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            WeatherCard.Visibility = Visibility.Visible;

            TempTextBlock.Text = $"{weather.Temperature:F1}°C";
            DescriptionTextBlock.Text = weather.Description;
            FeelsLikeTextBlock.Text = $"Ощущается как {weather.FeelsLike:F1}°C";
            HumidityTextBlock.Text = $"{weather.Humidity}%";
            PressureTextBlock.Text = $"{weather.PressureMmHg} мм рт. ст.";
            WindTextBlock.Text = $"{weather.WindSpeed:F1} м/с";
            WindDirTextBlock.Text = weather.WindDirection;

            // Загружаем иконку погоды
            if (!string.IsNullOrEmpty(weather.IconCode))
            {
                try
                {
                    string iconUrl = $"https://openweathermap.org/img/wn/{weather.IconCode}@2x.png";
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(iconUrl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    WeatherIcon.Source = bitmap;
                }
                catch { }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var selectionWindow = new SelectionWindow(_role, _username, _userId);
            selectionWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            selectionWindow.Show();
            Close();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await System.Threading.Tasks.Task.Run(() => LoadWeather());
        }
    }
}