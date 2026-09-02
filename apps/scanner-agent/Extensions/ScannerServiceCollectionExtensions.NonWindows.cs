using ScannerAgent.Providers;

namespace ScannerAgent.Extensions;

public static partial class ScannerServiceCollectionExtensions
{
    private static partial void AddPlatformScannerProvider(
        IServiceCollection services
    )
    {
    }

    private static partial bool TryGetWindowsScannerProvider(
        IServiceProvider services,
        out IScannerProvider scannerProvider
    )
    {
        scannerProvider = null!;
        return false;
    }
}
