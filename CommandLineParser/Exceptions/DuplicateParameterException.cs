namespace CommandLineParser.Exceptions;

public sealed class DuplicateParameterException : UserErrorException
{
    public DuplicateParameterException(string parameterName)
        : base($"Parameter with the name '{parameterName}' is defined multiple times.")
    {
    }
}
