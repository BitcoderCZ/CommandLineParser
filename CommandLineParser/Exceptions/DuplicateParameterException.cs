namespace CommandLineParser.Exceptions;

public sealed class DuplicateParameterException : UserErrorException
{
    public DuplicateParameterException(string parameterName, Type? commandType)
        : base($"Parameter with the name '{parameterName}' is defined multiple times.", commandType)
    {
    }
}
