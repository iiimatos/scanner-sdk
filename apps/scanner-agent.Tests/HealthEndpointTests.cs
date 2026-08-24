using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ScannerAgent.Health;
using Xunit;

namespace ScannerAgent.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task HealthEndpointReturnsReady()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("ready", body.Status);
        Assert.Equal("scanner-agent", body.Service);
        Assert.Equal("0.1.0", body.Version);
    }
}
