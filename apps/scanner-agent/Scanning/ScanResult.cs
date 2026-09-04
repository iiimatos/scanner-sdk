namespace ScannerAgent.Scanning;

public sealed record ScanResult(
    string Id,
    string DeviceId,
    string Status,
    string Format,
    string MimeType,
    string? FileName,
    string? Message,
    string? DataBase64 = null,
    string? DownloadUrl = null);
