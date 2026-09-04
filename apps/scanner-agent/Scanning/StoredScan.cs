namespace ScannerAgent.Scanning;

public sealed record StoredScan(
    string Id,
    byte[] Content,
    string FileName,
    string MimeType,
    DateTimeOffset CreatedAt
);
