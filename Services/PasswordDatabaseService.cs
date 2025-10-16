using Microsoft.Data.Sqlite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SwissKnifeApp.Models;

namespace SwissKnifeApp.Services
{
    public class PasswordDatabaseService
    {
        private readonly string _dbPath;
        private readonly string _masterKey;

        public PasswordDatabaseService(string masterKey = "DefaultMasterKey2025!")
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appData, "SwissKnifeApp");
            Directory.CreateDirectory(appFolder);
            _dbPath = Path.Combine(appFolder, "passwords.db");
            _masterKey = masterKey;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Categories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Color TEXT DEFAULT '#2196F3'
                );

                CREATE TABLE IF NOT EXISTS Passwords (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Username TEXT,
                    EncryptedPassword TEXT,
                    Url TEXT,
                    Notes TEXT,
                    CategoryId INTEGER,
                    ExpiryDate TEXT,
                    Strength TEXT,
                    CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    ModifiedDate TEXT DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE SET NULL
                );

                INSERT OR IGNORE INTO Categories (Id, Name, Color) VALUES 
                    (1, 'Genel', '#2196F3'),
                    (2, 'E-posta', '#4CAF50'),
                    (3, 'Sosyal Medya', '#FF9800'),
                    (4, 'Bankacılık', '#F44336'),
                    (5, 'İş', '#9C27B0'),
                    (6, 'Kişisel', '#00BCD4');
            ";
            command.ExecuteNonQuery();
        }

        // ============ AES Şifreleme/Çözme ============
        public string EncryptPassword(string plainText)
        {
            using var aes = Aes.Create();
            var key = DeriveKey(_masterKey);
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cs))
            {
                writer.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public string DecryptPassword(string cipherText)
        {
            try
            {
                var buffer = Convert.FromBase64String(cipherText);
                using var aes = Aes.Create();
                var key = DeriveKey(_masterKey);
                aes.Key = key;

                var iv = new byte[aes.IV.Length];
                Array.Copy(buffer, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var reader = new StreamReader(cs);
                return reader.ReadToEnd();
            }
            catch
            {
                return string.Empty;
            }
        }

        private byte[] DeriveKey(string password)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, 
                Encoding.UTF8.GetBytes("SwissKnifeSalt2025"), 10000, HashAlgorithmName.SHA256);
            return deriveBytes.GetBytes(32);
        }

        // ============ Kategori İşlemleri ============
        public List<PasswordCategory> GetAllCategories()
        {
            var categories = new List<PasswordCategory>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, Color FROM Categories ORDER BY Name";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new PasswordCategory
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Color = reader.IsDBNull(2) ? "#2196F3" : reader.GetString(2)
                });
            }
            return categories;
        }

        public void AddCategory(string name, string color = "#2196F3")
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Categories (Name, Color) VALUES (@name, @color)";
            command.Parameters.AddWithValue("@name", name);
            command.Parameters.AddWithValue("@color", color);
            command.ExecuteNonQuery();
        }

        public void DeleteCategory(int categoryId)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Categories WHERE Id = @id";
            command.Parameters.AddWithValue("@id", categoryId);
            command.ExecuteNonQuery();
        }

        // ============ Parola İşlemleri ============
        public List<PasswordEntry> GetAllPasswords()
        {
            var passwords = new List<PasswordEntry>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT p.Id, p.Title, p.Username, p.EncryptedPassword, p.Url, p.Notes, 
                       p.CategoryId, p.ExpiryDate, p.Strength, p.CreatedDate, p.ModifiedDate,
                       COALESCE(c.Name, 'Genel') as CategoryName
                FROM Passwords p
                LEFT JOIN Categories c ON p.CategoryId = c.Id
                ORDER BY p.ModifiedDate DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                passwords.Add(new PasswordEntry
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Username = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    EncryptedPassword = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Url = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Notes = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    CategoryId = reader.IsDBNull(6) ? 1 : reader.GetInt32(6),
                    ExpiryDate = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
                    Strength = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    CreatedDate = DateTime.Parse(reader.GetString(9)),
                    ModifiedDate = DateTime.Parse(reader.GetString(10)),
                    CategoryName = reader.GetString(11)
                });
            }
            return passwords;
        }

        public void AddPassword(PasswordEntry entry, string plainPassword)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Passwords (Title, Username, EncryptedPassword, Url, Notes, CategoryId, ExpiryDate, Strength, CreatedDate, ModifiedDate)
                VALUES (@title, @username, @password, @url, @notes, @categoryId, @expiryDate, @strength, @createdDate, @modifiedDate)";
            
            command.Parameters.AddWithValue("@title", entry.Title);
            command.Parameters.AddWithValue("@username", entry.Username ?? "");
            command.Parameters.AddWithValue("@password", EncryptPassword(plainPassword));
            command.Parameters.AddWithValue("@url", entry.Url ?? "");
            command.Parameters.AddWithValue("@notes", entry.Notes ?? "");
            command.Parameters.AddWithValue("@categoryId", entry.CategoryId);
            command.Parameters.AddWithValue("@expiryDate", entry.ExpiryDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@strength", entry.Strength ?? "");
            command.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@modifiedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        public void AddPasswordEncrypted(PasswordEntry entry)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Passwords (Title, Username, EncryptedPassword, Url, Notes, CategoryId, ExpiryDate, Strength, CreatedDate, ModifiedDate)
                VALUES (@title, @username, @password, @url, @notes, @categoryId, @expiryDate, @strength, @createdDate, @modifiedDate)";
            
            command.Parameters.AddWithValue("@title", entry.Title);
            command.Parameters.AddWithValue("@username", entry.Username ?? "");
            command.Parameters.AddWithValue("@password", entry.EncryptedPassword); // Zaten şifrelenmiş
            command.Parameters.AddWithValue("@url", entry.Url ?? "");
            command.Parameters.AddWithValue("@notes", entry.Notes ?? "");
            command.Parameters.AddWithValue("@categoryId", entry.CategoryId);
            command.Parameters.AddWithValue("@expiryDate", entry.ExpiryDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@strength", entry.Strength ?? "");
            command.Parameters.AddWithValue("@createdDate", entry.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@modifiedDate", entry.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        public void UpdatePassword(PasswordEntry entry, string? plainPassword = null)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            
            if (plainPassword != null)
            {
                command.CommandText = @"
                    UPDATE Passwords SET Title = @title, Username = @username, EncryptedPassword = @password, 
                           Url = @url, Notes = @notes, CategoryId = @categoryId, ExpiryDate = @expiryDate, 
                           Strength = @strength, ModifiedDate = @modifiedDate
                    WHERE Id = @id";
                command.Parameters.AddWithValue("@password", EncryptPassword(plainPassword));
            }
            else
            {
                command.CommandText = @"
                    UPDATE Passwords SET Title = @title, Username = @username, 
                           Url = @url, Notes = @notes, CategoryId = @categoryId, ExpiryDate = @expiryDate, 
                           Strength = @strength, ModifiedDate = @modifiedDate
                    WHERE Id = @id";
            }

            command.Parameters.AddWithValue("@id", entry.Id);
            command.Parameters.AddWithValue("@title", entry.Title);
            command.Parameters.AddWithValue("@username", entry.Username ?? "");
            command.Parameters.AddWithValue("@url", entry.Url ?? "");
            command.Parameters.AddWithValue("@notes", entry.Notes ?? "");
            command.Parameters.AddWithValue("@categoryId", entry.CategoryId);
            command.Parameters.AddWithValue("@expiryDate", entry.ExpiryDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@strength", entry.Strength ?? "");
            command.Parameters.AddWithValue("@modifiedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        public void DeletePassword(int id)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Passwords WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        public void DeleteAllPasswords()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Passwords";
            command.ExecuteNonQuery();
        }

        public List<PasswordEntry> SearchPasswords(string searchText, int? categoryId = null)
        {
            var passwords = new List<PasswordEntry>();
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            var whereClauses = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                whereClauses.Add("(p.Title LIKE @search OR p.Username LIKE @search OR p.Url LIKE @search OR p.Notes LIKE @search)");
                command.Parameters.AddWithValue("@search", $"%{searchText}%");
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                whereClauses.Add("p.CategoryId = @categoryId");
                command.Parameters.AddWithValue("@categoryId", categoryId.Value);
            }

            var whereClause = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            command.CommandText = $@"
                SELECT p.Id, p.Title, p.Username, p.EncryptedPassword, p.Url, p.Notes, 
                       p.CategoryId, p.ExpiryDate, p.Strength, p.CreatedDate, p.ModifiedDate,
                       COALESCE(c.Name, 'Genel') as CategoryName
                FROM Passwords p
                LEFT JOIN Categories c ON p.CategoryId = c.Id
                {whereClause}
                ORDER BY p.ModifiedDate DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                passwords.Add(new PasswordEntry
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Username = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    EncryptedPassword = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Url = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Notes = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    CategoryId = reader.IsDBNull(6) ? 1 : reader.GetInt32(6),
                    ExpiryDate = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
                    Strength = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    CreatedDate = DateTime.Parse(reader.GetString(9)),
                    ModifiedDate = DateTime.Parse(reader.GetString(10)),
                    CategoryName = reader.GetString(11)
                });
            }
            return passwords;
        }
    }
}
