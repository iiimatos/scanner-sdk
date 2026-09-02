namespace ScannerAgent.Errors;

public abstract class ScannerException : Exception
{
    protected ScannerException(
        string code,
        string message
    ) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}