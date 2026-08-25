namespace ScannerAgent.Devices;

public sealed record ScannerCapabilities(
    IReadOnlyList<int> Resolutions,
    IReadOnlyList<string> ColorModes,
    IReadOnlyList<string> Sources,
    bool Duplex
);
