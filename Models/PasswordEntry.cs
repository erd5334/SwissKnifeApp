namespace SwissKnifeApp.Models
{
    public class PasswordEntry
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Strength { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        // Navigation property
        public string CategoryName { get; set; } = string.Empty;
    }
}
