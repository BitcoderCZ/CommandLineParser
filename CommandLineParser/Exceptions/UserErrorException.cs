namespace CommandLineParser.Exceptions;

public abstract class UserErrorException : Exception
{
    protected UserErrorException(string? message)
        : base(message)
    {
    }

    protected UserErrorException(string? message, Exception innerException)
        : base(message, innerException)
    {
    }
}
