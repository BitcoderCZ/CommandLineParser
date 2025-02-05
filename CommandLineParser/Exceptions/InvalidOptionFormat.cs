﻿namespace CommandLineParser.Exceptions;

public sealed class InvalidOptionFormat : UserErrorException
{
	public InvalidOptionFormat(string optionName, Type? commandType)
		: base($"Option \"{optionName}\" isn't correctly formated.", commandType)
	{
	}
}
