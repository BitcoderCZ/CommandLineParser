namespace CommandLineParser.Exceptions;

public sealed class ParameterNotAssignedException : UserErrorException
{
    public ParameterNotAssignedException(string parameterName, Type? commandType)
        : base($"Required parameter '{parameterName}' wasn't assigned.", commandType)
    {
    }
}
