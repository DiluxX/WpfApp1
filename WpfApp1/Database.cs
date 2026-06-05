using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;

namespace WpfApp1
{
    internal class Database
    {
        private string connectionString = "Host=localhost;Port=5432;Database=gstv_CP;Username=postgres;Password=12345;";

        public (bool success, int userId, string username, string role) AuthenticateUser(string username, string password)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT u.user_id, u.username, r.name
                                     FROM users u JOIN roles r ON u.role_id = r.role_id
                                     WHERE u.username = @username AND u.password = @password";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                UpdateLastLogin(userId);
                                return (true, userId, reader.GetString(1), reader.GetString(2));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return (false, 0, "", "");
        }

        private void UpdateLastLogin(int userId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(
                        "UPDATE users SET last_login = CURRENT_TIMESTAMP WHERE user_id = @userId", connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        public UserProfile GetUserProfile(int userId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT u.user_id, u.username,
                        COALESCE(u.display_name, u.username),
                        COALESCE(u.email,''), COALESCE(u.bio,''), COALESCE(u.avatar_url,''),
                        r.name, u.created_at,
                        COALESCE(u.last_login, u.created_at),
                        COALESCE(u.updated_at, u.created_at)
                        FROM users u JOIN roles r ON u.role_id = r.role_id
                        WHERE u.user_id = @userId";
                    using (var cmd = new NpgsqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                                return new UserProfile
                                {
                                    UserId = reader.GetInt32(0),
                                    Username = reader.GetString(1),
                                    DisplayName = reader.GetString(2),
                                    Email = reader.GetString(3),
                                    Bio = reader.GetString(4),
                                    AvatarUrl = reader.GetString(5),
                                    Role = reader.GetString(6),
                                    CreatedAt = reader.GetDateTime(7),
                                    LastLogin = reader.GetDateTime(8),
                                    UpdatedAt = reader.GetDateTime(9)
                                };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return null;
        }

        public void InitializeDatabaseStructure()
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // Поля users
                    string[] cols = { "display_name VARCHAR(255)", "bio TEXT",
                                      "avatar_url VARCHAR(500)", "last_login TIMESTAMP",
                                      "updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP" };
                    foreach (var col in cols)
                    {
                        string name = col.Split(' ')[0];
                        ExecuteNonQuery(connection, $@"DO $$ BEGIN
                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                                WHERE table_name='users' AND column_name='{name}')
                            THEN EXECUTE 'ALTER TABLE users ADD COLUMN {col}'; END IF; END $$");
                    }

                    // Таблица избранных
                    ExecuteNonQuery(connection, @"CREATE TABLE IF NOT EXISTS favorites (
                        favorite_id  SERIAL PRIMARY KEY,
                        user_id      INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
                        title        TEXT    NOT NULL,
                        description  TEXT,
                        url          TEXT,
                        image_url    TEXT,
                        source       TEXT,
                        author       TEXT,
                        category     TEXT,
                        published_at TIMESTAMP,
                        added_at     TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )");

                    // Таблица комментариев к избранным
                    ExecuteNonQuery(connection, @"CREATE TABLE IF NOT EXISTS favorite_comments (
                        comment_id   SERIAL PRIMARY KEY,
                        favorite_id  INTEGER NOT NULL REFERENCES favorites(favorite_id) ON DELETE CASCADE,
                        user_id      INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
                        comment_text TEXT    NOT NULL,
                        created_at   TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )");

                    ExecuteNonQuery(connection,
                        "CREATE INDEX IF NOT EXISTS idx_favorites_user ON favorites(user_id)");
                    ExecuteNonQuery(connection,
                        "CREATE INDEX IF NOT EXISTS idx_fav_comments_fav ON favorite_comments(favorite_id)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации БД: {ex.Message}");
            }
        }

        private void ExecuteNonQuery(NpgsqlConnection conn, string sql)
        {
            using (var cmd = new NpgsqlCommand(sql, conn))
                cmd.ExecuteNonQuery();
        }

        // ── Избранное ──────────────────────────────────────────────────────

        public bool AddToFavorites(int userId, NewsArticle article)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    // Не добавляем дубликаты
                    using (var check = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM favorites WHERE user_id=@uid AND url=@url", connection))
                    {
                        check.Parameters.AddWithValue("@uid", userId);
                        check.Parameters.AddWithValue("@url", article.Url ?? "");
                        if (Convert.ToInt32(check.ExecuteScalar()) > 0) return false;
                    }
                    using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO favorites
                            (user_id,title,description,url,image_url,source,author,category,published_at)
                        VALUES
                            (@uid,@title,@desc,@url,@img,@src,@auth,@cat,@pub)", connection))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@title", article.Title ?? "");
                        cmd.Parameters.AddWithValue("@desc", article.Description ?? "");
                        cmd.Parameters.AddWithValue("@url", article.Url ?? "");
                        cmd.Parameters.AddWithValue("@img", article.ImageUrl ?? "");
                        cmd.Parameters.AddWithValue("@src", article.Source ?? "");
                        cmd.Parameters.AddWithValue("@auth", article.Author ?? "");
                        cmd.Parameters.AddWithValue("@cat", article.Category ?? "");
                        cmd.Parameters.AddWithValue("@pub", article.PublishedAt);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления в избранное: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool RemoveFromFavorites(int userId, string url)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(
                        "DELETE FROM favorites WHERE user_id=@uid AND url=@url", connection))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@url", url ?? "");
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch { return false; }
        }

        public bool IsFavorite(int userId, string url)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM favorites WHERE user_id=@uid AND url=@url", connection))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@url", url ?? "");
                        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    }
                }
            }
            catch { return false; }
        }

        public List<FavoriteArticle> GetFavorites(int userId)
        {
            var list = new List<FavoriteArticle>();
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT favorite_id,title,description,url,image_url,
                               source,author,category,published_at,added_at
                        FROM favorites WHERE user_id=@uid
                        ORDER BY added_at DESC", connection))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                list.Add(new FavoriteArticle
                                {
                                    FavoriteId = reader.GetInt32(0),
                                    Title = reader.GetString(1),
                                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    Url = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    ImageUrl = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    Source = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                    Author = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                    Category = reader.IsDBNull(7) ? "" : reader.GetString(7),
                                    PublishedAt = reader.IsDBNull(8) ? DateTime.Now : reader.GetDateTime(8),
                                    AddedAt = reader.GetDateTime(9)
                                });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки избранного: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return list;
        }

        // ── Комментарии к избранному ───────────────────────────────────────

        public bool AddComment(int favoriteId, int userId, string text)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(@"
                        INSERT INTO favorite_comments (favorite_id,user_id,comment_text)
                        VALUES (@fid,@uid,@txt)", connection))
                    {
                        cmd.Parameters.AddWithValue("@fid", favoriteId);
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@txt", text);
                        cmd.ExecuteNonQuery();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления комментария: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public List<FavoriteComment> GetComments(int favoriteId)
        {
            var list = new List<FavoriteComment>();
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT fc.comment_id, fc.comment_text, fc.created_at,
                               COALESCE(u.display_name, u.username)
                        FROM favorite_comments fc
                        JOIN users u ON fc.user_id = u.user_id
                        WHERE fc.favorite_id = @fid
                        ORDER BY fc.created_at ASC", connection))
                    {
                        cmd.Parameters.AddWithValue("@fid", favoriteId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                list.Add(new FavoriteComment
                                {
                                    CommentId = reader.GetInt32(0),
                                    Text = reader.GetString(1),
                                    CreatedAt = reader.GetDateTime(2),
                                    AuthorName = reader.GetString(3)
                                });
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        // ── Остальные методы (профиль, роли) ──────────────────────────────

        public bool UpdateUserProfile(int userId, UpdateProfileRequest request)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    if (!string.IsNullOrEmpty(request.NewPassword))
                        if (!VerifyPassword(userId, request.CurrentPassword))
                        {
                            MessageBox.Show("Текущий пароль неверен", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var updates = new List<string>();
                            var parameters = new List<NpgsqlParameter>
                                { new NpgsqlParameter("@userId", userId) };

                            if (!string.IsNullOrEmpty(request.DisplayName))
                            {
                                updates.Add("display_name = @displayName");
                                parameters.Add(new NpgsqlParameter("@displayName", request.DisplayName));
                            }
                            if (request.Bio != null)
                            {
                                updates.Add("bio = @bio");
                                parameters.Add(new NpgsqlParameter("@bio", request.Bio));
                            }
                            if (!string.IsNullOrEmpty(request.AvatarUrl) && request.AvatarUrl.StartsWith("http"))
                            {
                                updates.Add("avatar_url = @avatarUrl");
                                parameters.Add(new NpgsqlParameter("@avatarUrl", request.AvatarUrl));
                            }
                            if (!string.IsNullOrEmpty(request.NewPassword))
                            {
                                updates.Add("password = @newPassword");
                                parameters.Add(new NpgsqlParameter("@newPassword", request.NewPassword));
                            }
                            updates.Add("updated_at = CURRENT_TIMESTAMP");

                            if (updates.Count == 0) return false;

                            using (var cmd = new NpgsqlCommand(
                                $"UPDATE users SET {string.Join(", ", updates)} WHERE user_id = @userId",
                                connection))
                            {
                                cmd.Transaction = transaction;
                                foreach (var p in parameters) cmd.Parameters.Add(p);
                                if (cmd.ExecuteNonQuery() > 0) { transaction.Commit(); return true; }
                                transaction.Rollback(); return false;
                            }
                        }
                        catch { transaction.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления профиля: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool VerifyPassword(int userId, string password)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM users WHERE user_id=@uid AND password=@pwd", connection))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        cmd.Parameters.AddWithValue("@pwd", password);
                        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    }
                }
            }
            catch { return false; }
        }

        public Dictionary<string, bool> GetRolePermissions(string role)
        {
            var p = new Dictionary<string, bool>
            {
                ["CanViewNews"] = true,
                ["CanAddNews"] = false,
                ["CanEditNews"] = false,
                ["CanDeleteNews"] = false,
                ["CanAddComments"] = false,
                ["CanEditComments"] = false,
                ["CanDeleteComments"] = false,
                ["CanModerateComments"] = false,
                ["CanManageUsers"] = false,
                ["CanModerateNews"] = false,
                ["CanEditProfile"] = true
            };
            switch (role?.ToLower())
            {
                case "admin":
                    p["CanAddNews"] = p["CanEditNews"] = p["CanDeleteNews"] = true;
                    p["CanAddComments"] = p["CanEditComments"] = p["CanDeleteComments"] = true;
                    p["CanModerateComments"] = p["CanManageUsers"] = p["CanModerateNews"] = true;
                    break;
                case "manager":
                    p["CanAddNews"] = p["CanEditNews"] = true;
                    p["CanAddComments"] = p["CanEditComments"] = p["CanDeleteComments"] = true;
                    p["CanModerateComments"] = p["CanManageUsers"] = p["CanModerateNews"] = true;
                    break;
                case "user":
                    p["CanAddComments"] = true;
                    break;
                case "guest":
                    p["CanEditProfile"] = false;
                    break;
            }
            return p;
        }
    }
}