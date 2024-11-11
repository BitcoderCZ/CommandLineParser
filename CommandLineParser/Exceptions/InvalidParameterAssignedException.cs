namespace CommandLineParser.Exceptions;

public sealed class InvalidParameterAssignedException : UserErrorException
{
    public InvalidParameterAssignedException(string parameterName)
        : base($"Parameter '{parameterName}' cannot be assigned because not all parameters it depends on have the specified value.")
    {
    }
}
