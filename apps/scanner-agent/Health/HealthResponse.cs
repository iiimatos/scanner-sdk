namespace ScannerAgent.Health;

public sealed record HealthResponse(
    string Status,
    string Service,
    string Version);
