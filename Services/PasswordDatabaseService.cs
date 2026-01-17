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

                CREATE TABLE IF NOT EXISTS VaultSettings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT
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
                    TotpSecret TEXT,
                    IsSecureNote INTEGER DEFAULT 0,
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

            // Sütun kontrolü (Migration benzeri basit bir işlem)
            try {
                command.CommandText = "ALTER TABLE Passwords ADD COLUMN TotpSecret TEXT;";
                command.ExecuteNonQuery();
            } catch { }

            try {
                command.CommandText = "ALTER TABLE Passwords ADD COLUMN IsSecureNote INTEGER DEFAULT 0;";
                command.ExecuteNonQuery();
            } catch { }
        }

        public bool IsMasterPasswordSet()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM VaultSettings WHERE Key = 'MasterPasswordHash'";
            var result = command.ExecuteScalar();
            return result != null && !string.IsNullOrEmpty(result.ToString());
        }

        public void SetMasterPassword(string password)
        {
            var salt = Guid.NewGuid().ToString();
            var hash = ComputeHash(password, salt);

            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            using var transaction = connection.BeginTransaction();
            
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT OR REPLACE INTO VaultSettings (Key, Value) VALUES ('MasterPasswordHash', @hash)";
            command.Parameters.AddWithValue("@hash", hash);
            command.ExecuteNonQuery();

            command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT OR REPLACE INTO VaultSettings (Key, Value) VALUES ('MasterPasswordSalt', @salt)";
            command.Parameters.AddWithValue("@salt", salt);
            command.ExecuteNonQuery();

            transaction.Commit();
        }

        public bool VerifyMasterPassword(string password)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM VaultSettings WHERE Key = 'MasterPasswordHash'";
            var hash = command.ExecuteScalar()?.ToString();

            command.CommandText = "SELECT Value FROM VaultSettings WHERE Key = 'MasterPasswordSalt'";
            var salt = command.ExecuteScalar()?.ToString();

            if (hash == null || salt == null) return false;

            return ComputeHash(password, salt) == hash;
        }

        private string? _sessionMasterKey;
        public bool IsUnlocked => _sessionMasterKey != null;

        public void Unlock(string masterPassword)
        {
            if (VerifyMasterPassword(masterPassword))
            {
                _sessionMasterKey = masterPassword;
            }
            else
            {
                throw new Exception("Geçersiz master parola!");
            }
        }

        public void Lock()
        {
            _sessionMasterKey = null;
        }

        private string ComputeHash(string password, string salt)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, 
                Encoding.UTF8.GetBytes(salt), 10000, HashAlgorithmName.SHA256);
            return Convert.ToBase64String(deriveBytes.GetBytes(32));
        }

        public void AddPasswordEncrypted(PasswordEntry entry)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Passwords (Title, Username, EncryptedPassword, Url, Notes, CategoryId, TotpSecret, IsSecureNote, CreatedDate, ModifiedDate)
                VALUES (@title, @username, @password, @url, @notes, @categoryId, @totp, @isSecureNote, @createdDate, @modifiedDate)";
            
            command.Parameters.AddWithValue("@title", entry.Title);
            command.Parameters.AddWithValue("@username", entry.Username ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@password", entry.EncryptedPassword);
            command.Parameters.AddWithValue("@url", entry.Url ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@notes", entry.Notes ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@categoryId", entry.CategoryId);
            command.Parameters.AddWithValue("@totp", entry.TotpSecret ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@isSecureNote", entry.IsSecureNote ? 1 : 0);
            command.Parameters.AddWithValue("@createdDate", entry.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@modifiedDate", entry.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        // ============ AES Şifreleme/Çözme ============
        public string EncryptPassword(string plainText)
        {
            if (_sessionMasterKey == null) throw new Exception("Kasa kilitli!");
            
            using var aes = Aes.Create();
            var key = DeriveKey(_sessionMasterKey);
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
            if (_sessionMasterKey == null) return "********";
            
            try
            {
                var buffer = Convert.FromBase64String(cipherText);
                using var aes = Aes.Create();
                var key = DeriveKey(_sessionMasterKey);
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
                       p.CategoryId, p.ExpiryDate, p.Strength, p.TotpSecret, p.IsSecureNote,
                       p.CreatedDate, p.ModifiedDate, COALESCE(c.Name, 'Genel') as CategoryName
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
                    TotpSecret = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    IsSecureNote = reader.GetInt32(10) == 1,
                    CreatedDate = DateTime.Parse(reader.GetString(11)),
                    ModifiedDate = DateTime.Parse(reader.GetString(12)),
                    CategoryName = reader.GetString(13)
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
                INSERT INTO Passwords (Title, Username, EncryptedPassword, Url, Notes, CategoryId, ExpiryDate, Strength, TotpSecret, IsSecureNote, CreatedDate, ModifiedDate)
                VALUES (@title, @username, @password, @url, @notes, @categoryId, @expiryDate, @strength, @totp, @isSecureNote, @createdDate, @modifiedDate)";
            
            command.Parameters.AddWithValue("@title", entry.Title);
            command.Parameters.AddWithValue("@username", entry.Username ?? "");
            command.Parameters.AddWithValue("@password", EncryptPassword(plainPassword));
            command.Parameters.AddWithValue("@url", entry.Url ?? "");
            command.Parameters.AddWithValue("@notes", entry.Notes ?? "");
            command.Parameters.AddWithValue("@categoryId", entry.CategoryId);
            command.Parameters.AddWithValue("@expiryDate", entry.ExpiryDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@strength", entry.Strength ?? "");
            command.Parameters.AddWithValue("@totp", entry.TotpSecret ?? "");
            command.Parameters.AddWithValue("@isSecureNote", entry.IsSecureNote ? 1 : 0);
            command.Parameters.AddWithValue("@createdDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@modifiedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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
                           Strength = @strength, TotpSecret = @totp, IsSecureNote = @isSecureNote, ModifiedDate = @modifiedDate
                    WHERE Id = @id";
                command.Parameters.AddWithValue("@password", EncryptPassword(plainPassword));
            }
            else
            {
                command.CommandText = @"
                    UPDATE Passwords SET Title = @title, Username = @username, 
                           Url = @url, Notes = @notes, CategoryId = @categoryId, ExpiryDate = @expiryDate, 
                           Strength = @strength, TotpSecret = @totp, IsSecureNote = @isSecureNote, ModifiedDate = @modifiedDate
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
            command.Parameters.AddWithValue("@totp", entry.TotpSecret ?? "");
            command.Parameters.AddWithValue("@isSecureNote", entry.IsSecureNote ? 1 : 0);
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
                       p.CategoryId, p.ExpiryDate, p.Strength, p.TotpSecret, p.IsSecureNote,
                       p.CreatedDate, p.ModifiedDate, COALESCE(c.Name, 'Genel') as CategoryName
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
                    TotpSecret = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    IsSecureNote = reader.GetInt32(10) == 1,
                    CreatedDate = DateTime.Parse(reader.GetString(11)),
                    ModifiedDate = DateTime.Parse(reader.GetString(12)),
                    CategoryName = reader.GetString(13)
                });
            }
            return passwords;
        }
    }
}
