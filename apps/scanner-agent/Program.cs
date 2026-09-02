using ScannerAgent.Errors;
using ScannerAgent.Health;
using ScannerAgent.Providers;
using ScannerAgent.Scanning;
using ScannerAgent.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://127.0.0.1:17890");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
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
builder.Services.AddSingleton<IScannerProvider, MockScannerProvider>();
builder.Services.AddScoped<ScanService>();
var app = builder.Build();

app.UseCors("PlaygroundDevelopment");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => new HealthResponse("ready", "scanner-agent", "0.1.0"))
    .WithName("GetHealth");

app.MapGet("/devices", async (IScannerProvider scannerProvider, CancellationToken cancellationToken) =>
    await scannerProvider.GetDevicesAsync(cancellationToken))
    .WithName("GetDevices");

app.MapGet(
    "/devices/{deviceId}/capabilities",
    async (
        string deviceId,
        IScannerProvider scannerProvider,
        CancellationToken cancellationToken
    ) =>
    {
        var capabilities =
            await scannerProvider.GetCapabilitiesAsync(
                deviceId,
                cancellationToken
            );

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
);

app.MapPost(
    "/scan",
    async (
        ScanOptions options,
        ScanService scanService,
        CancellationToken cancellationToken
    ) =>
    {
        try
        {
            var result = await scanService.ScanAsync(
                options,
                cancellationToken
            );

            return Results.Ok(result);
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
    }
);

app.Run();

static object ToErrorResponse(ScannerException exception) => new
{
    code = exception.Code,
    message = exception.Message
};

public partial class Program;
