using ScannerAgent.Devices;
using ScannerAgent.Scanning;

namespace ScannerAgent.Providers;

public interface IScannerProvider
{
    Task<IReadOnlyList<ScannerDevice>> GetDevicesAsync(
        CancellationToken cancellationToken = default);

    Task<ScanResult> ScanAsync(
        ScanOptions options,
        CancellationToken cancellationToken = default);
}
