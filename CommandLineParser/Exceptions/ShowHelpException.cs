namespace CommandLineParser.Exceptions;

internal sealed class ShowHelpException : Exception
{
	public ShowHelpException(Type commandType)
	{
		CommandType = commandType;
	}

	public Type CommandType { get; }
}
