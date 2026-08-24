namespace ScannerAgent.Devices;

public sealed record ScannerCapabilities(
    bool SupportsDuplex,
    bool SupportsAdf,
    IReadOnlyList<string> ColorModes,
    IReadOnlyList<string> Formats,
    int MinDpi,
    int MaxDpi);
