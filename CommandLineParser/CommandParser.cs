using CommandLineParser.CommandParameters;
using CommandLineParser.Commands;
using CommandLineParser.Exceptions;
using CommandLineParser.Utils;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace CommandLineParser;

public static class CommandParser
{
	private static readonly ImmutableArray<Type> BuiltInCommands =
	[
		typeof(HelpCommand),
		typeof(VersionCommand),
	];

	private delegate object SpanParseCtor(ReadOnlySpan<char> span);

	/// <summary>
	/// 
	/// </summary>
	/// <param name="args"></param>
	/// <param name="parseOptions"></param>
	/// <param name="defaultCommand"></param>
	/// <param name="commands"></param>
	/// <param name="helpTextWriter"></param>
	/// <returns>Exit code</returns>
	/// <exception cref="ParameterCreateException">Thrown when an instance of a type of a command parameter cannot be created.</exception>
	public static int ParseAndRun(string[] args, ParseOptions parseOptions, Type? defaultCommand, IEnumerable<Type> commands, TextWriter? helpTextWriter = null)
	{
		var commandsSet = commands is HashSet<Type> set ? set : [.. commands];

		if (defaultCommand is not null)
		{
			commandsSet.Add(defaultCommand);
		}

		foreach (var ct in BuiltInCommands)
		{
			commandsSet.Add(ct);
		}

		helpTextWriter ??= Console.Out;

		ConsoleCommand command;
		try
		{
			command = ParseInternal(args, parseOptions, defaultCommand, commandsSet);
		}
		catch (UserErrorException ex)
		{
			HelpText.GenerateVersionInfo(helpTextWriter);

			HelpText.GenerateForError(ex, helpTextWriter);

			if (ex.CommandType is not null)
			{
				HelpText.GenerateForCommand(ex.CommandType, helpTextWriter);
			}
			else if (ex is NoCommandSpecified or CommandNotFoundException)
			{
				HelpText.GenerateForCommands(commandsSet, helpTextWriter);
			}

			return 1;
		}
		catch (ShowHelpException ex)
		{
			HelpText.GenerateVersionInfo(helpTextWriter);
			if (ex.CommandType is null)
			{
				HelpText.GenerateForCommands(commandsSet, helpTextWriter);
			}
			else
			{
				HelpText.GenerateForCommand(ex.CommandType, helpTextWriter);
			}

			return 0;
		}

		if (command is HelpCommand)
		{
			HelpText.GenerateVersionInfo(helpTextWriter);
			HelpText.GenerateForCommands(commandsSet, helpTextWriter);
			return 0;
		}
		else if (command is VersionCommand)
		{
			HelpText.GenerateVersionInfo(helpTextWriter);
			return 0;
		}

		return command.Run();
	}

