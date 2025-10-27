using SwissKnifeApp.Models;

namespace SwissKnifeApp.Services;

public interface IConfigService
{
    AppConfig Load();
    void Save(AppConfig config);
}
