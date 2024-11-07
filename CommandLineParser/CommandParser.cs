using System.Collections.Frozen;
using System.Data;
using System.Linq.Expressions;
using CommandLineParser.Exceptions;
using CommandLineParser.Utils;

namespace CommandLineParser
{
    public static class CommandParser
    {
        private static readonly FrozenDictionary<Type, Func<CommandOption, string, object>> ParseBasicTypes = new Dictionary<Type, Func<CommandOption, string, object>>()
        {
            [typeof(string)] = (option, value)
                => value,
            [typeof(bool)] = (option, value)
                => string.IsNullOrWhiteSpace(value) || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                    ? true
                    : value.Equals("false", StringComparison.OrdinalIgnoreCase)
                    ? (object)false
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid bool."),
            [typeof(byte)] = (option, value)
                => byte.TryParse(value, out byte parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid byte."),
            [typeof(sbyte)] = (option, value)
                => sbyte.TryParse(value, out sbyte parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid sbyte."),
            [typeof(short)] = (option, value)
                => short.TryParse(value, out short parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid short."),
            [typeof(ushort)] = (option, value)
                => ushort.TryParse(value, out ushort parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid ushort."),
            [typeof(int)] = (option, value)
                => int.TryParse(value, out int parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid int."),
            [typeof(uint)] = (option, value)
                => uint.TryParse(value, out uint parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid uint."),
            [typeof(long)] = (option, value)
                => long.TryParse(value, out long parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid long."),
            [typeof(ulong)] = (option, value)
                => ulong.TryParse(value, out ulong parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid ulong."),
            [typeof(float)] = (option, value)
                => float.TryParse(value, out float parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid float."),
            [typeof(double)] = (option, value)
                => double.TryParse(value, out double parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid double."),
            [typeof(decimal)] = (option, value)
                => decimal.TryParse(value, out decimal parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid decimal."),
            [typeof(nint)] = (option, value)
                => nint.TryParse(value, out nint parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid nint."),
            [typeof(nuint)] = (option, value)
                => nuint.TryParse(value, out nuint parsed)
                    ? parsed
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a nuint byte."),
        }.ToFrozenDictionary();

        private delegate object SpanParseCtor(ReadOnlySpan<char> span);

        public static void ParseAndRun(string[] arguments, ParseOptions parseOptions, Type? defaultCommand, params Type[] commands)
        {
            // TODO: try-catch, provide help text
            // TODO: help command, run if nothing is specified if the defaultCommand is null or it has options (if it doesn't have options, run it), help - description of all commands, help [command_name] - description of a specific command
            // TODO: version command - use Assembly version
            ConsoleCommand? command = null;

            ReadOnlySpan<string> args;

            // find which command was called
            if (defaultCommand is not null && (arguments.Length == 0 || arguments[0].StartsWith('-')))
            {
                command = ConsoleCommand.CreateInstance(defaultCommand);
                args = arguments;
            }
            else
            {
                if (arguments.Length == 0 || arguments[0].StartsWith('-'))
                {
                    throw new NoCommandSpecified();
                }

                string commandName = arguments[0];
                args = arguments.AsSpan(1);

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
                    throw new CommandNotFoundException(commandName);
                }
            }

            Type commandType = command.GetType();

            var (positionalOptions, namedOptions) = ConsoleCommand.GetOptions(commandType);
            CommandOption[] allOptions = [.. positionalOptions, .. namedOptions];

            Dictionary<char, NamedCommandOption> shortNameOptions = [];
            Dictionary<string, NamedCommandOption> longNameOptions = [];
            List<NamedCommandOption> optionsToVerify = [];

            for (int i = 0; i < namedOptions.Length; i++)
            {
                var option = namedOptions[i];

                if (option.ShortName is not null)
                {
                    shortNameOptions.Add(option.ShortName.Value, option);
                }

                if (option.LongName is not null)
                {
                    longNameOptions.Add(option.LongName, option);
                }

                if (option.DependsOnAnotherOption)
                {
                    optionsToVerify.Add(option);
                }
            }

            // assign option values
            int positionalOptionIndex = 0;
            HashSet<CommandOption> assignedOption = [];

            for (int i = 0; i < args.Length; i++)
            {
                CommandOption? option;
                string value;

                if (args[i].StartsWith("-", StringComparison.Ordinal))
                {
                    (string name, value, bool isLongName) = ParseNamedArg(args[i]);

                    option = isLongName
                        ? longNameOptions.TryGetValue(name, out var longOption)
                            ? longOption
                            : throw new OptionNotFoundException(name, ConsoleCommand.GetName(command))
                        : (shortNameOptions.TryGetValue(name[0], out var shortOption)
                            ? shortOption
                            : throw new OptionNotFoundException(name, ConsoleCommand.GetName(command)));
                }
                else
                {
                    if (positionalOptionIndex >= positionalOptions.Length)
                    {
                        throw new PositionalOptionOutOfBounds(positionalOptions.Length);
                    }

                    option = positionalOptions[positionalOptionIndex++];
                    value = args[i];
                }

                if (!assignedOption.Add(option) && parseOptions.ThrowOnDuplicateArgument)
                {
                    throw new DuplicateOptionException(option.GetNames());
                }

                option.SetValue(command, ParseOptionValue(option, value, parseOptions));
            }

            // validate required options have been assigned
            foreach (var option in allOptions.Where(option => option.IsRequired && (option is not NamedCommandOption namedOption || !namedOption.DependsOnAnotherOption)))
            {
                if (!assignedOption.Contains(option))
                {
                    throw new RequiredOptionNotAssignedException(option.GetNames());
                }
            }

            foreach (var option in optionsToVerify)
            {
                if (option.DependsOnAnotherOption)
                {
                    int numbNotEquals = 0;

                    foreach (var (name, val) in option.GetDependencies())
                    {
                        var parentOption = namedOptions.FirstOrDefault(option => option.PropName == name) ?? throw new InvalidOptionDepencyException(option.GetNames(), name);
                        object? parentVal = parentOption.GetValue(command);

                        if (!(parentVal?.Equals(val) ?? (parentVal is null) == (val is null)))
                        {
                            numbNotEquals++;
                        }
                    }

                    if (numbNotEquals == 0)
                    {
                        if (option.IsRequired && !assignedOption.Contains(option))
                        {
                            throw new RequiredOptionNotAssignedException(option.GetNames());
                        }
                    }
                    else
                    {
                        if (assignedOption.Contains(option))
                        {
                            throw new InvalidOptionAssignedException(option.GetNames());
                        }
                    }
                }
            }

            command.Run();
        }

        private static (string Name, string Value, bool IsLongName) ParseNamedArg(ReadOnlySpan<char> arg)
        {
            if (!arg.StartsWith("-", StringComparison.Ordinal))
            {
                throw new InvalidArgumentFormat(new string(arg));
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

        private static object ParseOptionValue(CommandOption option, string value, ParseOptions parseOptions)
        {
            if (ParseBasicTypes.TryGetValue(option.Type, out var parseFunc))
            {
                return parseFunc.Invoke(option, value);
            }
            else if (option.Type.IsEnum)
            {
                return EnumExtension.TryParse(option.Type, value, out object? enumValue)
                    ? enumValue
                    : throw new InvalidArgValueException(option.GetNames(), $"'{value}' isn't a valid for enum {option.Type.Name}, valid values are: {string.Join(", ", EnumExtension.GetNames(option.Type))}");
            }

            for (int i = 0; i < parseOptions.Parsers.Count; i++)
            {
                if (parseOptions.Parsers[i].CanParse(option.Type))
                {
                    return parseOptions.Parsers[i].Parse(value, parseOptions);
                }
            }

            // string constructor
            var constructor = option.Type.GetConstructor([typeof(string)]);

            if (constructor is not null)
            {
                return constructor.Invoke([value]);
            }

            // ReadOnlySpan<char> ctor
            constructor = option.Type.GetConstructor([typeof(ReadOnlySpan<char>)]);

            if (constructor is not null)
            {
                // https://stackoverflow.com/a/60271513/15878562
                ParameterExpression param = Expression.Parameter(typeof(ReadOnlySpan<char>));

                var ctorCall = Expression.New(constructor, param);

                var delegateCtor = Expression.Lambda<SpanParseCtor>(ctorCall, [param]).Compile();

                return delegateCtor((ReadOnlySpan<char>)value);
            }

            throw new Exception($"The option '{option.GetNames()}' couldn't be parsed because it doesn't have a constructor that takes string/ReadOnlySpan<char> and no custom parser is defined for it.");
        }
    }
}
