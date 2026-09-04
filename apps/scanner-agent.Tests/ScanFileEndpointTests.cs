using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ScannerAgent.Providers;
using ScannerAgent.Scanning;
using Xunit;

namespace ScannerAgent.Tests;

public sealed class ScanFileEndpointTests
{
    [Fact]
    public async Task ScanEndpointReturnsDownloadUrlWithoutBase64WhenRequested()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var scanResponse = await client.PostAsJsonAsync(
            "/scan",
            new ScanOptions(
                DeviceId: MockScannerProvider.DeviceId,
                Dpi: 300,
                ColorMode: "color",
                Source: "flatbed",
                Duplex: false,
                Format: "pdf",
                OutputMode: "url"
            )
        );
        var scanResult =
            await scanResponse.Content.ReadFromJsonAsync<ScanResult>();

        Assert.Equal(HttpStatusCode.OK, scanResponse.StatusCode);
        Assert.NotNull(scanResult);
        Assert.Null(scanResult.DataBase64);
        Assert.NotNull(scanResult.DownloadUrl);

        var fileResponse = await client.GetAsync(scanResult.DownloadUrl);
        var fileContent = await fileResponse.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, fileResponse.StatusCode);
        Assert.Equal(
            scanResult.MimeType,
            fileResponse.Content.Headers.ContentType?.MediaType
        );
        Assert.NotEmpty(fileContent);
    }

    [Fact]
    public async Task ScanFileEndpointReturnsNotFoundForMissingScan()
    {
        await using var application = new WebApplicationFactory<Program>();
        using var client = application.CreateClient();

        var response = await client.GetAsync("/scans/missing-scan/file");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
