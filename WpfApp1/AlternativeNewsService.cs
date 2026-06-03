// AlternativeNewsService.cs
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using WpfApp1;

namespace WpfApp1
{
    public class AlternativeNewsService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "93b54dbbe72264310d89f9c8d68d7bdb"; // Пример ключа, получите свой на gnews.io
        private const string BaseUrl = "https://gnews.io/api/v4/";

        public AlternativeNewsService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "NewsPortal/1.0");
        }

        public async Task<List<NewsArticle>> GetTopHeadlinesAsync(string country = "ru", string category = null, int pageSize = 10)
        {
            try
            {
                // GNews API формат
                string url = $"{BaseUrl}top-headlines?token={ApiKey}&lang=ru&country={country}&max={pageSize}";

                if (!string.IsNullOrEmpty(category) && category != "Все новости")
                {
                    url += $"&topic={MapCategoryToTopic(category)}";
                }

                var response = await _httpClient.GetStringAsync(url);
                var gnewsResponse = JsonConvert.DeserializeObject<GNewsResponse>(response);

                if (gnewsResponse?.Articles != null)
                {
                    return MapGNewsToArticles(gnewsResponse.Articles);
                }

                return GetFallbackArticles();
            }
            catch (Exception)
            {
                // Возвращаем тестовые данные если API не доступен
                return GetFallbackArticles();
            }
        }

        private string MapCategoryToTopic(string category)
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

        private List<NewsArticle> MapGNewsToArticles(List<GNewsArticle> gnewsArticles)
        {
            var articles = new List<NewsArticle>();

            foreach (var gnewsArticle in gnewsArticles)
            {
                if (string.IsNullOrEmpty(gnewsArticle.Title) || gnewsArticle.Title == "[Removed]")
                    continue;

                DateTime publishedDate;
                if (!DateTime.TryParse(gnewsArticle.PublishedAt, out publishedDate))
                {
                    publishedDate = DateTime.Now;
                }

                articles.Add(new NewsArticle
                {
                    Title = gnewsArticle.Title,
                    Description = gnewsArticle.Description,
                    Content = gnewsArticle.Content,
                    Url = gnewsArticle.Url,
                    ImageUrl = gnewsArticle.Image,
                    PublishedAt = publishedDate,
                    Source = gnewsArticle.Source?.Name ?? "Неизвестно",
                    Author = "Автор не указан",
                    Category = "Новости"
                });
            }

            return articles;
        }

        public List<NewsArticle> GetFallbackArticles()
        {
            var random = new Random();
            var categories = new[] { "Политика", "Технологии", "Спорт", "Бизнес", "Здоровье", "Наука", "Культура" };
            var sources = new[] { "РИА Новости", "ТАСС", "Интерфакс", "BBC", "Reuters", "Forbes", "Meduza" };

            var articles = new List<NewsArticle>();

            for (int i = 0; i < 15; i++) // 15 случайных новостей
            {
                var category = categories[random.Next(categories.Length)];
                var source = sources[random.Next(sources.Length)];
                var hoursAgo = random.Next(1, 72);

                articles.Add(new NewsArticle
                {
                    Title = $"Новость {i + 1}: {GetRandomTitle(category)}",
                    Description = GetRandomDescription(category),
                    Content = GetRandomContent(category),
                    Source = source,
                    PublishedAt = DateTime.Now.AddHours(-hoursAgo),
                    Author = GetRandomAuthor(),
                    Category = category,
                    Url = $"https://example.com/news/{i + 1}",
                    ImageUrl = GetRandomImageUrl(i)
                });
            }

            return articles;
        }

        private string GetRandomTitle(string category)
        {
            var titles = new Dictionary<string, string[]>
            {
                ["Политика"] = new[] { "Новые законы приняты", "Международные переговоры", "Выборы 2024", "Политический кризис" },
                ["Технологии"] = new[] { "Новый смартфон представлен", "ИИ создает музыку", "Кибербезопасность", "Квантовые компьютеры" },
                ["Спорт"] = new[] { "Рекордный матч", "Новые трансферы", "Олимпийские игры", "Чемпионат мира" },
                ["Бизнес"] = new[] { "Курсы валют", "Новые инвестиции", "Фондовый рынок", "Стартап привлек финансирование" },
                ["Здоровье"] = new[] { "Новый метод лечения", "Медицинское открытие", "Здоровый образ жизни", "Вакцинация" }
            };

            var categoryTitles = titles.ContainsKey(category) ? titles[category] : titles["Политика"];
            return categoryTitles[new Random().Next(categoryTitles.Length)];
        }

        private string GetRandomDescription(string category)
        {
            return $"Описание новости в категории {category}. Это автоматически сгенерированное описание для демонстрации работы приложения.";
        }

        private string GetRandomContent(string category)
        {
            return $"Полное содержание новости в категории {category}. Этот текст создан автоматически для тестирования функционала новостного портала. Здесь могла бы быть реальная новость.";
        }

        private string GetRandomAuthor()
        {
            var authors = new[] { "Иван Иванов", "Петр Петров", "Анна Сидорова", "Мария Кузнецова", "Алексей Смирнов" };
            return authors[new Random().Next(authors.Length)];
        }

        private string GetRandomImageUrl(int index)
        {
            var images = new[]
            {
                "https://picsum.photos/300/200?random=1",
                "https://picsum.photos/300/200?random=2",
                "https://picsum.photos/300/200?random=3",
                "https://picsum.photos/300/200?random=4",
                "https://picsum.photos/300/200?random=5",
                "https://via.placeholder.com/300x200/2196F3/FFFFFF?text=News",
                "https://via.placeholder.com/300x200/4CAF50/FFFFFF?text=Sport",
                "https://via.placeholder.com/300x200/FF9800/FFFFFF?text=Tech",
                "https://via.placeholder.com/300x200/9C27B0/FFFFFF?text=Business"
            };

            return images[index % images.Length];
        }
    }

    // Модели для GNews API
    public class GNewsResponse
    {
        public int TotalArticles { get; set; }
        public List<GNewsArticle> Articles { get; set; }
    }

    public class GNewsArticle
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
        public string PublishedAt { get; set; }
        public GNewsSource Source { get; set; }
    }

    public class GNewsSource
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}