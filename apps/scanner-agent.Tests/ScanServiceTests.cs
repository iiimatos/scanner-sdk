using ScannerAgent.Errors;
using ScannerAgent.Providers;
using ScannerAgent.Scanning;
using ScannerAgent.Services;
using Xunit;

namespace ScannerAgent.Tests;

public sealed class ScanServiceTests
{
    private readonly ScanFileStore _scanFileStore = new();
    private readonly ScanService _scanService;

    public ScanServiceTests()
    {
        _scanService = new ScanService(
            new MockScannerProvider(),
            _scanFileStore
        );
    }

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

    [Fact]
    public async Task ScanAsyncReturnsOnlyBase64ByDefault()
    {
        var result = await _scanService.ScanAsync(new ScanOptions(
            DeviceId: MockScannerProvider.DeviceId,
            Dpi: 300,
            ColorMode: "color",
            Source: "flatbed",
            Duplex: false,
            Format: "pdf"
        ));

        Assert.NotNull(result.DataBase64);
        Assert.Null(result.DownloadUrl);
        Assert.Null(_scanFileStore.Get(result.Id));
    }

    [Fact]
    public async Task ScanAsyncReturnsOnlyDownloadUrlWhenRequested()
    {
        var result = await _scanService.ScanAsync(new ScanOptions(
            DeviceId: MockScannerProvider.DeviceId,
            Dpi: 300,
            ColorMode: "color",
            Source: "flatbed",
            Duplex: false,
            Format: "pdf",
            OutputMode: "url"
        ));

        Assert.Null(result.DataBase64);
        Assert.Equal($"/scans/{result.Id}/file", result.DownloadUrl);
        Assert.NotNull(_scanFileStore.Get(result.Id));
    }

    [Theory]
    [InlineData(75, "color", "flatbed", false, "pdf", null, "resolution")]
    [InlineData(300, "unsupported-color", "flatbed", false, "pdf", null, "colorMode")]
    [InlineData(300, "color", "adf", false, "pdf", null, "source")]
    [InlineData(300, "color", "flatbed", true, "pdf", null, "duplex")]
    [InlineData(300, "color", "flatbed", false, "tiff", null, "format")]
    [InlineData(300, "color", "flatbed", false, "pdf", "binary", "outputMode")]
    public async Task ScanAsyncRejectsUnsupportedCapabilities(
        int dpi,
        string colorMode,
        string source,
        bool duplex,
        string format,
        string? outputMode,
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
                Format: format,
                OutputMode: outputMode
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
