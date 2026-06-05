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
        private const string ApiKey = "49a5c09f12b24cff89352fa0706c00f9";
        private const string BaseUrl = "https://newsapi.org/v2/";

        // ── Категории ──────────────────────────────────────────────────────

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

        // ── Русскоязычные источники (domains) ─────────────────────────────
        // NewsAPI /everything поддерживает фильтр по доменам — это гарантирует
        // русский язык без параметра language= (который ограничивает выборку)
        private const string RuDomains =
            "ria.ru,tass.ru,rbc.ru,lenta.ru,gazeta.ru,kommersant.ru," +
            "interfax.ru,iz.ru,rt.com,regnum.ru,fontanka.ru,znak.com," +
            "e1.ru,74.ru,ngs.ru,161.ru,nsk.ru,ural.ru";

        // Домены по регионам (для региональных новостей)
        private static readonly Dictionary<string, string> RegionDomains =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Москва"] = "mos.ru,m24.ru,vm.ru,moskvich-mag.ru,the-village.ru",
                ["Санкт-Петербург"] = "fontanka.ru,dp.ru,peterburg2.ru,spb.ru,nevnov.ru",
                ["Новосибирск"] = "ngs.ru,nsk.ru,sibkray.ru,tayga.info",
                ["Екатеринбург"] = "e1.ru,ural.ru,znak.com,eburg.ru,66.ru",
                ["Казань"] = "kazan.ru,kazanfirst.ru,business-gazeta.ru,realnoevremya.ru",
                ["Нижний Новгород"] = "nn.ru,niann.ru,novayagazeta-nn.ru",
                ["Челябинск"] = "74.ru,cheltoday.ru,mediazavod.ru,chel.ru",
                ["Омск"] = "ngs55.ru,bk55.ru,omskinform.ru",
                ["Самара"] = "63.ru,samaratoday.ru,volga.news",
                ["Ростов-на-Дону"] = "161.ru,rostov.ru,donnews.ru,rostovgazeta.ru",
                ["Уфа"] = "ufa.ru,bashinform.ru,proufu.ru,mkset.ru",
                ["Красноярск"] = "dela.ru,krsk.ru,newslab.ru,24rus.ru",
                ["Воронеж"] = "moe-online.ru,vrn.ru,kommuna.ru,voronezh-media.ru",
                ["Пермь"] = "59.ru,permkrai.ru,properm.ru,perm.ru",
                ["Волгоград"] = "v1.ru,volgograd.ru,volg.ru,riac34.ru",
                ["Магнитогорск"] = "magcity74.ru,magmetall.ru,magnitka-news.ru,mr-info.ru,magnitogorsk.ru,magnitogorsktv.ru", 
            };

        // Ключевые слова для регионального поиска как запасной вариант
        private static readonly Dictionary<string, string> RegionKeywords =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Москва"] = "Москва OR московский OR мэрия Москвы",
                ["Санкт-Петербург"] = "Петербург OR Санкт-Петербург OR петербургский",
                ["Новосибирск"] = "Новосибирск OR новосибирский OR Новосибирская область",
                ["Екатеринбург"] = "Екатеринбург OR Свердловская область OR уральский",
                ["Казань"] = "Казань OR Татарстан OR казанский",
                ["Нижний Новгород"] = "Нижний Новгород OR Нижегородская область",
                ["Челябинск"] = "Челябинск OR Челябинская область OR южный Урал",
                ["Омск"] = "Омск OR Омская область OR омский",
                ["Самара"] = "Самара OR Самарская область OR самарский",
                ["Ростов-на-Дону"] = "Ростов-на-Дону OR Ростовская область OR донской",
                ["Уфа"] = "Уфа OR Башкортостан OR башкирский",
                ["Красноярск"] = "Красноярск OR Красноярский край OR енисейский",
                ["Воронеж"] = "Воронеж OR Воронежская область OR воронежский",
                ["Пермь"] = "Пермь OR Пермский край OR пермский",
                ["Волгоград"] = "Волгоград OR Волгоградская область OR волжский",
                ["Магнитогорск"] = "Магнитогорск OR Челябинская область OR магнитогорский",
            };

        // Источники для международных категорий (en, работают стабильно)
        private static readonly Dictionary<string, string> CatSources =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["general"] = "bbc-news,reuters,associated-press,the-guardian-uk,al-jazeera-english",
                ["technology"] = "techcrunch,the-verge,wired,ars-technica",
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
        /// Основная лента с пагинацией.
        /// regionName != null → региональные русскоязычные новости.
        /// </summary>
        public async Task<NewsPageResult> GetTopHeadlinesPageAsync(
            string country = "ru",
            string categoryRu = null,
            int page = 1,
            int pageSize = 20,
            string regionName = null)
        {
            // Если передан регион — грузим региональные новости
            if (!string.IsNullOrEmpty(regionName))
                return await FetchRegionalNewsAsync(regionName, page, pageSize);

            if (string.IsNullOrEmpty(categoryRu) || categoryRu == "Все новости")
                return await FetchAllCategoriesAsync(page, pageSize);

            string apiCat = CategoryMap.ContainsKey(categoryRu) ? CategoryMap[categoryRu] : "general";
            return await FetchCategoryPageAsync(apiCat, categoryRu, page, pageSize);
        }

        /// <summary>Совместимость.</summary>
        public async Task<List<NewsArticle>> GetTopHeadlinesAsync(
            string country = "ru", string categoryRu = null, int pageSize = 20)
            => (await GetTopHeadlinesPageAsync(country, categoryRu, 1, pageSize)).Articles;

        /// <summary>Поиск с пагинацией. Сначала ищет по-русски, затем по-английски.</summary>
        public async Task<NewsPageResult> SearchNewsPageAsync(
            string query, string language = "ru", int page = 1, int pageSize = 20)
        {
            // Попытка 1: русские домены + запрос
            string url = $"{BaseUrl}everything" +
                         $"?q={Uri.EscapeDataString(query)}" +
                         $"&domains={RuDomains}" +
                         $"&page={page}&pageSize={pageSize}&sortBy=publishedAt";
            var (arts, total) = await FetchWithTotalAsync(url, "Результаты поиска");
            if (arts.Count > 0)
                return new NewsPageResult { Articles = arts, TotalResults = total };

            // Попытка 2: language=ru без ограничения домена
            url = $"{BaseUrl}everything" +
                  $"?q={Uri.EscapeDataString(query)}" +
                  $"&language=ru" +
                  $"&page={page}&pageSize={pageSize}&sortBy=publishedAt";
            (arts, total) = await FetchWithTotalAsync(url, "Результаты поиска");
            if (arts.Count > 0)
                return new NewsPageResult { Articles = arts, TotalResults = total };

            // Попытка 3: английский запрос
            url = $"{BaseUrl}everything" +
                  $"?q={Uri.EscapeDataString(query)}" +
                  $"&language=en" +
                  $"&page={page}&pageSize={pageSize}&sortBy=publishedAt";
            (arts, total) = await FetchWithTotalAsync(url, "Результаты поиска");
            if (arts.Count > 0)
                return new NewsPageResult { Articles = arts, TotalResults = total };

            return new NewsPageResult { Articles = new List<NewsArticle>(), TotalResults = 0 };
        }

        public async Task<List<NewsArticle>> SearchNewsAsync(
            string query, string language = "ru", int pageSize = 30)
            => (await SearchNewsPageAsync(query, language, 1, pageSize)).Articles;

        // ── Региональные новости ───────────────────────────────────────────

        private async Task<NewsPageResult> FetchRegionalNewsAsync(
            string regionName, int page, int pageSize)
        {
            // Стратегия 1: домены регионального издания
            if (RegionDomains.TryGetValue(regionName, out string domains))
            {
                string url = $"{BaseUrl}everything" +
                             $"?domains={domains}" +
                             $"&page={page}&pageSize={pageSize}&sortBy=publishedAt";
                var (arts, total) = await FetchWithTotalAsync(url, regionName);
                if (arts.Count > 0)
                {
                    arts.ForEach(a => a.Category = regionName);
                    return new NewsPageResult { Articles = arts, TotalResults = total };
                }
            }

            // Стратегия 2: ключевые слова региона + русские домены
            if (RegionKeywords.TryGetValue(regionName, out string keywords))
            {
                string url = $"{BaseUrl}everything" +
                             $"?q={Uri.EscapeDataString(keywords)}" +
                             $"&domains={RuDomains}" +
                             $"&page={page}&pageSize={pageSize}&sortBy=publishedAt";
                var (arts, total) = await FetchWithTotalAsync(url, regionName);
                if (arts.Count > 0)
                {
                    arts.ForEach(a => a.Category = regionName);
                    return new NewsPageResult { Articles = arts, TotalResults = total };
                }
            }

            // Стратегия 3: только ключевые слова, language=ru
            if (RegionKeywords.TryGetValue(regionName, out string kw2))
            {
                string url = $"{BaseUrl}everything" +
                             $"?q={Uri.EscapeDataString(kw2)}" +
                             $"&language=ru" +
                             $"&page={page}&pageSize={pageSize}&sortBy=publishedAt";
                var (arts, total) = await FetchWithTotalAsync(url, regionName);
                if (arts.Count > 0)
                {
                    arts.ForEach(a => a.Category = regionName);
                    return new NewsPageResult { Articles = arts, TotalResults = total };
                }
            }

            var fb = BuildFallbackNews(pageSize, regionName);
            return new NewsPageResult { Articles = fb, TotalResults = fb.Count };
        }

        // ── Общая лента (все категории) ────────────────────────────────────

        private async Task<NewsPageResult> FetchAllCategoriesAsync(int page, int pageSize)
        {
            // Сначала пробуем русские источники через /everything
            string ruUrl = $"{BaseUrl}everything" +
                           $"?q=Россия OR политика OR экономика OR спорт" +
                           $"&domains={RuDomains}" +
                           $"&page={page}&pageSize={pageSize}&sortBy=publishedAt";
            var (ruArts, ruTotal) = await FetchWithTotalAsync(ruUrl, "Новости");
            if (ruArts.Count > 0)
            {
                AssignCategoriesToArticles(ruArts);
                return new NewsPageResult { Articles = ruArts, TotalResults = ruTotal };
            }

            // Затем международные источники по категориям параллельно
            int perCat = Math.Max(3, pageSize / AllApiCats.Length + 1);
            var tasks = AllApiCats
                .Select(cat => FetchCategoryPageAsync(cat, ApiCatToRu[cat], page, perCat))
                .ToList();

            try
            {
                var results = await Task.WhenAll(tasks);
                var all = results.SelectMany(r => r.Articles).OrderByDescending(a => a.PublishedAt).ToList();
                int total = results.Sum(r => r.TotalResults);
                if (all.Count > 0)
                    return new NewsPageResult { Articles = all, TotalResults = total };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NewsApi] FetchAll error: {ex.Message}");
            }

            var fb = BuildFallbackNews(pageSize);
            return new NewsPageResult { Articles = fb, TotalResults = fb.Count };
        }

        // Распределяем категории по ключевым словам в заголовке/описании
        private static void AssignCategoriesToArticles(List<NewsArticle> articles)
        {
            var rules = new[]
            {
                (cat: "Спорт",       keys: new[]{ "спорт","футбол","хоккей","матч","чемпионат","сборная","олимпи","теннис","баскетбол" }),
                (cat: "Технологии",  keys: new[]{ "технолог","искусственный интеллект","цифров","ИИ","смартфон","программ","интернет","кибер" }),
                (cat: "Бизнес",      keys: new[]{ "бизнес","экономик","рынок","банк","финанс","инвестиц","компани","акци","рубль","биржа" }),
                (cat: "Здоровье",    keys: new[]{ "здоровь","медицин","врач","больниц","лечени","вакцин","пандеми","вирус","препарат" }),
                (cat: "Наука",       keys: new[]{ "наук","учёный","исследовани","открыт","физик","биолог","космос","астроном","эксперимент" }),
                (cat: "Культура",    keys: new[]{ "культур","кино","театр","музык","фильм","концерт","выставк","искусство","книг","премьер" }),
            };

            foreach (var a in articles)
            {
                string text = ((a.Title ?? "") + " " + (a.Description ?? "")).ToLowerInvariant();
                bool assigned = false;
                foreach (var (cat, keys) in rules)
                {
                    if (keys.Any(k => text.Contains(k)))
                    {
                        a.Category = cat;
                        assigned = true;
                        break;
                    }
                }
                if (!assigned) a.Category = "Политика";
            }
        }

        // ── По категории ───────────────────────────────────────────────────

        private async Task<NewsPageResult> FetchCategoryPageAsync(
            string apiCat, string ruCat, int page, int pageSize)
        {
            // Стратегия 1: top-headlines по sources (без country)
            if (CatSources.TryGetValue(apiCat, out string sources))
            {
                string url = $"{BaseUrl}top-headlines?sources={sources}&page={page}&pageSize={pageSize}";
                var (arts, total) = await FetchWithTotalAsync(url, ruCat);
                if (arts.Count > 0) return new NewsPageResult { Articles = arts, TotalResults = total };
            }

            // Стратегия 2: top-headlines category без country
            {
                string url = $"{BaseUrl}top-headlines?category={apiCat}&page={page}&pageSize={pageSize}";
                var (arts, total) = await FetchWithTotalAsync(url, ruCat);
                if (arts.Count > 0) return new NewsPageResult { Articles = arts, TotalResults = total };
            }

            // Стратегия 3: everything с английскими ключевыми словами
            {
                string url = $"{BaseUrl}everything" +
                             $"?q={Uri.EscapeDataString(CatToEnglishQuery(apiCat))}" +
                             $"&language=en&page={page}&pageSize={pageSize}&sortBy=publishedAt";
                var (arts, total) = await FetchWithTotalAsync(url, ruCat);
                if (arts.Count > 0) return new NewsPageResult { Articles = arts, TotalResults = total };
            }

            var fb = BuildFallbackNews(pageSize, ruCat);
            return new NewsPageResult { Articles = fb, TotalResults = fb.Count };
        }

        // ── HTTP ───────────────────────────────────────────────────────────

        private async Task<(List<NewsArticle> articles, int totalResults)> FetchWithTotalAsync(
            string url, string defaultCategory)
        {
            try
            {
                var resp = await _http.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
                var json = await resp.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"[NewsApi] {resp.StatusCode} {url}");

                if (!resp.IsSuccessStatusCode) return (new List<NewsArticle>(), 0);

                var data = JsonConvert.DeserializeObject<GnApiResponse>(json);
                if (data?.Status != "ok" || data.Articles == null || data.Articles.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[NewsApi] No articles: {data?.Message}");
                    return (new List<NewsArticle>(), 0);
                }

                System.Diagnostics.Debug.WriteLine($"[NewsApi] Got {data.Articles.Count} / {data.TotalResults}");

                var arts = data.Articles
                    .Where(a => !string.IsNullOrEmpty(a.Title) && a.Title != "[Removed]")
                    .Select(a => MapToArticle(a, defaultCategory))
                    .ToList();

                return (arts, data.TotalResults);
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
                Source = a.Source?.Name ?? "Неизвестно",
                Author = string.IsNullOrWhiteSpace(a.Author) ? "Редакция" : a.Author,
                Category = defaultCategory
            };
        }

        private static string StripSuffix(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            int i = s.IndexOf(" [+", StringComparison.Ordinal);
            return i > 0 ? s.Substring(0, i) : s;
        }

        private static string CatToEnglishQuery(string apiCat)
        {
            switch (apiCat)
            {
                case "technology": return "technology OR AI OR software OR gadgets";
                case "sports": return "sports OR football OR basketball OR tennis";
                case "business": return "business OR economy OR finance OR market";
                case "health": return "health OR medicine OR vaccine OR disease";
                case "science": return "science OR research OR space OR discovery";
                case "entertainment": return "entertainment OR movies OR music OR culture";
                default: return "politics OR government OR world news";
            }
        }

        // ── Fallback ───────────────────────────────────────────────────────

        private static readonly string[] FbCats = { "Политика", "Технологии", "Спорт", "Бизнес", "Здоровье", "Наука", "Культура" };
        private static readonly string[] FbSources = { "РИА Новости", "ТАСС", "Интерфакс", "РБК", "Газета.ру", "Коммерсантъ", "Лента.ру" };
        private static readonly string[] FbAuthors = { "Иван Иванов", "Анна Петрова", "Сергей Сидоров", "Мария Кузнецова", "Алексей Смирнов" };

        private static readonly Dictionary<string, string[]> FbTitles =
            new Dictionary<string, string[]>
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
                    Title = $"[Офлайн] {title}",
                    Description = $"Нет подключения к интернету. Это заглушка для категории «{cat}».",
                    Content = $"{source} сообщает: {title.ToLower()}. Подключитесь к интернету для загрузки актуальных новостей.",
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

        // ── JSON-модели ────────────────────────────────────────────────────

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