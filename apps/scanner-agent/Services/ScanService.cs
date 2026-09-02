using ScannerAgent.Errors;
using ScannerAgent.Providers;
using ScannerAgent.Scanning;

namespace ScannerAgent.Services;

public sealed class ScanService
{
    private readonly IScannerProvider _scannerProvider;

    public ScanService(IScannerProvider scannerProvider)
    {
        _scannerProvider = scannerProvider;
    }

    public async Task<ScanResult> ScanAsync(
        ScanOptions options,
        CancellationToken cancellationToken = default
    )
    {
        var capabilities =
            await _scannerProvider.GetCapabilitiesAsync(
                options.DeviceId,
                cancellationToken
            );

        if (capabilities is null)
        {
            throw new ScannerDeviceNotFoundException(
                options.DeviceId
            );
        }

        if (!capabilities.Resolutions.Contains(options.Dpi))
        {
            throw new UnsupportedCapabilityException(
                "resolution",
                options.Dpi,
                capabilities.Resolutions
            );
        }

        if (!capabilities.ColorModes.Contains(options.ColorMode))
        {
            throw new UnsupportedCapabilityException(
                "colorMode",
                options.ColorMode,
                capabilities.ColorModes
            );
        }

        if (!capabilities.Sources.Contains(options.Source))
        {
            throw new UnsupportedCapabilityException(
                "source",
                options.Source,
                capabilities.Sources
            );
        }

        if (options.Duplex && !capabilities.Duplex)
        {
            throw new UnsupportedCapabilityException(
                "duplex",
                options.Duplex,
                false
            );
        }

        if (!capabilities.Formats.Contains(options.Format))
        {
            throw new UnsupportedCapabilityException(
                "format",
                options.Format,
                capabilities.Formats
            );
        }

        return await _scannerProvider.ScanAsync(
            options,
            cancellationToken
        );
    }
}
