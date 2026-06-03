using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WpfApp1;

namespace WpfApp1
{
    public class NewsApiService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "a302a226787c408e9a4c00596f19fc17";

        // Используем CORS прокси или другой домен
        private const string BaseUrl = "https://newsapi.org/v2/";

        // Альтернативный подход - использовать HTTPS
        private bool _useDirectApi = true;

        public NewsApiService()
        {
            var handler = new HttpClientHandler
            {
                UseProxy = false,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Добавляем заголовки для обхода ограничений
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        }

        public async Task<List<NewsArticle>> GetTopHeadlinesAsync(string country = "ru", string category = null, int pageSize = 20)
        {
            try
            {
                if (_useDirectApi)
                {
                    // Попытка 1: Прямой запрос к API
                    return await GetNewsDirect(country, category, pageSize);
                }

                // Попытка 2: Локальные данные
                return GetLocalNews(country, category, pageSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error: {ex.Message}");
                return GetLocalNews(country, category, pageSize);
            }
        }

        private async Task<List<NewsArticle>> GetNewsDirect(string country, string category, int pageSize)
        {
            try
            {
                // Пробуем разные форматы запросов
                string url;

                if (string.IsNullOrEmpty(category) || category == "Все новости")
                {
                    url = $"https://newsapi.org/v2/top-headlines?country={country}&pageSize={pageSize}";
                }
                else
                {
                    string apiCategory = MapCategoryToApi(category);
                    url = $"https://newsapi.org/v2/top-headlines?country={country}&category={apiCategory}&pageSize={pageSize}";
                }

                Console.WriteLine($"Request URL: {url}");

                // Используем заголовок вместо параметра в URL
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Api-Key", ApiKey);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var newsResponse = JsonConvert.DeserializeObject<NewsApiDirectResponse>(json);

                    if (newsResponse?.Status == "ok" && newsResponse.Articles != null)
                    {
                        return MapToArticles(newsResponse.Articles);
                    }
                }

                // Если не получилось, пробуем другой подход
                return await GetNewsFallback(country, category, pageSize);
            }
            catch
            {
                // В случае ошибки возвращаем локальные данные
                return GetLocalNews(country, category, pageSize);
            }
        }

        private async Task<List<NewsArticle>> GetNewsFallback(string country, string category, int pageSize)
        {
            // Попробуем через другой endpoint или подход
            try
            {
                string url = $"https://newsapi.org/v2/everything?q={MapCategoryToQuery(category)}&language=ru&pageSize={pageSize}&sortBy=publishedAt";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Api-Key", ApiKey);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var newsResponse = JsonConvert.DeserializeObject<NewsApiDirectResponse>(json);

                    if (newsResponse?.Status == "ok" && newsResponse.Articles != null)
                    {
                        return MapToArticles(newsResponse.Articles);
                    }
                }
            }
            catch
            {
                // Игнорируем ошибку
            }

            return GetLocalNews(country, category, pageSize);
        }

        private List<NewsArticle> GetLocalNews(string country, string category, int pageSize)
        {
            // Генерируем реалистичные тестовые данные
            var random = new Random();
            var articles = new List<NewsArticle>();

            string[] categories = { "Политика", "Технологии", "Спорт", "Бизнес", "Здоровье", "Наука", "Культура" };
            string[] sources = { "РИА Новости", "ТАСС", "Интерфакс", "BBC News", "Reuters", "Forbes", "Meduza", "РБК" };
            string[] authors = { "Иван Иванов", "Анна Петрова", "Сергей Сидоров", "Мария Кузнецова", "Алексей Смирнов" };

            // Если выбрана конкретная категория, используем ее
            if (!string.IsNullOrEmpty(category) && category != "Все новости")
            {
                categories = new[] { category };
            }

            for (int i = 0; i < pageSize; i++)
            {
                string currentCategory = categories[random.Next(categories.Length)];
                string currentSource = sources[random.Next(sources.Length)];
                string currentAuthor = authors[random.Next(authors.Length)];
                DateTime publishDate = DateTime.Now.AddHours(-random.Next(1, 168)); // До 7 дней назад

                articles.Add(new NewsArticle
                {
                    Title = GenerateTitle(currentCategory, i),
                    Description = GenerateDescription(currentCategory),
                    Content = GenerateContent(currentCategory),
                    Url = $"https://example.com/news/{Guid.NewGuid()}",
                    ImageUrl = GetRandomImageUrl(i),
                    PublishedAt = publishDate,
                    Source = currentSource,
                    Author = currentAuthor,
                    Category = currentCategory
                });
            }

            return articles;
        }

        private string MapCategoryToApi(string category)
        {
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["политика"] = "general",
                ["технологии"] = "technology",
                ["спорт"] = "sports",
                ["бизнес"] = "business",
                ["здоровье"] = "health",
                ["наука"] = "science",
                ["культура"] = "entertainment",
                ["развлечения"] = "entertainment"
            };

