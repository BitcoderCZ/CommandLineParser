namespace CommandLineParser.Exceptions;

public sealed class CommandNotFoundException : UserErrorException
{
	public CommandNotFoundException(string commandName, Type? commandType)
		: base($"Command \"{commandName}\" doesn't exist.", commandType)
	{
	}
}
