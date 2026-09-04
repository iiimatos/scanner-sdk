using ScannerAgent.Errors;
using ScannerAgent.Extensions;
using ScannerAgent.Health;
using ScannerAgent.Models;
using ScannerAgent.Providers;
using ScannerAgent.Scanning;
using ScannerAgent.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(
    builder.Configuration["ScannerAgent:Url"] ??
    "http://127.0.0.1:17890"
);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("PlaygroundDevelopment", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();
builder.Services.AddScannerProvider(builder.Configuration);
var app = builder.Build();

app.UseCors("PlaygroundDevelopment");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

static HealthResponse GetHealth() => new("ready", "scanner-agent", "0.1.0");

app.MapGet("/health", GetHealth)
    .WithName("GetHealth");

app.MapGet("/health/live", GetHealth)
    .WithName("GetHealthLive");

app.MapGet("/devices", async (IScannerProvider scannerProvider, CancellationToken cancellationToken) =>
    await scannerProvider.GetDevicesAsync(cancellationToken))
    .WithName("GetDevices");

app.MapGet(
    "/devices/{deviceId}/capabilities",
    async (
        string deviceId,
        IScannerProvider scannerProvider,
        CancellationToken cancellationToken
    ) => await GetCapabilitiesResult(
        deviceId,
        scannerProvider,
        cancellationToken
    )
);

app.MapGet(
    "/capabilities",
    async (
        string deviceId,
        IScannerProvider scannerProvider,
        CancellationToken cancellationToken
    ) => await GetCapabilitiesResult(
        deviceId,
        scannerProvider,
        cancellationToken
    )
);

app.MapPost(
    "/scan",
    async (
        ScanOptions options,
        ScanService scanService,
        HttpRequest httpRequest,
        CancellationToken cancellationToken
    ) =>
    {
        try
        {
            var result = await scanService.ScanAsync(
                options,
                cancellationToken
            );

            return Results.Ok(
                ToPublicScanResult(result, httpRequest)
            );
        }
        catch (ScannerDeviceNotFoundException exception)
        {
            return Results.NotFound(ToErrorResponse(exception));
        }
        catch (UnsupportedCapabilityException exception)
        {
            return Results.BadRequest(new
            {
                code = exception.Code,
                message = exception.Message,
                capability = exception.Capability,
                requested = exception.Requested,
                supported = exception.Supported
            });
        }
        catch (ScannerOperationException exception)
        {
            return Results.Problem(
                title: exception.Code,
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }
    }
);

app.MapGet(
    "/scans/{scanId}/file",
    (
        string scanId,
        ScanFileStore scanFileStore
    ) =>
    {
        var scan = scanFileStore.Get(scanId);

        if (scan is null)
        {
            return Results.NotFound(new
            {
                code = "SCAN_FILE_NOT_FOUND",
                message = "Scan file was not found."
            });
        }

        return Results.File(
            scan.Content,
            scan.MimeType,
            scan.FileName
        );
    }
);

app.Run();

static object ToErrorResponse(ScannerException exception) => new
{
    code = exception.Code,
    message = exception.Message
};

static ScanResult ToPublicScanResult(
    ScanResult scanResult,
    HttpRequest request
)
{
    if (scanResult.DownloadUrl is null || !scanResult.DownloadUrl.StartsWith('/'))
    {
        return scanResult;
    }

    return scanResult with
    {
        DownloadUrl = $"{request.Scheme}://{request.Host}{scanResult.DownloadUrl}"
    };
}

static async Task<IResult> GetCapabilitiesResult(
    string deviceId,
    IScannerProvider scannerProvider,
    CancellationToken cancellationToken
)
{
    ScannerCapabilities? capabilities;

    try
    {
        capabilities =
            await scannerProvider.GetCapabilitiesAsync(
                deviceId,
                cancellationToken
            );
    }
    catch (ScannerOperationException exception)
    {
        return Results.Problem(
            title: exception.Code,
            detail: exception.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable
        );
    }

    if (capabilities is null)
    {
        return Results.NotFound(new
        {
            code = "SCANNER_DEVICE_NOT_FOUND",
            message = "Scanner device was not found."
        });
    }

    return Results.Ok(capabilities);
}

public partial class Program;
