namespace CommandLineParser.Exceptions;

public sealed class InvalidParameterValueException : UserErrorException
{
	public InvalidParameterValueException(string parameterName, string message, Type? commandType)
		: base($"Parameter '{parameterName}' isn't formated correctly: " + message, commandType)
	{
	}

	public InvalidParameterValueException(string parameterName, Exception innerException, Type? commandType)
		: base($"Parameter '{parameterName}' isn't formated correctly.", innerException, commandType)
	{
	}
}
