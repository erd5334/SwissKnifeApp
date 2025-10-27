using System.Windows.Forms;

namespace SwissKnifeApp.Services;

public class FolderDialogService : IFolderDialogService
{
    public string? PickFolder(string? initialPath = null)
    {
        using var dlg = new FolderBrowserDialog();
        if (!string.IsNullOrWhiteSpace(initialPath))
            dlg.SelectedPath = initialPath!;
        dlg.ShowNewFolderButton = true;
        return dlg.ShowDialog() == DialogResult.OK ? dlg.SelectedPath : null;
    }
}
