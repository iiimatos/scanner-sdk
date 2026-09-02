using ScannerAgent.Errors;
using ScannerAgent.Providers;
using ScannerAgent.Scanning;
using Xunit;

namespace ScannerAgent.Tests;

public sealed class TwainScannerProviderTests
{
    [Fact]
    public async Task GetDevicesAsyncDoesNotReturnFakeHardware()
    {
        var provider = new TwainScannerProvider();

        var devices = await provider.GetDevicesAsync();

        Assert.Empty(devices);
    }

    [Fact]
    public async Task ScanAsyncRejectsUnknownTwainDevice()
    {
        var provider = new TwainScannerProvider();

        var exception = await Assert.ThrowsAsync<ScannerDeviceNotFoundException>(
            () => provider.ScanAsync(new ScanOptions(
                DeviceId: "twain-scanner-001",
                Dpi: 300,
                ColorMode: "color",
                Source: "flatbed",
                Duplex: false,
                Format: "pdf"
            ))
        );

        Assert.Equal("SCANNER_DEVICE_NOT_FOUND", exception.Code);
    }
}
