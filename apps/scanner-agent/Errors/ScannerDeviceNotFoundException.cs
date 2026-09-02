namespace ScannerAgent.Errors;

public sealed class ScannerDeviceNotFoundException : ScannerException
{
    public ScannerDeviceNotFoundException(string deviceId)
        : base(
            "SCANNER_DEVICE_NOT_FOUND",
            $"Scanner device '{deviceId}' was not found."
        )
    {
        DeviceId = deviceId;
    }

    public string DeviceId { get; }
}