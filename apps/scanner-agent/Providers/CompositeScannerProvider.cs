using ScannerAgent.Errors;
using ScannerAgent.Models;
using ScannerAgent.Scanning;

namespace ScannerAgent.Providers;

public sealed class CompositeScannerProvider : IScannerProvider
{
    private readonly IReadOnlyList<IScannerProvider> _providers;

    public CompositeScannerProvider(
        IReadOnlyList<IScannerProvider> providers
    )
    {
        _providers = providers;
    }

    public async Task<IReadOnlyList<ScannerDevice>> GetDevicesAsync(
        CancellationToken cancellationToken = default
    )
    {
        var devices = new List<ScannerDevice>();

        foreach (var provider in _providers)
        {
            devices.AddRange(
                await provider.GetDevicesAsync(cancellationToken)
            );
        }

        return devices;
    }

    public async Task<ScannerCapabilities?> GetCapabilitiesAsync(
        string deviceId,
        CancellationToken cancellationToken = default
    )
    {
        foreach (var provider in _providers)
        {
            var capabilities = await provider.GetCapabilitiesAsync(
                deviceId,
                cancellationToken
            );

            if (capabilities is not null)
            {
                return capabilities;
            }
        }

        return null;
    }

    public async Task<ScanResult> ScanAsync(
        ScanOptions options,
        CancellationToken cancellationToken = default
    )
    {
        foreach (var provider in _providers)
        {
            var capabilities = await provider.GetCapabilitiesAsync(
                options.DeviceId,
                cancellationToken
            );

            if (capabilities is not null)
            {
                return await provider.ScanAsync(
                    options,
                    cancellationToken
                );
            }
        }

        throw new ScannerDeviceNotFoundException(
            options.DeviceId
        );
    }
}
