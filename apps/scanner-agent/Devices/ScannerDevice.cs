namespace ScannerAgent.Devices;

public sealed record ScannerDevice(
    string Id,
    string Name,
    string Provider,
    ScannerStatus Status,
    ScannerCapabilities Capabilities);
