namespace SwissKnifeApp.Services;

public interface IFolderDialogService
{
    string? PickFolder(string? initialPath = null);
}
