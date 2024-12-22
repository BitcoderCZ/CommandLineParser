using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions;

/// <summary>
/// Thrown when <see cref="CommandParser"/> cannot create an instance of a type of a command parameter.
/// </summary>
public sealed class ParameterCreateException : Exception
{
	public ParameterCreateException(string? message)
		: base(message)
	{
	}

	public ParameterCreateException(string paramName, string message)
		: base($"Failed to assign parameter '{paramName}': {message}")
	{
		ParameterName = paramName;
	}

	public string? ParameterName { get; }
}
