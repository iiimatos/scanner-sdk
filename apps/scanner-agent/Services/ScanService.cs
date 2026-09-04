using ScannerAgent.Errors;
using ScannerAgent.Providers;
using ScannerAgent.Scanning;

namespace ScannerAgent.Services;

public sealed class ScanService
{
    private readonly IScannerProvider _scannerProvider;
    private readonly ScanFileStore _scanFileStore;

    public ScanService(
        IScannerProvider scannerProvider,
        ScanFileStore scanFileStore
    )
    {
        _scannerProvider = scannerProvider;
        _scanFileStore = scanFileStore;
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

        var outputMode = NormalizeOutputMode(options.OutputMode);
        var scanResult = await _scannerProvider.ScanAsync(
            options,
            cancellationToken
        );

        if (outputMode == "base64")
        {
            return scanResult with
            {
                DownloadUrl = null
            };
        }

        if (scanResult.DataBase64 is null)
        {
            throw new ScannerOperationException(
                "SCAN_OUTPUT_NOT_AVAILABLE",
                "The scanner provider did not return scan content."
            );
        }

        byte[] content;

        try
        {
            content = Convert.FromBase64String(scanResult.DataBase64);
        }
        catch (FormatException)
        {
            throw new ScannerOperationException(
                "SCAN_OUTPUT_INVALID",
                "The scanner provider returned invalid scan content."
            );
        }

        _scanFileStore.Save(scanResult, content);

        return scanResult with
        {
            DataBase64 = null,
            DownloadUrl = $"/scans/{Uri.EscapeDataString(scanResult.Id)}/file"
        };
    }

    private static string NormalizeOutputMode(string? outputMode)
    {
        if (string.IsNullOrWhiteSpace(outputMode))
        {
            return "base64";
        }

        var normalizedOutputMode = outputMode.Trim().ToLowerInvariant();

        return normalizedOutputMode switch
        {
            "base64" or "url" => normalizedOutputMode,
            _ => throw new UnsupportedCapabilityException(
                "outputMode",
                outputMode,
                new[] { "base64", "url" }
            )
        };
    }
}
