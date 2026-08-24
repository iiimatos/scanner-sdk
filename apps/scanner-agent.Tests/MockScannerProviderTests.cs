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
        Assert.Equal("mock-scanner-001", devices[0].Id);
        Assert.Equal("Scanner SDK Virtual Scanner", devices[0].Name);
        Assert.Equal("mock", devices[0].Provider);
    }

    [Fact]
    public async Task ScanAsyncReturnsCompletedMockResult()
    {
        var provider = new MockScannerProvider();

        var result = await provider.ScanAsync(new ScanOptions(
            DeviceId: "mock-scanner-001",
            Dpi: 300,
            ColorMode: "color",
            Format: "pdf"));

        Assert.Equal("completed", result.Status);
        Assert.Equal("application/pdf", result.MimeType);
    }
}
