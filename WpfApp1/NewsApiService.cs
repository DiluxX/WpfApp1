using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class NewsPageResult
    {
        public List<NewsArticle> Articles { get; set; }
        public int TotalResults { get; set; }
    }

    public class NewsApiService
    {
        private readonly HttpClient _http;
        private const string ApiKey = "49a5c09f12b24cff89352fa0706c00f9"; // <-- НОВЫЙ КЛЮЧ
        private const string BaseUrl = "https://newsapi.org/v2/";

        private static readonly Dictionary<string, string> CategoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Политика"] = "general",
            ["Технологии"] = "technology",
            ["Спорт"] = "sports",
            ["Бизнес"] = "business",
            ["Здоровье"] = "health",
            ["Наука"] = "science",
            ["Культура"] = "entertainment",
            ["Развлечения"] = "entertainment",
        };

        private static readonly Dictionary<string, string> ApiCatToRu = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["general"] = "Политика",
            ["technology"] = "Технологии",
            ["sports"] = "Спорт",
            ["business"] = "Бизнес",
            ["health"] = "Здоровье",
            ["science"] = "Наука",
            ["entertainment"] = "Культура",
        };

        private static readonly string[] AllApiCats = { "general", "technology", "sports", "business", "health", "science", "entertainment" };

        public NewsApiService()
        {
            var handler = new HttpClientHandler
            {
                UseProxy = false,
                ServerCertificateCustomValidationCallback = (s, c, ch, e) => true
            };
            _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _http.DefaultRequestHeaders.Add("Accept", "application/json");
            _http.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        }

        // ----- Пагинация -----
        public async Task<NewsPageResult> GetTopHeadlinesPageAsync(string country = "ru", string categoryRu = null, int page = 1, int pageSize = 20)
        {
            if (string.IsNullOrEmpty(categoryRu) || categoryRu == "Все новости")
                return await FetchAllCategoriesPageAsync(page, pageSize);

            string apiCat = CategoryMap.ContainsKey(categoryRu) ? CategoryMap[categoryRu] : "general";
            return await FetchCategoryPageAsync(apiCat, categoryRu, page, pageSize);
        }

        // ----- Старый метод для совместимости -----
        public async Task<List<NewsArticle>> GetTopHeadlinesAsync(string country = "ru", string categoryRu = null, int pageSize = 20)
        {
            var result = await GetTopHeadlinesPageAsync(country, categoryRu, 1, pageSize);
            return result.Articles;
        }

        private async Task<NewsPageResult> FetchAllCategoriesPageAsync(int page, int pageSize)
        {
            int perCat = Math.Max(3, pageSize / AllApiCats.Length);
            var tasks = AllApiCats.Select(cat => FetchCategoryPageAsync(cat, ApiCatToRu[cat], page, perCat)).ToList();
            try
            {
                var results = await Task.WhenAll(tasks);
                var all = results.SelectMany(r => r.Articles).ToList();
                int total = results.Sum(r => r.TotalResults);
                if (all.Count > 0)
                    return new NewsPageResult { Articles = all.OrderByDescending(a => a.PublishedAt).ToList(), TotalResults = total };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки категорий: {ex.Message}");
            }
            var fallback = BuildFallbackNews(pageSize);
            return new NewsPageResult { Articles = fallback, TotalResults = fallback.Count };
        }

        private async Task<NewsPageResult> FetchCategoryPageAsync(string apiCat, string ruCat, int page, int pageSize)
        {
            string url = $"{BaseUrl}top-headlines?country=ru&category={apiCat}&page={page}&pageSize={pageSize}";
            var (articles, total) = await FetchArticlesWithTotalAsync(url, ruCat);
            if (articles.Count > 0)
                return new NewsPageResult { Articles = articles, TotalResults = total };

            url = $"{BaseUrl}everything?q={Uri.EscapeDataString(CatToQuery(apiCat))}&language=ru&page={page}&pageSize={pageSize}&sortBy=publishedAt";
            (articles, total) = await FetchArticlesWithTotalAsync(url, ruCat);
            if (articles.Count > 0)
                return new NewsPageResult { Articles = articles, TotalResults = total };

            var fallback = BuildFallbackNews(pageSize, ruCat);
            return new NewsPageResult { Articles = fallback, TotalResults = fallback.Count };
        }

        private async Task<(List<NewsArticle> articles, int totalResults)> FetchArticlesWithTotalAsync(string url, string defaultCategory)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                var resp = await _http.SendAsync(req);
                var json = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка API: {resp.StatusCode} - {json}");
                    return (new List<NewsArticle>(), 0);
                }

                var data = JsonConvert.DeserializeObject<GnApiResponse>(json);
                if (data?.Status != "ok" || data.Articles == null || data.Articles.Count == 0)
                    return (new List<NewsArticle>(), 0);

                var articles = data.Articles
                    .Where(a => !string.IsNullOrEmpty(a.Title) && a.Title != "[Removed]")
                    .Select(a => MapToArticle(a, defaultCategory))
                    .ToList();
                return (articles, data.TotalResults);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Исключение: {ex.Message}");
                return (new List<NewsArticle>(), 0);
            }
        }

        public async Task<List<NewsArticle>> SearchNewsAsync(string query, string language = "ru", int pageSize = 30)
        {
            try
            {
                string url = $"{BaseUrl}everything?q={Uri.EscapeDataString(query)}&language={language}&pageSize={pageSize}&sortBy=publishedAt";
                var result = await FetchArticlesWithTotalAsync(url, "Новости");
                if (result.articles.Count > 0) return result.articles;
            }
            catch { }
            string q = query.ToLowerInvariant();
            return BuildFallbackNews(100).Where(a =>
                (a.Title ?? "").ToLowerInvariant().Contains(q) ||
                (a.Description ?? "").ToLowerInvariant().Contains(q) ||
                (a.Source ?? "").ToLowerInvariant().Contains(q)).ToList();
        }
        public async Task<NewsPageResult> SearchNewsPageAsync(string query, string language = "ru", int page = 1, int pageSize = 20)
        {
            try
            {
                string url = $"{BaseUrl}everything?q={Uri.EscapeDataString(query)}&language={language}&page={page}&pageSize={pageSize}&sortBy=publishedAt";
                var (articles, total) = await FetchArticlesWithTotalAsync(url, "Результаты поиска");
                if (articles.Count > 0)
                    return new NewsPageResult { Articles = articles, TotalResults = total };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка поиска: {ex.Message}");
            }
            return new NewsPageResult { Articles = new List<NewsArticle>(), TotalResults = 0 };
        }
       
        private static NewsArticle MapToArticle(GnArticle a, string defaultCategory)
        {
            DateTime.TryParse(a.PublishedAt, out DateTime pub);
            if (pub == default) pub = DateTime.Now;
            return new NewsArticle
            {
                Title = a.Title?.Trim(),
                Description = a.Description?.Trim(),
                Content = StripSuffix(a.Content),
                Url = a.Url,
                ImageUrl = a.UrlToImage,
                PublishedAt = pub,
                Source = a.Source?.Name ?? "Неизвестно",
                Author = string.IsNullOrWhiteSpace(a.Author) ? "Автор не указан" : a.Author,
                Category = defaultCategory
            };
        }

        private static string StripSuffix(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int i = s.IndexOf(" [+", StringComparison.Ordinal);
            return i > 0 ? s.Substring(0, i) : s;
        }

        private static string CatToQuery(string apiCat)
        {
            switch (apiCat)
            {
                case "technology": return "технологии OR IT OR искусственный интеллект";
                case "sports": return "спорт OR футбол OR хоккей OR теннис";
                case "business": return "бизнес OR экономика OR финансы OR рынок";
                case "health": return "здоровье OR медицина OR лечение OR вакцина";
                case "science": return "наука OR исследование OR открытие OR космос";
                case "entertainment": return "культура OR кино OR музыка OR искусство";
                default: return "политика OR Россия OR правительство";
            }
        }

        // Fallback данные (используются только если API полностью недоступен)
        private static readonly string[] FbCats = { "Политика", "Технологии", "Спорт", "Бизнес", "Здоровье", "Наука", "Культура" };
        private static readonly string[] FbSources = { "РИА Новости", "ТАСС", "Интерфакс", "BBC", "Reuters", "Forbes", "РБК" };
        private static readonly string[] FbAuthors = { "Иван Иванов", "Анна Петрова", "Сергей Сидоров", "Мария Кузнецова", "Алексей Смирнов" };
        private static readonly Dictionary<string, string[]> FbTitles = new Dictionary<string, string[]>
        {
            ["Политика"] = new[] { "Международный саммит прошёл в Москве", "Парламент принял важные поправки", "Президент выступил с обращением к нации", "Новые законы вступают в силу" },
            ["Технологии"] = new[] { "Новый смартфон побил рекорды продаж", "ИИ создал картину стоимостью миллион", "Кибербезопасность: новые угрозы", "Квантовый компьютер поставил рекорд" },
            ["Спорт"] = new[] { "Сборная вышла в финал чемпионата", "Новый рекорд в лёгкой атлетике", "Трансфер года: сделка на миллиард", "Олимпийские игры: итоги дня" },
            ["Бизнес"] = new[] { "Фондовый рынок показывает рост", "Крупная компания объявила о слиянии", "Нефть дорожает третий день подряд", "Стартап привлёк $500 млн инвестиций" },
            ["Здоровье"] = new[] { "Учёные нашли новый метод лечения", "ВОЗ предупреждает о новом вирусе", "Здоровый образ жизни продлевает жизнь", "Новая вакцина прошла испытания" },
            ["Наука"] = new[] { "Открыта новая планета у соседней звезды", "Физики доказали существование частицы", "Биологи расшифровали геном динозавра", "Марс: новые находки ровера" },
            ["Культура"] = new[] { "Новый фильм собрал $200 млн за выходные", "Концерт года: аншлаг в Москве", "Выставка открылась в Эрмитаже", "Букеровская премия объявила победителя" },
        };

        private List<NewsArticle> BuildFallbackNews(int count, string category = null)
        {
            var rng = new Random();
            var cats = category != null ? new[] { category } : FbCats;
            var list = new List<NewsArticle>();
            for (int i = 0; i < count; i++)
            {
                string cat = cats[i % cats.Length];
                string source = FbSources[rng.Next(FbSources.Length)];
                string author = FbAuthors[rng.Next(FbAuthors.Length)];
                string[] titles = FbTitles.ContainsKey(cat) ? FbTitles[cat] : FbTitles["Политика"];
                string title = titles[i % titles.Length];
                list.Add(new NewsArticle
                {
                    Title = title,
                    Description = $"Подробности события в категории «{cat}». Следите за обновлениями.",
                    Content = $"Редакция {source} сообщает: {title.ToLower()}. Эксперты дают комментарии, аналитики следят за развитием событий. Подробный репортаж читайте на сайте издания.",
                    Source = source,
                    Author = author,
                    Category = cat,
                    PublishedAt = DateTime.Now.AddHours(-rng.Next(1, 120)),
                    Url = $"https://example.com/news/{Guid.NewGuid()}",
                    ImageUrl = $"https://picsum.photos/seed/{cat}{i}/300/200",
                });
            }
            return list;
        }

        internal class GnApiResponse
        {
            [JsonProperty("status")] public string Status { get; set; }
            [JsonProperty("totalResults")] public int TotalResults { get; set; }
            [JsonProperty("articles")] public List<GnArticle> Articles { get; set; }
            [JsonProperty("message")] public string Message { get; set; }
        }

        internal class GnArticle
        {
            [JsonProperty("source")] public GnSource Source { get; set; }
            [JsonProperty("author")] public string Author { get; set; }
            [JsonProperty("title")] public string Title { get; set; }
            [JsonProperty("description")] public string Description { get; set; }
            [JsonProperty("url")] public string Url { get; set; }
            [JsonProperty("urlToImage")] public string UrlToImage { get; set; }
            [JsonProperty("publishedAt")] public string PublishedAt { get; set; }
            [JsonProperty("content")] public string Content { get; set; }
        }

        internal class GnSource
        {
            [JsonProperty("id")] public string Id { get; set; }
            [JsonProperty("name")] public string Name { get; set; }
        }
    }
}