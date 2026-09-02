using ScannerAgent.Providers;
using ScannerAgent.Scanning;
using Xunit;

namespace ScannerAgent.Tests;

public sealed class CompositeScannerProviderTests
{
    [Fact]
    public async Task GetDevicesAsyncIncludesDevicesFromRegisteredProviders()
    {
        var provider = new CompositeScannerProvider(
            [
                new MockScannerProvider(),
                new TwainScannerProvider()
            ]
        );

        var devices = await provider.GetDevicesAsync();

        Assert.Single(devices);
        Assert.Equal(MockScannerProvider.DeviceId, devices[0].Id);
    }

    [Fact]
    public async Task ScanAsyncRoutesToProviderThatOwnsDevice()
    {
        var provider = new CompositeScannerProvider(
            [
                new TwainScannerProvider(),
                new MockScannerProvider()
            ]
        );

        var result = await provider.ScanAsync(new ScanOptions(
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
}
