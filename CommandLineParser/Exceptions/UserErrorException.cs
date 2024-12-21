using CommandLineParser.Commands;

namespace CommandLineParser.Exceptions;

/// <summary>
/// An <see cref="Exception"/> cause by the user, usually by entering data incorrectly.
/// </summary>
public abstract class UserErrorException : Exception
{
	protected UserErrorException(string? message, Type? commandType)
		: base(message)
	{
		if (commandType is not null && !ConsoleCommand.IsCommand(commandType))
		{
			throw new ArgumentException($"{nameof(commandType)} must be {nameof(ConsoleCommand)}.", nameof(commandType));
		}

		CommandType = commandType;
	}

	protected UserErrorException(string? message, Exception innerException, Type? commandType)
		: base(message, innerException)
	{
		if (commandType is not null && !ConsoleCommand.IsCommand(commandType))
		{
			throw new ArgumentException($"{nameof(commandType)} must be {nameof(ConsoleCommand)}.", nameof(commandType));
		}

		CommandType = commandType;
	}

	/// <summary>
	/// Type of the command that was being processed when this exception was thrown.
	/// </summary>
	public Type? CommandType { get; }
}
