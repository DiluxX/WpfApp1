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
        public List<NewsArticle> Articles { get; set; } = new List<NewsArticle>();
        public int TotalResults { get; set; }
    }

    public class NewsApiService
    {
        private readonly HttpClient _http;

        // ── Актуальный ключ ───────────────────────────────────────────────
        private const string ApiKey = "bcfe5ae4a7b69625891553c25a3b9938";
        private const string BaseUrl = "https://newsapi.org/v2/";

        // Русская категория → параметр NewsAPI category
        private static readonly Dictionary<string, string> CategoryMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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

        // Параметр NewsAPI → русская категория
        private static readonly Dictionary<string, string> ApiCatToRu =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["general"] = "Политика",
                ["technology"] = "Технологии",
                ["sports"] = "Спорт",
                ["business"] = "Бизнес",
                ["health"] = "Здоровье",
                ["science"] = "Наука",
                ["entertainment"] = "Культура",
            };

        private static readonly string[] AllApiCats =
            { "general", "technology", "sports", "business", "health", "science", "entertainment" };

        // Крупные источники, которые NewsAPI отдаёт на бесплатном плане
        // (без country= ограничений)
        private static readonly Dictionary<string, string> CatSources =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["general"] = "bbc-news,reuters,associated-press,the-guardian-uk,al-jazeera-english",
                ["technology"] = "techcrunch,the-verge,wired,ars-technica,hacker-news",
                ["sports"] = "bbc-sport,espn,bleacher-report,fox-sports",
                ["business"] = "bloomberg,business-insider,financial-times,fortune",
                ["health"] = "medical-news-today",
                ["science"] = "new-scientist,national-geographic",
                ["entertainment"] = "entertainment-weekly,buzzfeed",
            };

        public NewsApiService()
        {
            var handler = new HttpClientHandler
            {
                UseProxy = false,
                ServerCertificateCustomValidationCallback = (s, c, ch, e) => true
            };
            _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(25) };
            _http.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _http.DefaultRequestHeaders.Add("Accept", "application/json");
            _http.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        }

        // ── Публичный API ──────────────────────────────────────────────────

        /// <summary>
        /// Основной метод для ленты. Поддерживает пагинацию.
        /// categoryRu = null / "Все новости" → все категории параллельно.
        /// </summary>
        public async Task<NewsPageResult> GetTopHeadlinesPageAsync(
            string country = "ru",
            string categoryRu = null,
            int page = 1,
            int pageSize = 20)
        {
            if (string.IsNullOrEmpty(categoryRu) || categoryRu == "Все новости")
                return await FetchAllCategoriesPageAsync(page, pageSize);

            string apiCat = CategoryMap.ContainsKey(categoryRu) ? CategoryMap[categoryRu] : "general";
            return await FetchCategoryPageAsync(apiCat, categoryRu, page, pageSize);
        }

        /// <summary>Совместимость со старым кодом.</summary>
        public async Task<List<NewsArticle>> GetTopHeadlinesAsync(
            string country = "ru", string categoryRu = null, int pageSize = 20)
        {
            var result = await GetTopHeadlinesPageAsync(country, categoryRu, 1, pageSize);
            return result.Articles;
        }

        /// <summary>Поиск по ключевому слову через /everything.</summary>
        public async Task<List<NewsArticle>> SearchNewsAsync(
            string query, string language = "en", int pageSize = 30)
        {
            var result = await SearchNewsPageAsync(query, language, 1, pageSize);
            return result.Articles;
        }

        /// <summary>Поиск с пагинацией.</summary>
        public async Task<NewsPageResult> SearchNewsPageAsync(
            string query, string language = "en", int page = 1, int pageSize = 20)
        {
            // Пробуем английский вариант запроса (NewsAPI лучше работает с en)
            string url = $"{BaseUrl}everything" +
                         $"?q={Uri.EscapeDataString(query)}" +
                         $"&language=en" +
                         $"&page={page}&pageSize={pageSize}" +
                         $"&sortBy=publishedAt";

            var (articles, total) = await FetchWithTotalAsync(url, "Результаты поиска");
            if (articles.Count > 0)
                return new NewsPageResult { Articles = articles, TotalResults = total };

            // Если ничего — ищем без фильтра языка
            url = $"{BaseUrl}everything" +
                  $"?q={Uri.EscapeDataString(query)}" +
                  $"&page={page}&pageSize={pageSize}" +
                  $"&sortBy=publishedAt";

            (articles, total) = await FetchWithTotalAsync(url, "Результаты поиска");
            if (articles.Count > 0)
                return new NewsPageResult { Articles = articles, TotalResults = total };

            return new NewsPageResult { Articles = new List<NewsArticle>(), TotalResults = 0 };
        }

        // ── Внутренние методы ──────────────────────────────────────────────

        private async Task<NewsPageResult> FetchAllCategoriesPageAsync(int page, int pageSize)
        {
            // Берём по несколько статей из каждой категории параллельно
            int perCat = Math.Max(3, pageSize / AllApiCats.Length + 1);
            var tasks = AllApiCats
                .Select(cat => FetchCategoryPageAsync(cat, ApiCatToRu[cat], page, perCat))
                .ToList();

            try
            {
                var results = await Task.WhenAll(tasks);
                var all = results.SelectMany(r => r.Articles).ToList();
                int total = results.Sum(r => r.TotalResults);

                if (all.Count > 0)
                    return new NewsPageResult
                    {
                        Articles = all.OrderByDescending(a => a.PublishedAt).ToList(),
                        TotalResults = total
                    };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NewsApi] FetchAll error: {ex.Message}");
            }

            // Полный fallback
            var fb = BuildFallbackNews(pageSize);
            return new NewsPageResult { Articles = fb, TotalResults = fb.Count };
        }

        private async Task<NewsPageResult> FetchCategoryPageAsync(
            string apiCat, string ruCat, int page, int pageSize)
        {
            // ── Стратегия 1: top-headlines с sources (без country!) ──────────
            if (CatSources.TryGetValue(apiCat, out string sources))
            {
                string url = $"{BaseUrl}top-headlines" +
                             $"?sources={sources}" +
                             $"&page={page}&pageSize={pageSize}";

                var (articles, total) = await FetchWithTotalAsync(url, ruCat);
                if (articles.Count > 0)
                    return new NewsPageResult { Articles = articles, TotalResults = total };
            }

            // ── Стратегия 2: top-headlines с category (без country) ──────────
            {
                string url = $"{BaseUrl}top-headlines" +
                             $"?category={apiCat}" +
                             $"&page={page}&pageSize={pageSize}";

                var (articles, total) = await FetchWithTotalAsync(url, ruCat);
                if (articles.Count > 0)
                    return new NewsPageResult { Articles = articles, TotalResults = total };
            }

            // ── Стратегия 3: everything с английскими ключевыми словами ─────
            {
                string q = CatToEnglishQuery(apiCat);
                string url = $"{BaseUrl}everything" +
                             $"?q={Uri.EscapeDataString(q)}" +
                             $"&language=en" +
                             $"&page={page}&pageSize={pageSize}" +
                             $"&sortBy=publishedAt";

                var (articles, total) = await FetchWithTotalAsync(url, ruCat);
                if (articles.Count > 0)
                    return new NewsPageResult { Articles = articles, TotalResults = total };
            }

            // ── Финальный fallback ───────────────────────────────────────────
            var fb = BuildFallbackNews(pageSize, ruCat);
            return new NewsPageResult { Articles = fb, TotalResults = fb.Count };
        }

        private async Task<(List<NewsArticle> articles, int totalResults)> FetchWithTotalAsync(
            string url, string defaultCategory)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                var resp = await _http.SendAsync(req);
                var json = await resp.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[NewsApi] GET {url}");
                System.Diagnostics.Debug.WriteLine($"[NewsApi] Status: {resp.StatusCode}");

                if (!resp.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"[NewsApi] Error body: {json}");
                    return (new List<NewsArticle>(), 0);
                }

                var data = JsonConvert.DeserializeObject<GnApiResponse>(json);
                if (data?.Status != "ok" || data.Articles == null || data.Articles.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[NewsApi] No articles. status={data?.Status} msg={data?.Message}");
                    return (new List<NewsArticle>(), 0);
                }

                System.Diagnostics.Debug.WriteLine($"[NewsApi] Got {data.Articles.Count} articles (total={data.TotalResults})");

                var articles = data.Articles
                    .Where(a => !string.IsNullOrEmpty(a.Title) && a.Title != "[Removed]")
                    .Select(a => MapToArticle(a, defaultCategory))
                    .ToList();

                return (articles, data.TotalResults);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NewsApi] Exception: {ex.Message}");
                return (new List<NewsArticle>(), 0);
            }
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
                Source = a.Source?.Name ?? "Unknown",
                Author = string.IsNullOrWhiteSpace(a.Author) ? "Staff" : a.Author,
                Category = defaultCategory
            };
        }

        // Убираем "[+N chars]" которые NewsAPI добавляет к обрезанному контенту
        private static string StripSuffix(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int i = s.IndexOf(" [+", StringComparison.Ordinal);
            return i > 0 ? s.Substring(0, i) : s;
        }

        // Английские запросы — NewsAPI /everything лучше работает с en
        private static string CatToEnglishQuery(string apiCat)
        {
            switch (apiCat)
            {
                case "technology": return "technology OR AI OR software OR gadgets";
                case "sports": return "sports OR football OR basketball OR tennis";
                case "business": return "business OR economy OR finance OR market OR stocks";
                case "health": return "health OR medicine OR vaccine OR disease";
                case "science": return "science OR research OR space OR discovery";
                case "entertainment": return "entertainment OR movies OR music OR culture";
                default: return "politics OR government OR world news";
            }
        }

        // ── Fallback-данные (только если API полностью недоступен) ─────────

        private static readonly string[] FbCats =
            { "Политика", "Технологии", "Спорт", "Бизнес", "Здоровье", "Наука", "Культура" };

        private static readonly string[] FbSources =
            { "BBC News", "Reuters", "AP News", "The Guardian", "Bloomberg", "TechCrunch", "ESPN" };

        private static readonly string[] FbAuthors =
            { "John Smith", "Anna Brown", "Michael Johnson", "Sarah Davis", "Robert Wilson" };

        private static readonly Dictionary<string, string[]> FbTitles =
            new Dictionary<string, string[]>
            {
                ["Политика"] = new[] { "World leaders meet at international summit", "New legislation passes in parliament", "Diplomatic talks resume between nations", "Election results shape future policy" },
                ["Технологии"] = new[] { "New AI model breaks performance records", "Tech giant announces major product launch", "Cybersecurity threats on the rise globally", "Quantum computing reaches new milestone" },
                ["Спорт"] = new[] { "Championship final draws record viewers", "Athlete breaks world record at major event", "Transfer season: biggest deals revealed", "Tournament results: upsets and surprises" },
                ["Бизнес"] = new[] { "Markets rally on positive economic data", "Major merger announced in tech sector", "Oil prices fluctuate amid global tensions", "Startup secures $500M in funding round" },
                ["Здоровье"] = new[] { "Scientists discover new treatment method", "WHO issues health advisory for new strain", "Study links lifestyle choices to longevity", "New vaccine shows promising trial results" },
                ["Наука"] = new[] { "Astronomers discover exoplanet in habitable zone", "Physicists confirm existence of new particle", "Fossils reveal unknown dinosaur species", "Mars rover finds evidence of ancient water" },
                ["Культура"] = new[] { "Blockbuster film breaks box office records", "Major music festival announces lineup", "Landmark exhibition opens at national museum", "Prestigious literary award winner announced" },
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
                    Title = $"[Offline] {title}",
                    Description = $"This is a placeholder article for the '{cat}' category. Connect to the internet to load real news.",
                    Content = $"{source} reports: {title.ToLower()}. Full coverage available on our website.",
                    Source = source,
                    Author = author,
                    Category = cat,
                    PublishedAt = DateTime.Now.AddHours(-rng.Next(1, 48)),
                    Url = $"https://example.com/news/{Guid.NewGuid()}",
                    ImageUrl = $"https://picsum.photos/seed/{i + 10}/300/200",
                });
            }

            return list;
        }

        // ── JSON-модели (internal — не конфликтуют с Models.cs) ───────────

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