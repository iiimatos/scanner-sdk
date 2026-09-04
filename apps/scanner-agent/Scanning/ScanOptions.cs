namespace ScannerAgent.Scanning;

public sealed record ScanOptions(
    string DeviceId,
    int Dpi,
    string ColorMode,
    string Source,
    bool Duplex,
    string Format,
    string? OutputMode = null
);
