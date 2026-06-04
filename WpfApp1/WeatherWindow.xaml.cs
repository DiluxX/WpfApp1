using System;
using System.Globalization;
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

        // Метод async void — вызывается из UI-потока, UI-элементы трогает напрямую
        private async void LoadWeather()
        {
            LoadingProgress.Visibility = Visibility.Visible;
            WeatherCard.Visibility = Visibility.Collapsed;
            ErrorTextBlock.Visibility = Visibility.Collapsed;
            RefreshButton.IsEnabled = false;

            var weather = await _weatherService.GetWeatherAsync(_selectedRegion.Name);

            LoadingProgress.Visibility = Visibility.Collapsed;
            RefreshButton.IsEnabled = true;

            if (!string.IsNullOrEmpty(weather.Error))
            {
                ErrorTextBlock.Text = weather.Error;
                ErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }

            WeatherCard.Visibility = Visibility.Visible;

            TempTextBlock.Text = $"{weather.Temperature:F1}°C";
            DescriptionTextBlock.Text = char.ToUpper(weather.Description[0]) + weather.Description.Substring(1);
            FeelsLikeTextBlock.Text = $"Ощущается как {weather.FeelsLike:F1}°C";
            HumidityTextBlock.Text = $"{weather.Humidity}%";
            PressureTextBlock.Text = $"{weather.PressureMmHg} мм рт. ст.";
            WindTextBlock.Text = $"{weather.WindSpeed:F1} м/с";
            WindDirTextBlock.Text = weather.WindDirection;

            if (!string.IsNullOrEmpty(weather.IconCode))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri($"https://openweathermap.org/img/wn/{weather.IconCode}@2x.png");
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    WeatherIcon.Source = bitmap;
                }
                catch { WeatherIcon.Source = null; }
            }
        }

        // Кнопка "Назад" возвращает на SelectionWindow
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new SelectionWindow(_role, _username, _userId);
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            win.Show();
            Close();
        }

        // ФИX КРАША: просто вызываем LoadWeather() напрямую из UI-потока
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadWeather();
        }
    }
}