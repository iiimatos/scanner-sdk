using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ScannerAgent.Models;
using ScannerAgent.Providers;
using Xunit;

namespace ScannerAgent.Tests;

public sealed class CapabilitiesEndpointTests
{
    [Fact]
    public async Task CapabilitiesEndpointReturnsCapabilitiesByQueryDeviceId()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync(
            $"/capabilities?deviceId={MockScannerProvider.DeviceId}"
        );
        var body =
            await response.Content.ReadFromJsonAsync<ScannerCapabilities>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal([200, 300], body.Resolutions);
        Assert.Equal(["flatbed"], body.Sources);
        Assert.False(body.Duplex);
    }
}
