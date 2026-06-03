using System;
using System.Collections.Generic;

namespace WpfApp1
{
    // Основная модель новости для приложения
    public class NewsArticle
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public DateTime PublishedAt { get; set; }
        public string Source { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }

        public string ShortDescription
        {
            get
            {
                if (string.IsNullOrEmpty(Description)) return "";
                return Description.Length > 100 ? Description.Substring(0, 100) + "..." : Description;
            }
        }

        public string FormattedDate
        {
            get { return PublishedAt.ToString("dd.MM.yyyy HH:mm"); }
        }
    }

    // Модели для NewsAPI
    public class NewsApiResponse
    {
        public string Status { get; set; }
        public int TotalResults { get; set; }
        public List<ApiArticle> Articles { get; set; }
    }

    public class ApiArticle
    {
        public ApiSource Source { get; set; }
        public string Author { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string UrlToImage { get; set; }
        public string PublishedAt { get; set; }
        public string Content { get; set; }
    }

    public class UserProfile
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Bio { get; set; }
        public string AvatarUrl { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Модель для обновления профиля
    public class UpdateProfileRequest
    {
        public string DisplayName { get; set; }
        public string Bio { get; set; }
        public string AvatarUrl { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class ApiSource
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}