namespace ScannerAgent.Models;

public sealed record ScannerCapabilities(
    IReadOnlyList<int> Resolutions,
    IReadOnlyList<string> ColorModes,
    IReadOnlyList<string> Sources,
    IReadOnlyList<string> Formats,
    bool Duplex
);
