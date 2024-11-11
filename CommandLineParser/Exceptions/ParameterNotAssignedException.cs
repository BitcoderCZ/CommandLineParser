namespace CommandLineParser.Exceptions;

public sealed class ParameterNotAssignedException : UserErrorException
{
    public ParameterNotAssignedException(string parameterName)
        : base($"Required parameter '{parameterName}' wasn't assigned.")
    {
    }
}
