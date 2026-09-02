using ScannerAgent.Models;
using ScannerAgent.Scanning;

namespace ScannerAgent.Providers;

public interface IScannerProvider
{
    bool IsAvailable { get; }

    Task<IReadOnlyList<ScannerDevice>> GetDevicesAsync(
        CancellationToken cancellationToken = default
    );

    Task<ScannerCapabilities?> GetCapabilitiesAsync(
        string deviceId,
        CancellationToken cancellationToken = default
    );

    Task<ScanResult> ScanAsync(
        ScanOptions options,
        CancellationToken cancellationToken = default
    );
}