	/// <exception cref="NoCommandSpecified"></exception>
	/// <exception cref="CommandNotFoundException"></exception>
	/// <exception cref="ShowHelpException"></exception>
	/// <exception cref="OptionNotFoundException"></exception>
	/// <exception cref="ArgumentOutOfBounds"></exception>
	/// <exception cref="DuplicateParameterException"></exception>
	/// <exception cref="ParameterNotAssignedException"></exception>
	/// <exception cref="InvalidOptionDepencyException"></exception>
	/// <exception cref="InvalidParameterAssignedException"></exception>
	/// <exception cref="InvalidParameterValueException"></exception>
	/// <exception cref="ParameterCreateException"></exception>
	private static ConsoleCommand ParseInternal(string[] args, ParseOptions parseOptions, Type? defaultCommand, IEnumerable<Type> commands)
	{
		ConsoleCommand? command = null;
		bool isDefaultCommand = false;

		ReadOnlySpan<string> argsSpan;

		// find which command was called
		if (defaultCommand is not null && (args.Length == 0 || args[0].StartsWith('-')))
		{
			command = ConsoleCommand.CreateInstance(defaultCommand);
			argsSpan = args;
			isDefaultCommand = true;
		}
		else
		{
			if (args.Length == 0 || args[0].StartsWith('-'))
			{
				throw new NoCommandSpecified();
			}

			string commandName = args[0];
			argsSpan = args.AsSpan(1);

			foreach (var ct in commands)
			{
				if (ConsoleCommand.GetName(ct) == commandName)
				{
					command = ConsoleCommand.CreateInstance(ct);
					break;
				}
			}

			if (command is null)
			{
				if (defaultCommand is not null)
				{
					command = ConsoleCommand.CreateInstance(defaultCommand);
					argsSpan = args;
					isDefaultCommand = true;
				}
				else
				{
					throw new CommandNotFoundException(commandName, null);
				}
			}
		}

		Type commandType = command.GetType();

		var (arguments, options) = ConsoleCommand.GetParameters(commandType);
		CommandParameter[] parameters = [.. arguments, .. options];

		Dictionary<char, CommandOption> shortNameOptions = [];
		Dictionary<string, CommandOption> longNameOptions = [];
		List<CommandOption> optionsToVerify = [];

		for (int i = 0; i < options.Length; i++)
		{
			var option = options[i];

			if (option.ShortName is not null)
			{
				shortNameOptions.Add(option.ShortName.Value, option);
			}

			if (option.LongName is not null)
			{
				longNameOptions.Add(option.LongName, option);
			}

			if (option.DependsOnAnotherParameter)
			{
				optionsToVerify.Add(option);
			}
		}

		// assign parameter values
		int argumentIndex = 0;
		HashSet<CommandParameter> assignedParameters = [];

		for (int i = 0; i < argsSpan.Length; i++)
		{
			CommandParameter? parameter;
			string value;

			if (argsSpan[i].StartsWith("-", StringComparison.Ordinal))
			{
				(string name, value, bool isLongName) = ParseOption(argsSpan[i], commandType);

				if (isLongName && name == "help")
				{
					if (isDefaultCommand)
					{
						throw new ShowHelpException();
					}
					else
					{
						throw new ShowHelpException(commandType);
					}
				}

				parameter = isLongName
					? longNameOptions.TryGetValue(name, out var longOption)
						? longOption
						: throw new OptionNotFoundException(name, ConsoleCommand.GetName(command), commandType)
					: (shortNameOptions.TryGetValue(name[0], out var shortOption)
						? shortOption
						: throw new OptionNotFoundException(name, ConsoleCommand.GetName(command), commandType));
			}
			else
			{
				if (argumentIndex >= arguments.Length)
				{
					throw new ArgumentOutOfBounds(arguments.Length, commandType);
				}

				parameter = arguments[argumentIndex++];
				value = argsSpan[i];
			}

			if (!assignedParameters.Add(parameter) && parseOptions.ThrowOnDuplicateArgument)
			{
				throw new DuplicateParameterException(parameter.GetNames(), commandType);
			}

			parameter.SetValue(command, ParseParameterValue(parameter.Type, parameter.GetNames(), parameter.CommandType, value, parseOptions));
		}

		// validate required options have been assigned
		foreach (var parameter in parameters.Where(parameter => parameter.IsRequired && (parameter is not CommandOption option || !option.DependsOnAnotherParameter)))
		{
			if (!assignedParameters.Contains(parameter))
			{
				throw new ParameterNotAssignedException(parameter.GetNames(), commandType);
			}
		}

		foreach (var option in optionsToVerify)
		{
			if (option.DependsOnAnotherParameter)
			{
				int numbNotEquals = 0;

				foreach (var (name, val) in option.GetDependencies())
				{
					var parentParameter = parameters.FirstOrDefault(option => option.PropName == name) ?? throw new InvalidOptionDepencyException(option.GetNames(), name);
					object? parentVal = parentParameter.GetValue(command);

					if (!Equals(parentVal, val))
					{
						numbNotEquals++;
					}
				}

				if (numbNotEquals == 0)
				{
					if (option.IsRequired && !assignedParameters.Contains(option))
					{
						throw new ParameterNotAssignedException(option.GetNames(), commandType);
					}
				}
				else
				{
					if (assignedParameters.Contains(option))
					{
						throw new InvalidParameterAssignedException(option.GetNames(), commandType);
					}
				}
			}
		}

		return command;
	}

