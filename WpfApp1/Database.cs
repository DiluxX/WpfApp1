using System;
using System.Collections.Generic;
using System.Windows;
using Npgsql;

namespace WpfApp1
{
    internal class Database
    {
        private string connectionString = "Host=localhost;Port=5432;Database=gstv_CP;Username=postgres;Password=12345;";

        // Существующий метод аутентификации
        public (bool success, int userId, string username, string role) AuthenticateUser(string username, string password)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT u.user_id, u.username, r.name as role_name
                        FROM users u
                        JOIN roles r ON u.role_id = r.role_id
                        WHERE u.username = @username AND u.password = @password";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password", password);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32(0);
                                // Обновляем время последнего входа
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

                    string query = "UPDATE users SET last_login = CURRENT_TIMESTAMP WHERE user_id = @userId";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Игнорируем ошибку обновления времени входа
            }
        }

        // НОВЫЙ МЕТОД: Получение профиля пользователя
        public UserProfile GetUserProfile(int userId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    u.user_id, 
                    u.username,
                    COALESCE(u.display_name, u.username) as display_name,
                    u.email,
                    COALESCE(u.bio, '') as bio,
                    COALESCE(u.avatar_url, '') as avatar_url,
                    r.name as role_name,
                    u.created_at,
                    COALESCE(u.last_login, u.created_at) as last_login,
                    COALESCE(u.updated_at, u.created_at) as updated_at
                FROM users u
                JOIN roles r ON u.role_id = r.role_id
                WHERE u.user_id = @userId";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
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

                    // Проверяем и добавляем отсутствующие поля
                    string[] columnsToAdd = {
                "display_name VARCHAR(255)",
                "bio TEXT",
                "avatar_url VARCHAR(500)",
                "last_login TIMESTAMP",
                "updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP"
            };

                    foreach (var columnDef in columnsToAdd)
                    {
                        string columnName = columnDef.Split(' ')[0];

                        string checkQuery = $@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1 
                            FROM information_schema.columns 
                            WHERE table_name = 'users' 
                            AND column_name = '{columnName}'
                        ) THEN
                            EXECUTE 'ALTER TABLE users ADD COLUMN {columnDef}';
                        END IF;
                    END $$";

                        using (var command = new NpgsqlCommand(checkQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }

                    Console.WriteLine("Структура базы данных проверена и обновлена");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка инициализации структуры БД: {ex.Message}");
            }
        }
        private bool CheckIfTableHasNewFields()
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // Проверяем наличие поля display_name
                    string query = @"
                        SELECT column_name 
                        FROM information_schema.columns 
                        WHERE table_name = 'users' AND column_name = 'display_name'";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        var result = command.ExecuteScalar();
                        return result != null;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // НОВЫЙ МЕТОД: Обновление профиля пользователя
        public bool UpdateUserProfile(int userId, UpdateProfileRequest request)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // Проверяем текущий пароль если меняем пароль
                    if (!string.IsNullOrEmpty(request.NewPassword))
                    {
                        if (!VerifyPassword(userId, request.CurrentPassword))
                        {
                            MessageBox.Show("Текущий пароль неверен", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }

                    // Начинаем транзакцию
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Собираем поля для обновления
                            var updates = new List<string>();
                            var parameters = new List<NpgsqlParameter>
                    {
                        new NpgsqlParameter("@userId", userId)
                    };

                            // Добавляем параметры динамически
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

                            // Обработка аватара
                            if (!string.IsNullOrEmpty(request.AvatarUrl))
                            {
                                // Проверяем, существует ли файл
                                if (System.IO.File.Exists(request.AvatarUrl))
                                {
                                    // Копируем файл в папку приложения
                                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                                    string appFolder = System.IO.Path.Combine(appDataPath, "NewsPortal", "Avatars");

                                    if (!System.IO.Directory.Exists(appFolder))
                                    {
                                        System.IO.Directory.CreateDirectory(appFolder);
                                    }

                                    string fileName = $"avatar_{userId}_{DateTime.Now:yyyyMMddHHmmss}{System.IO.Path.GetExtension(request.AvatarUrl)}";
                                    string destinationPath = System.IO.Path.Combine(appFolder, fileName);

                                    // Копируем файл
                                    System.IO.File.Copy(request.AvatarUrl, destinationPath, true);

                                    // Сохраняем относительный путь
                                    string relativePath = $"avatars/{fileName}";
                                    updates.Add("avatar_url = @avatarUrl");
                                    parameters.Add(new NpgsqlParameter("@avatarUrl", relativePath));
                                }
                                else if (request.AvatarUrl.StartsWith("http"))
                                {
                                    // URL из интернета
                                    updates.Add("avatar_url = @avatarUrl");
                                    parameters.Add(new NpgsqlParameter("@avatarUrl", request.AvatarUrl));
                                }
                                else if (string.IsNullOrEmpty(request.AvatarUrl))
                                {
                                    // Удаление аватара
                                    updates.Add("avatar_url = ''");
                                }
                            }

                            if (!string.IsNullOrEmpty(request.NewPassword))
                            {
                                updates.Add("password = @newPassword");
                                parameters.Add(new NpgsqlParameter("@newPassword", request.NewPassword));
                            }

                            // Всегда обновляем время
                            updates.Add("updated_at = CURRENT_TIMESTAMP");

                            if (updates.Count > 0)
                            {
                                string query = $"UPDATE users SET {string.Join(", ", updates)} WHERE user_id = @userId";

                                using (var command = new NpgsqlCommand(query, connection))
                                {
                                    command.Transaction = transaction;

                                    foreach (var param in parameters)
                                    {
                                        command.Parameters.Add(param);
                                    }

                                    int rowsAffected = command.ExecuteNonQuery();

                                    if (rowsAffected > 0)
                                    {
                                        transaction.Commit();
                                        return true;
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                        MessageBox.Show("Пользователь не найден", "Ошибка",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                        return false;
                                    }
                                }
                            }
                            else
                            {
                                transaction.Rollback();
                                MessageBox.Show("Нет данных для обновления", "Информация",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                return false;
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception($"Ошибка при обновлении: {ex.Message}", ex);
                        }
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

        // Проверка пароля
        private bool VerifyPassword(int userId, string password)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT COUNT(*) FROM users WHERE user_id = @userId AND password = @password";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@password", password);

                        var result = command.ExecuteScalar();
                        return Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // Проверка доступности имени пользователя
        public bool IsUsernameAvailable(string username, int excludeUserId = 0)
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT COUNT(*) FROM users WHERE username = @username AND user_id != @excludeUserId";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@excludeUserId", excludeUserId);

                        var result = command.ExecuteScalar();
                        return Convert.ToInt32(result) == 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        // Существующий метод получения прав роли
        public Dictionary<string, bool> GetRolePermissions(string role)
        {
            var permissions = new Dictionary<string, bool>
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
                ["CanEditProfile"] = true // Все могут редактировать свой профиль
            };

            switch (role.ToLower())
            {
                case "admin":
                    permissions["CanAddNews"] = true;
                    permissions["CanEditNews"] = true;
                    permissions["CanDeleteNews"] = true;
                    permissions["CanAddComments"] = true;
                    permissions["CanEditComments"] = true;
                    permissions["CanDeleteComments"] = true;
                    permissions["CanModerateComments"] = true;
                    permissions["CanManageUsers"] = true;
                    permissions["CanModerateNews"] = true;
                    break;

                case "manager":
                    permissions["CanAddNews"] = true;
                    permissions["CanEditNews"] = true;
                    permissions["CanDeleteNews"] = false;
                    permissions["CanAddComments"] = true;
                    permissions["CanEditComments"] = true;
                    permissions["CanDeleteComments"] = true;
                    permissions["CanModerateComments"] = true;
                    permissions["CanManageUsers"] = true;
                    permissions["CanModerateNews"] = true;
                    break;

                case "user":
                    permissions["CanAddComments"] = true;
                    permissions["CanEditComments"] = false;
                    permissions["CanDeleteComments"] = false;
                    break;

                case "guest":
                    permissions["CanEditProfile"] = false;
                    break;
            }

            return permissions;
        }

        // Дополнительный метод для обновления структуры БД
        public void UpdateDatabaseStructure()
        {
            try
            {
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    // Добавляем новые поля если их нет
                    string[] newColumns = {
                        "display_name", "bio", "avatar_url", "last_login", "updated_at"
                    };

                    foreach (var column in newColumns)
                    {
                        string checkQuery = $@"
                            DO $$ 
                            BEGIN 
                                IF NOT EXISTS (
                                    SELECT 1 
                                    FROM information_schema.columns 
                                    WHERE table_name = 'users' AND column_name = '{column}'
                                ) THEN
                                    EXECUTE 'ALTER TABLE users ADD COLUMN {column} VARCHAR(500)';
                                END IF;
                            END $$";

                        using (var command = new NpgsqlCommand(checkQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }

                    // Обновляем существующих пользователей
                    string updateQuery = "UPDATE users SET display_name = username WHERE display_name IS NULL";
                    using (var command = new NpgsqlCommand(updateQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    MessageBox.Show("Структура базы данных успешно обновлена", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления структуры БД: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}