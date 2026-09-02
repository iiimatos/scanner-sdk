using ScannerAgent.Models;
using ScannerAgent.Scanning;

namespace ScannerAgent.Providers;

public sealed class MockScannerProvider : IScannerProvider
{
    public const string DeviceId = "mock-scanner-001";

    private static readonly ScannerDevice Device = new(
        DeviceId,
        "Scanner SDK Virtual Scanner",
        "mock",
        ScannerStatus.Ready,
        new ScannerCapabilities(
            Resolutions: [150, 200, 300, 600],
            ColorModes: ["color", "grayscale", "black-white"],
            Sources: ["flatbed", "feeder"],
            Duplex: true,
            Formats: ["pdf", "png", "jpeg"]
        ));

    public Task<ScannerCapabilities?> GetCapabilitiesAsync(
        string deviceId,
        CancellationToken cancellationToken = default
    )
    {
        if (deviceId != DeviceId)
        {
            return Task.FromResult<ScannerCapabilities?>(null);
        }

        return Task.FromResult<ScannerCapabilities?>(
            Device.Capabilities
        );
    }

    public Task<IReadOnlyList<ScannerDevice>> GetDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ScannerDevice> devices = [Device];
        return Task.FromResult(devices);
    }

    public Task<ScanResult> ScanAsync(
        ScanOptions options,
        CancellationToken cancellationToken = default)
    {
        var mimeType = options.Format.ToLowerInvariant() switch
        {
            "png" => "image/png",
            "jpeg" => "image/jpeg",
            _ => "application/pdf"
        };

        var result = new ScanResult(
            Id: $"scan_{options.DeviceId}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            DeviceId: options.DeviceId,
            Status: "completed",
            Format: options.Format,
            MimeType: mimeType,
            FileName: $"mock-scan.{options.Format.ToLowerInvariant()}",
            Message: "Mock scan completed. No physical scanner was used.");

        return Task.FromResult(result);
    }
}
