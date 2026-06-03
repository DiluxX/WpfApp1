using System;

namespace WpfApp1
{
    // Основная модель новости — используется везде в приложении
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
                return Description.Length > 120
                    ? Description.Substring(0, 120) + "..."
                    : Description;
            }
        }

        public string FormattedDate
        {
            get { return PublishedAt.ToString("dd.MM.yyyy HH:mm"); }
        }
    }

    // Профиль пользователя
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

    // Запрос на обновление профиля
    public class UpdateProfileRequest
    {
        public string DisplayName { get; set; }
        public string Bio { get; set; }
        public string AvatarUrl { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}
