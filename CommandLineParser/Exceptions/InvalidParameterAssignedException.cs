namespace CommandLineParser.Exceptions;

public sealed class InvalidParameterAssignedException : UserErrorException
{
	public InvalidParameterAssignedException(string parameterName, Type? commandType)
		: base($"Parameter '{parameterName}' cannot be assigned because not all parameters it depends on have the specified value.", commandType)
	{
	}
}
