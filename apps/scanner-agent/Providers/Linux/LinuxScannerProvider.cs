using ScannerAgent.Errors;
using ScannerAgent.Models;
using ScannerAgent.Scanning;

namespace ScannerAgent.Providers;

public sealed class LinuxScannerProvider : IScannerProvider
{
    public bool IsAvailable => false;

    public Task<IReadOnlyList<ScannerDevice>> GetDevicesAsync(
        CancellationToken cancellationToken = default
    )
    {
        IReadOnlyList<ScannerDevice> devices = [];
        return Task.FromResult(devices);
    }

    public Task<ScannerCapabilities?> GetCapabilitiesAsync(
        string deviceId,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult<ScannerCapabilities?>(null);
    }

    public Task<ScanResult> ScanAsync(
        ScanOptions options,
        CancellationToken cancellationToken = default
    )
    {
        throw new ScannerOperationException(
            "LINUX_SCANNER_PROVIDER_NOT_IMPLEMENTED",
            "Linux scanner support is not implemented yet."
        );
    }
}