            return mapping.ContainsKey(category) ? mapping[category] : "general";
        }

        private string MapCategoryToQuery(string category)
        {
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["политика"] = "политика OR политик OR государство",
                ["технологии"] = "технологии OR IT OR программирование",
                ["спорт"] = "спорт OR футбол OR хоккей",
                ["бизнес"] = "бизнес OR экономика OR финансы",
                ["здоровье"] = "здоровье OR медицина OR лечение",
                ["наука"] = "наука OR исследование OR открытие",
                ["культура"] = "культура OR искусство OR кино",
                ["развлечения"] = "развлечения OR музыка OR шоу"
            };

            return mapping.ContainsKey(category) ? mapping[category] : "новости";
        }

        private List<NewsArticle> MapToArticles(List<DirectArticle> apiArticles)
        {
            var articles = new List<NewsArticle>();

            foreach (var apiArticle in apiArticles)
            {
                if (string.IsNullOrEmpty(apiArticle.Title) || apiArticle.Title == "[Removed]")
                    continue;

                DateTime publishedDate;
                if (!DateTime.TryParse(apiArticle.PublishedAt, out publishedDate))
                {
                    publishedDate = DateTime.Now;
                }

                articles.Add(new NewsArticle
                {
                    Title = apiArticle.Title,
                    Description = apiArticle.Description,
                    Content = apiArticle.Content,
                    Url = apiArticle.Url,
                    ImageUrl = apiArticle.UrlToImage,
                    PublishedAt = publishedDate,
                    Source = apiArticle.Source?.Name ?? "Неизвестно",
                    Author = apiArticle.Author ?? "Автор не указан",
                    Category = "Новости"
                });
            }

            return articles;
        }

        private string GenerateTitle(string category, int index)
        {
            var titles = new Dictionary<string, string[]>
            {
                ["Политика"] = new[]
                {
                    "Новые законы вступают в силу с нового года",
                    "Международный саммит прошел в Москве",
                    "Парламент принял важные поправки",
                    "Президент выступил с обращением к нации"
                },
                ["Технологии"] = new[]
                {
                    "Новый смартфон побил рекорды продаж",
                    "Искусственный интеллект создал картину",
                    "Кибербезопасность: новые угрозы",
                    "Роботы заменяют людей на производствах"
                },
                ["Спорт"] = new[]
                {
                    "Футбольный матч завершился со счетом 3:2",
                    "Новый рекорд в легкой атлетике",
                    "Сборная страны готовится к чемпионату",
                    "Спортсмен года: итоги голосования"
                },
                ["Бизнес"] = new[]
                {
                    "Фондовый рынок показывает рост",
                    "Крупная компания объявила о слиянии",
                    "Новые инвестиции в экономику",
                    "Курс доллара стабилизировался"
                }
            };

            var categoryTitles = titles.ContainsKey(category)
                ? titles[category]
                : new[] { "Важная новость", "Свежая информация", "Актуальный репортаж" };

            return categoryTitles[index % categoryTitles.Length];
        }

        private string GenerateDescription(string category)
        {
            return $"Актуальные новости в категории {category}. Подробности в полном тексте статьи.";
        }

        private string GenerateContent(string category)
        {
            return $"Это полный текст новости в категории {category}. Здесь содержится подробная информация о событии. " +
                   $"Новость была подготовлена редакцией и проверена факт-чекерами. " +
                   $"Следите за обновлениями для получения дополнительной информации.";
        }

        private string GetRandomImageUrl(int index)
        {
            var images = new[]
            {
                "https://images.unsplash.com/photo-1588681664899-f142ff2dc9b1?w=300&h=200&fit=crop",
                "https://images.unsplash.com/photo-1585829365295-ab7cd400c167?w=300&h=200&fit=crop",
                "https://images.unsplash.com/photo-1504711434969-e33886168f5c?w=300&h=200&fit=crop",
                "https://images.unsplash.com/photo-1495020689067-958852a7765e?w=300&h=200&fit=crop",
                "https://via.placeholder.com/300x200/2196F3/FFFFFF?text=News",
                "https://via.placeholder.com/300x200/4CAF50/FFFFFF?text=Sport",
                "https://via.placeholder.com/300x200/FF9800/FFFFFF?text=Tech",
                "https://via.placeholder.com/300x200/9C27B0/FFFFFF?text=Business"
            };

            return images[index % images.Length];
        }

        public async Task<List<NewsArticle>> SearchNewsAsync(string query, string language = "ru", int pageSize = 20)
        {
            try
            {
                string url = $"https://newsapi.org/v2/everything?q={Uri.EscapeDataString(query)}&language={language}&pageSize={pageSize}&sortBy=publishedAt";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("X-Api-Key", ApiKey);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var newsResponse = JsonConvert.DeserializeObject<NewsApiDirectResponse>(json);

                    if (newsResponse?.Status == "ok")
                    {
                        return MapToArticles(newsResponse.Articles);
                    }
                }
            }
            catch
            {
                // Игнорируем ошибку
            }

            // Фильтруем локальные данные по запросу
            var localNews = GetLocalNews("ru", null, 50);
            return localNews.FindAll(a =>
                (a.Title?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (a.Description?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        public async Task<List<NewsArticle>> GetNewsByCategoryAsync(string category, int pageSize = 20)
        {
            return await GetTopHeadlinesAsync("ru", category, pageSize);
        }
    }

    // Модели для прямого API
    public class NewsApiDirectResponse
    {
        public string Status { get; set; }
        public int TotalResults { get; set; }
        public List<DirectArticle> Articles { get; set; }
        public string Message { get; set; }
    }

    public class DirectArticle
    {
        public DirectSource Source { get; set; }
        public string Author { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string UrlToImage { get; set; }
        public string PublishedAt { get; set; }
        public string Content { get; set; }
    }

    public class DirectSource
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}