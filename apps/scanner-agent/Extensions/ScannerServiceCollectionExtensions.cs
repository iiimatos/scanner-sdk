using ScannerAgent.Configuration;
using ScannerAgent.Providers;
using ScannerAgent.Services;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;

namespace ScannerAgent.Extensions;

public static partial class ScannerServiceCollectionExtensions
{
    public static IServiceCollection AddScannerProvider(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<ScannerOptions>(
            configuration.GetSection("Scanner")
        );

        services.AddSingleton<MockScannerProvider>();
        services.AddSingleton<MacScannerProvider>();
        services.AddSingleton<LinuxScannerProvider>();
        AddPlatformScannerProvider(services);
        services.AddSingleton<IScannerProvider>(
            ResolveScannerProvider
        );
        services.AddSingleton<ScanFileStore>();
        services.AddScoped<ScanService>();

        return services;
    }

    private static IScannerProvider ResolveScannerProvider(
        IServiceProvider services
    )
    {
        var options = services
            .GetRequiredService<IOptions<ScannerOptions>>()
            .Value;

        if (options.UseMock)
        {
            return services.GetRequiredService<MockScannerProvider>();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && TryGetWindowsScannerProvider(services, out var windowsProvider))
        {
            return windowsProvider;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return services.GetRequiredService<MacScannerProvider>();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return services.GetRequiredService<LinuxScannerProvider>();
        }

        throw new PlatformNotSupportedException(
            "No scanner provider is available for this operating system."
        );
    }

    private static partial void AddPlatformScannerProvider(
        IServiceCollection services
    );

    private static partial bool TryGetWindowsScannerProvider(
        IServiceProvider services,
        out IScannerProvider scannerProvider
    );
}
