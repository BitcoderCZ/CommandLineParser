namespace CommandLineParser.Exceptions;

public sealed class InvalidEscapeSequenceException : UserErrorException
{
	public InvalidEscapeSequenceException(Type? commandType)
		: base("Another non-whitespace character must follow a '\\', to enter a '\\' type 2 after eachother: \\\\.", commandType)
	{
	}
}
