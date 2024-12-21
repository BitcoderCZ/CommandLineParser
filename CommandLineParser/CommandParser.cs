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
	private static readonly FrozenDictionary<Type, Func<CommandParameter, string, object>> ParseBasicTypes = new Dictionary<Type, Func<CommandParameter, string, object>>()
	{
		[typeof(string)] = (param, value)
			=> value,
		[typeof(char)] = (param, value)
			=> !string.IsNullOrEmpty(value) && value.Length == 1
				? value[0]
				: throw new InvalidParameterValueException(param.GetNames(), $"'{value}' must be a single character.", param.CommandType),
		[typeof(bool)] = (param, value)
			=> string.IsNullOrWhiteSpace(value) || value.Equals("true", StringComparison.OrdinalIgnoreCase)
				? true
				: value.Equals("false", StringComparison.OrdinalIgnoreCase)
				? (object)false
				: throw new InvalidParameterValueException(param.GetNames(), $"'{value}' isn't a valid bool.", param.CommandType),
	}.ToFrozenDictionary();

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
	public static int ParseAndRun(string[] args, ParseOptions parseOptions, Type? defaultCommand, IEnumerable<Type> commands, TextWriter? helpTextWriter = null)
	{
		commands = new HashSet<Type>(commands);
		var commandsSet = (HashSet<Type>)commands;

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
			command = ParseInternal(args, parseOptions, defaultCommand, commands);
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
				HelpText.GenerateForCommands(commands, helpTextWriter);
			}

			return 1;
		}
		catch (ShowHelpException ex)
		{
			HelpText.GenerateVersionInfo(helpTextWriter);
			HelpText.GenerateForCommand(ex.CommandType, helpTextWriter);
			return 0;
		}

		if (command is HelpCommand)
		{
			HelpText.GenerateVersionInfo(helpTextWriter);
			HelpText.GenerateForCommands(commands, helpTextWriter);
			return 0;
		}
		else if (command is VersionCommand)
		{
			HelpText.GenerateVersionInfo(helpTextWriter);
			return 0;
		}

		return command.Run();
	}

	private static ConsoleCommand ParseInternal(string[] args, ParseOptions parseOptions, Type? defaultCommand, IEnumerable<Type> commands)
	{
		ConsoleCommand? command = null;

		ReadOnlySpan<string> argsSpan;

		// find which command was called
		if (defaultCommand is not null && (args.Length == 0 || args[0].StartsWith('-')))
		{
			command = ConsoleCommand.CreateInstance(defaultCommand);
			argsSpan = args;
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
					throw new ShowHelpException(commandType);
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

			parameter.SetValue(command, ParseParameterValue(parameter, value, parseOptions));
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

	private static object? ParseParameterValue(CommandParameter parameter, string value, ParseOptions parseOptions)
	{
		for (int i = 0; i < parseOptions.Parsers.Count; i++)
		{
			if (parseOptions.Parsers[i].CanParse(parameter.Type))
			{
				return parseOptions.Parsers[i].Parse(value, parseOptions);
			}
		}

		if (ParseBasicTypes.TryGetValue(parameter.Type, out var parseFunc))
		{
			return parseFunc.Invoke(parameter, value);
		}
		else if (parameter.Type.IsEnum)
		{
			return EnumUtils.TryParse(parameter.Type, value, out object? enumValue)
				? enumValue
				: throw new InvalidParameterValueException(parameter.GetNames(), $"'{value}' isn't a valid value for enum {parameter.Type.Name}, valid values are: {string.Join(", ", EnumUtils.GetNames(parameter.Type))}", parameter.CommandType);
		}
		else if (parameter.Type.HasGenericInterface(typeof(IParsable<>)))
		{
			InterfaceMapping mapping = parameter.Type.GetInterfaceMap(typeof(IParsable<>).MakeGenericType([parameter.Type]));

			try
			{
				return mapping.GetMappedMethod("Parse")!.Invoke(null, [value, null]);
			}
			catch (Exception ex)
			{
				throw new InvalidParameterValueException(parameter.GetNames(), ex, parameter.CommandType);
			}
		}

		// string constructor
		var constructor = parameter.Type.GetConstructor([typeof(string)]);

		if (constructor is not null)
		{
			return constructor.Invoke([value]);
		}

		// ReadOnlySpan<char> ctor
		constructor = parameter.Type.GetConstructor([typeof(ReadOnlySpan<char>)]);

		if (constructor is not null)
		{
			// https://stackoverflow.com/a/60271513/15878562
			ParameterExpression param = Expression.Parameter(typeof(ReadOnlySpan<char>));

			var ctorCall = Expression.New(constructor, param);

			var delegateCtor = Expression.Lambda<SpanParseCtor>(ctorCall, [param]).Compile();

			return delegateCtor((ReadOnlySpan<char>)value);
		}

		throw new Exception($"The option '{parameter.GetNames()}' couldn't be parsed because it doesn't have a constructor that takes string/ReadOnlySpan<char> and no custom parser is defined for it.");
	}
}
