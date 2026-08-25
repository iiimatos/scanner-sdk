using ScannerAgent.Devices;
using ScannerAgent.Health;
using ScannerAgent.Providers;
using ScannerAgent.Scanning;
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
                "http://127.0.0.1:3000",
                "http://localhost:3001",
                "http://127.0.0.1:3001",
                "http://localhost:3002",
                "http://127.0.0.1:3002",
                "http://localhost:3003",
                "http://127.0.0.1:3003",
                "http://localhost:3004",
                "http://127.0.0.1:3004",
                "http://localhost:3005",
                "http://127.0.0.1:3005")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IScannerProvider, MockScannerProvider>();

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
                code = "DEVICE_NOT_FOUND",
                message = "Scanner device was not found."
            });
        }

        return Results.Ok(capabilities);
    }
);

app.MapPost("/scan", async (ScanOptions options, IScannerProvider scannerProvider, CancellationToken cancellationToken) =>
    await scannerProvider.ScanAsync(options, cancellationToken))
    .WithName("Scan");

app.Run();

public partial class Program;