	private static (string Name, string Value, bool IsLongName) ParseOption(ReadOnlySpan<char> arg, Type commandType)
	{
		if (!arg.StartsWith("-", StringComparison.Ordinal))
		{
			throw new InvalidOptionFormat(new string(arg), commandType);
		}

		// remove - or --
		arg = arg[1..];

		bool isLongName;
		if (arg.StartsWith("-", StringComparison.Ordinal))
		{
			arg = arg[1..];
			isLongName = true;
		}
		else
		{
			isLongName = false;
		}

		int equalsIndex = arg.IndexOf('=');

		string name;
		string value;
		if (equalsIndex == -1 || equalsIndex == arg.Length - 1)
		{
			name = new string(arg[..(equalsIndex == -1 ? arg.Length : equalsIndex)]);
			value = string.Empty;
		}
		else
		{
			value = new string(arg[(equalsIndex + 1)..]);
			name = new string(arg[..equalsIndex]);
		}

		return (name, value, isLongName);
	}

	/// <exception cref="InvalidParameterValueException"></exception>
	/// <exception cref="ParameterCreateException"></exception>
	private static object? ParseParameterValue(Type paramType, string paramName, Type commandType, string value, ParseOptions parseOptions)
	{
		for (int i = 0; i < parseOptions.Parsers.Count; i++)
		{
			if (parseOptions.Parsers[i].CanParse(paramType))
			{
				return parseOptions.Parsers[i].Parse(value, parseOptions);
			}
		}

		if (paramType == typeof(string))
		{
			return value;
		}
		else if (paramType == typeof(char))
		{
			return !string.IsNullOrEmpty(value) && value.Length == 1
				? value[0]
				: throw new InvalidParameterValueException(paramName, $"'{value}' must be a single character.", paramType);
		}
		else if (paramType == typeof(bool))
		{
			return bool.TryParse(value, out bool result)
				? result
				: throw new InvalidParameterValueException(paramName, $"'{value}' isn't a valid bool.", commandType);
		}
		else if (paramType.IsEnum)
		{
			return EnumUtils.TryParse(paramType, value, out object? enumValue)
				? enumValue
				: throw new InvalidParameterValueException(paramName, $"'{value}' isn't a valid value for enum {paramType.Name}, valid values are: {string.Join(", ", EnumUtils.GetNames(paramType))}", commandType);
		}
		else if (paramType.HasGenericInterface(typeof(IParsable<>)))
		{
			InterfaceMapping mapping = paramType.GetInterfaceMap(typeof(IParsable<>).MakeGenericType([paramType]));

			try
			{
				return mapping.GetMappedMethod("Parse")!.Invoke(null, [value, null]);
			}
			catch (Exception ex)
			{
				throw new InvalidParameterValueException(paramName, ex, commandType);
			}
		}
		else if (ParameterUtils.IsCollection(paramType))
		{
			var elementType = ParameterUtils.GetElementType(paramType);
			if (ParameterUtils.IsCollection(elementType))
			{
				throw new ParameterCreateException(paramName, $"Nested collection types aren't supported.");
			}

			string[] split = value.Split(',');
			Array arr = Array.CreateInstance(elementType, split.Length);
			for (int i = 0; i < split.Length; i++)
			{
				arr.SetValue(ParseParameterValue(elementType, paramName, commandType, split[i], parseOptions), i);
			}

			MethodInfo createInstance = typeof(ParameterUtils).GetMethod(nameof(ParameterUtils.CreateCollectionInstance), BindingFlags.Public | BindingFlags.Static)!
				.MakeGenericMethod(elementType);

			return createInstance.Invoke(null, [paramType, arr]);
		}

		// string constructor
		var constructor = paramType.GetConstructor([typeof(string)]);

		if (constructor is not null)
		{
			return constructor.Invoke([value]);
		}

		// ReadOnlySpan<char> ctor
		constructor = paramType.GetConstructor([typeof(ReadOnlySpan<char>)]);

		if (constructor is not null)
		{
			// https://stackoverflow.com/a/60271513/15878562
			ParameterExpression param = Expression.Parameter(typeof(ReadOnlySpan<char>));

			var ctorCall = Expression.New(constructor, param);

			var delegateCtor = Expression.Lambda<SpanParseCtor>(ctorCall, [param]).Compile();

			return delegateCtor((ReadOnlySpan<char>)value);
		}

		throw new ParameterCreateException(paramName, $"The type '{paramType.FullName}' doesn't have a constructor that takes string/ReadOnlySpan<char> and no custom parser is defined for it.");
	}
}
