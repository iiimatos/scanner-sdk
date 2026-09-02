namespace ScannerAgent.Errors;

public sealed class UnsupportedCapabilityException : ScannerException
{
    public UnsupportedCapabilityException(
        string capability,
        object requested,
        object supported
    )
        : base(
            "UNSUPPORTED_CAPABILITY",
            $"The requested {capability} is not supported."
        )
    {
        Capability = capability;
        Requested = requested;
        Supported = supported;
    }

    public string Capability { get; }

    public object Requested { get; }

    public object Supported { get; }
}