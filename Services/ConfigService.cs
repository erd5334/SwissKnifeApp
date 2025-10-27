using System.Text.Json;
using System.IO;
using SwissKnifeApp.Models;

namespace SwissKnifeApp.Services;

public class ConfigService : IConfigService
{
    private static string AppDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ResimKopyalayici");
    private static string ConfigPath => Path.Combine(AppDir, "config.json");

    public AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                if (cfg != null) return cfg;
            }
        }
        catch { }
        return new AppConfig();
    }

    public void Save(AppConfig config)
    {
        try
        {
            Directory.CreateDirectory(AppDir);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }
}
