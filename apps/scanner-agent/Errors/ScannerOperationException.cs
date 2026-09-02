namespace ScannerAgent.Errors;

public sealed class ScannerOperationException : ScannerException
{
    public ScannerOperationException(
        string code,
        string message
    ) : base(code, message)
    {
    }
}
