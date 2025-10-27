using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SwissKnifeApp.Models;

namespace SwissKnifeApp.Services;

public interface ICopyService
{
    Task<(long totalFiles, long totalBytes)> CountAsync(string sourceRoot, IEnumerable<string> extensionsOrAll, CancellationToken ct);

    Task CopyAsync(
        IEnumerable<CopyItem> items,
        int maxDegreeOfParallelism,
        bool overwrite,
        TimeSpan retryWindow,
        Action<long, long, string>? onProgress,
        Action<string>? onLog,
        Action<string, Exception>? onError,
        CancellationToken ct);
}
