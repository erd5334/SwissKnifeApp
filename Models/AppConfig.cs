namespace SwissKnifeApp.Models;

public class AppConfig
{
    public bool RememberLast { get; set; } = true;
    public string? LastSource { get; set; }
    public string? LastTarget { get; set; }
}
