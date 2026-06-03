using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WpfApp1
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "76d3e8fcca5154a30cb50788ca8ea9d0";
        private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";

        public WeatherService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<WeatherInfo> GetWeatherAsync(string cityName)
        {
            try
            {
                string url = $"{BaseUrl}?q={Uri.EscapeDataString(cityName)}&appid={ApiKey}&units=metric&lang=ru";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return new WeatherInfo { Error = $"Ошибка API: {response.StatusCode}" };
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<OpenWeatherResponse>(json);
                if (data == null || data.Main == null)
                {
                    return new WeatherInfo { Error = "Не удалось разобрать данные погоды" };
                }

                return new WeatherInfo
                {
                    City = data.Name,
                    Temperature = data.Main.Temp,
                    FeelsLike = data.Main.Feels_Like,
                    Humidity = data.Main.Humidity,
                    Pressure = data.Main.Pressure,
                    WindSpeed = data.Wind.Speed,
                    WindDeg = data.Wind.Deg,
                    Description = data.Weather?[0]?.Description ?? "",
                    IconCode = data.Weather?[0]?.Icon ?? "",
                    Error = null
                };
            }
            catch (Exception ex)
            {
                return new WeatherInfo { Error = $"Ошибка: {ex.Message}" };
            }
        }
    }

    public class WeatherInfo
    {
        public string City { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public int Humidity { get; set; }
        public int Pressure { get; set; } // hPa
        public double WindSpeed { get; set; } // m/s
        public int WindDeg { get; set; } // градусы
        public string Description { get; set; }
        public string IconCode { get; set; }
        public string Error { get; set; }

        public string WindDirection
        {
            get
            {
                if (WindDeg >= 337.5 || WindDeg < 22.5) return "С";
                if (WindDeg >= 22.5 && WindDeg < 67.5) return "СВ";
                if (WindDeg >= 67.5 && WindDeg < 112.5) return "В";
                if (WindDeg >= 112.5 && WindDeg < 157.5) return "ЮВ";
                if (WindDeg >= 157.5 && WindDeg < 202.5) return "Ю";
                if (WindDeg >= 202.5 && WindDeg < 247.5) return "ЮЗ";
                if (WindDeg >= 247.5 && WindDeg < 292.5) return "З";
                if (WindDeg >= 292.5 && WindDeg < 337.5) return "СЗ";
                return "—";
            }
        }

        public string PressureMmHg => (Pressure * 0.75006).ToString("F0");
    }

    // Внутренние модели для десериализации JSON
    internal class OpenWeatherResponse
    {
        public string Name { get; set; }
        public MainInfo Main { get; set; }
        public WindInfo Wind { get; set; }
        public WeatherInfoShort[] Weather { get; set; }
    }

    internal class MainInfo
    {
        public double Temp { get; set; }
        public double Feels_Like { get; set; }
        public int Humidity { get; set; }
        public int Pressure { get; set; }
    }

    internal class WindInfo
    {
        public double Speed { get; set; }
        public int Deg { get; set; }
    }

    internal class WeatherInfoShort
    {
        public string Description { get; set; }
        public string Icon { get; set; }
    }
}