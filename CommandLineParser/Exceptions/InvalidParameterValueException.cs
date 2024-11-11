namespace CommandLineParser.Exceptions;

public sealed class InvalidParameterValueException : UserErrorException
{
    public InvalidParameterValueException(string parameterName, string message)
        : base($"Parameter '{parameterName}' isn't formated correctly: " + message)
    {
    }

    public InvalidParameterValueException(string parameterName, Exception innerException)
        : base($"Parameter '{parameterName}' isn't formated correctly.", innerException)
    {
    }
}
