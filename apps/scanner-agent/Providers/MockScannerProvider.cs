using ScannerAgent.Models;
using ScannerAgent.Scanning;
using System.Text;

namespace ScannerAgent.Providers;

public sealed class MockScannerProvider : IScannerProvider
{
    public const string DeviceId = "mock-scanner-001";

    // Smallest valid single-pixel files, used so the playground has real
    // bytes to preview without a physical scanner attached.
    private const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";
    private const string OnePixelJpegBase64 =
        "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgICAgMCAgIDAwMDBAYEBAQEBAgGBgUGCQgKCgkICQkKDA8MCgsOCwkJDRENDg8QEBEQCgwSExIQEw8QEBD/2wBDAQMDAwQDBAgEBAgQCwkLEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBD/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAj/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCdABmX/9k=";

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
            Message: "Mock scan completed. No physical scanner was used.",
            DataBase64: CreatePlaceholderFileBase64(options.Format));

        return Task.FromResult(result);
    }

    private static string CreatePlaceholderFileBase64(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "png" => OnePixelPngBase64,
            "jpeg" => OnePixelJpegBase64,
            _ => Convert.ToBase64String(
                Encoding.ASCII.GetBytes(CreatePlaceholderPdf())
            ),
        };
    }

    private static string CreatePlaceholderPdf()
    {
        return "%PDF-1.4\n"
            + "1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n"
            + "2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj\n"
            + "3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>endobj\n"
            + "4 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj\n"
            + "5 0 obj<< /Length 44 >>\n"
            + "stream\n"
            + "BT /F1 14 Tf 20 100 Td (Mock scan) Tj ET\n"
            + "endstream\n"
            + "endobj\n"
            + "trailer<< /Root 1 0 R /Size 6 >>\n"
            + "%%EOF";
    }
}
