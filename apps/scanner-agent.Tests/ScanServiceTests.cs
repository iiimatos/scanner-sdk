using ScannerAgent.Errors;
using ScannerAgent.Providers;
using ScannerAgent.Scanning;
using ScannerAgent.Services;
using Xunit;

namespace ScannerAgent.Tests;

public sealed class ScanServiceTests
{
    private readonly ScanService _scanService = new(new MockScannerProvider());

    [Fact]
    public async Task ScanAsyncReturnsResultForSupportedOptions()
    {
        var result = await _scanService.ScanAsync(new ScanOptions(
            DeviceId: MockScannerProvider.DeviceId,
            Dpi: 300,
            ColorMode: "color",
            Source: "flatbed",
            Duplex: false,
            Format: "pdf"
        ));

        Assert.Equal("completed", result.Status);
        Assert.Equal(MockScannerProvider.DeviceId, result.DeviceId);
    }

    [Theory]
    [InlineData(75, "color", "flatbed", false, "pdf", "resolution")]
    [InlineData(300, "unsupported-color", "flatbed", false, "pdf", "colorMode")]
    [InlineData(300, "color", "adf", false, "pdf", "source")]
    [InlineData(300, "color", "flatbed", true, "pdf", "duplex")]
    [InlineData(300, "color", "flatbed", false, "tiff", "format")]
    public async Task ScanAsyncRejectsUnsupportedCapabilities(
        int dpi,
        string colorMode,
        string source,
        bool duplex,
        string format,
        string capability
    )
    {
        var exception = await Assert.ThrowsAsync<UnsupportedCapabilityException>(
            () => _scanService.ScanAsync(new ScanOptions(
                DeviceId: MockScannerProvider.DeviceId,
                Dpi: dpi,
                ColorMode: colorMode,
                Source: source,
                Duplex: duplex,
                Format: format
            ))
        );

        Assert.Equal("UNSUPPORTED_CAPABILITY", exception.Code);
        Assert.Equal(capability, exception.Capability);
    }

    [Fact]
    public async Task ScanAsyncRejectsUnknownDevice()
    {
        var exception = await Assert.ThrowsAsync<ScannerDeviceNotFoundException>(
            () => _scanService.ScanAsync(new ScanOptions(
                DeviceId: "missing-scanner",
                Dpi: 300,
                ColorMode: "color",
                Source: "flatbed",
                Duplex: false,
                Format: "pdf"
            ))
        );

        Assert.Equal("SCANNER_DEVICE_NOT_FOUND", exception.Code);
        Assert.Equal("missing-scanner", exception.DeviceId);
    }
}
