using ScannerAgent.Scanning;

namespace ScannerAgent.Services;

public sealed class ScanFileStore
{
    private readonly Dictionary<string, StoredScan> _scans = [];
    private readonly object _lock = new();

    public void Save(
        ScanResult scanResult,
        byte[] content
    )
    {
        if (scanResult.FileName is null)
        {
            return;
        }

        lock (_lock)
        {
            _scans[scanResult.Id] = new StoredScan(
                Id: scanResult.Id,
                Content: content,
                FileName: scanResult.FileName,
                MimeType: scanResult.MimeType,
                CreatedAt: DateTimeOffset.UtcNow
            );
        }
    }

    public StoredScan? Get(string scanId)
    {
        lock (_lock)
        {
            return _scans.GetValueOrDefault(scanId);
        }
    }
}
