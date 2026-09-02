using ScannerAgent.Providers;
using ScannerAgent.Scanning;
using Xunit;

namespace ScannerAgent.Tests;

public sealed class MockScannerProviderTests
{
    [Fact]
    public async Task GetDevicesAsyncReturnsVirtualScanner()
    {
        var provider = new MockScannerProvider();

        var devices = await provider.GetDevicesAsync();

        Assert.Single(devices);
        Assert.Equal("mock-scanner-1", devices[0].Id);
        Assert.Equal("Development Scanner", devices[0].Name);
        Assert.Equal("mock", devices[0].Provider);
        Assert.Equal([200, 300], devices[0].Capabilities.Resolutions);
        Assert.Equal(["flatbed"], devices[0].Capabilities.Sources);
        Assert.False(devices[0].Capabilities.Duplex);
    }

    [Fact]
    public async Task ScanAsyncReturnsCompletedMockResult()
    {
        var provider = new MockScannerProvider();

        var result = await provider.ScanAsync(new ScanOptions(
            DeviceId: MockScannerProvider.DeviceId,
            Dpi: 300,
            ColorMode: "color",
            Source: "flatbed",
            Duplex: false,
            Format: "pdf"
        ));

        Assert.Equal("completed", result.Status);
        Assert.Equal("application/pdf", result.MimeType);
    }
}
