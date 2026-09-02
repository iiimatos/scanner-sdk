using ScannerAgent.Errors;
using ScannerAgent.Models;
using ScannerAgent.Scanning;

namespace ScannerAgent.Providers;

public sealed class TwainScannerProvider : IScannerProvider
{
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
        throw new ScannerDeviceNotFoundException(
            options.DeviceId
        );
    }
}
