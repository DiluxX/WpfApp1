using System;

namespace WpfApp1
{
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
                    ? Description.Substring(0, 120) + "..." : Description;
            }
        }
        public string FormattedDate => PublishedAt.ToString("dd.MM.yyyy HH:mm");
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

    public class UpdateProfileRequest
    {
        public string DisplayName { get; set; }
        public string Bio { get; set; }
        public string AvatarUrl { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }

    public class FavoriteArticle
    {
        public int FavoriteId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public string Source { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public DateTime PublishedAt { get; set; }
        public DateTime AddedAt { get; set; }

        public string FormattedDate => PublishedAt.ToString("dd.MM.yyyy HH:mm");
        public string FormattedAddedAt => AddedAt.ToString("dd.MM.yyyy HH:mm");
        public string ShortDescription
        {
            get
            {
                if (string.IsNullOrEmpty(Description)) return "";
                return Description.Length > 120
                    ? Description.Substring(0, 120) + "..." : Description;
            }
        }
    }

    public class FavoriteComment
    {
        public int CommentId { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; }

        public string FormattedDate => CreatedAt.ToString("dd.MM.yyyy HH:mm");
    }
}