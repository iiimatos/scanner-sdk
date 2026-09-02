using ScannerAgent.Errors;
using ScannerAgent.Models;
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
                new EmptyScannerProvider()
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
                new EmptyScannerProvider(),
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

    private sealed class EmptyScannerProvider : IScannerProvider
    {
        public bool IsAvailable => false;

        public Task<IReadOnlyList<ScannerDevice>> GetDevicesAsync(
            CancellationToken cancellationToken = default
        )
        {
            IReadOnlyList<ScannerDevice> devices = [];
            return Task.FromResult(devices);
        }

        public Task<ScannerCapabilities?> GetCapabilitiesAsync(
            string deviceId,
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult<ScannerCapabilities?>(null);
        }

        public Task<ScanResult> ScanAsync(
            ScanOptions options,
            CancellationToken cancellationToken = default
        )
        {
            throw new ScannerDeviceNotFoundException(options.DeviceId);
        }
    }
}
