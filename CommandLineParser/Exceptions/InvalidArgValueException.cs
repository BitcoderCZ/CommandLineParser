namespace CommandLineParser.Exceptions;

public sealed class InvalidArgValueException : UserErrorException
{
    public InvalidArgValueException(string optionName, string message)
        : base($"Option '{optionName}' isn't formated correctly: " + message)
    {
    }

    public InvalidArgValueException(string optionName, Exception innerException)
        : base($"Option '{optionName}' isn't formated correctly.", innerException)
    {
    }
}
