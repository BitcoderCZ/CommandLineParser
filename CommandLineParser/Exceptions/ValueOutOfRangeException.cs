namespace CommandLineParser.Exceptions;

public sealed class ValueOutOfRangeException : UserErrorException
{
    public ValueOutOfRangeException(string parameterName, string message, Type? commandType)
        : base($"Value for '{parameterName}' is out of range: {message}.", commandType)
    {
    }
}
