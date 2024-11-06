using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLineParser.Attributes;
using CommandLineParser.Exceptions;
using CommandLineParser.Utils;
using Microsoft.VisualBasic.FileIO;

namespace CommandLineParser
{
    public class CommandParser
    {
        public static readonly CommandParser Default = new CommandParser();

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

        private CommandParser()
        {
        }

        private delegate object SpanParseCtor(ReadOnlySpan<char> span);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "a")]
        public void ParseAndRun(string[] arguments, ParseOptions parseOptions, Type? defaultCommand, params Type[] commands)
        {
            ConsoleCommand? command = null;

            ReadOnlySpan<string> args;

            if (defaultCommand is not null && (arguments.Length == 0 || arguments[0].StartsWith('-')))
            {
                command = CreateCommandInstance(defaultCommand);
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
                        command = CreateCommandInstance(ct);
                        break;
                    }
                }

                if (command is null)
                {
                    throw new CommandNotFoundException(commandName);
                }
            }

            Type commandType = command.GetType();

            CommandOption[] commandOptions = commandType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.CanWrite && (prop.GetSetMethod(true)?.IsPublic ?? false) && prop.GetCustomAttribute<OptionNameAttribute>() is not null)
                .Select(prop => new CommandOption(prop))
                .ToArray();

            Dictionary<char, CommandOption> shortNameOptions = [];
            Dictionary<string, CommandOption> longNameOptions = [];

            for (int i = 0; i < commandOptions.Length; i++)
            {
                var option = commandOptions[i];

                if (option.ShortName is not null && !shortNameOptions.TryAdd(option.ShortName.Value, option))
                {
                    throw new DuplicateOptionException(option.ShortName.Value.ToString());
                }

                if (option.LongName is not null && !longNameOptions.TryAdd(option.LongName, option))
                {
                    throw new DuplicateOptionException(option.LongName);
                }
            }

            HashSet<CommandOption> assignedOption = [];

            for (int i = 0; i < args.Length; i++)
            {
                var (name, value, isLongName) = ParseArg(args[i]);

                CommandOption? option;

                if (isLongName)
                {
                    if (!longNameOptions.TryGetValue(name, out option))
                    {
                        throw new OptionNotFoundException(name, ConsoleCommand.GetName(command));
                    }
                }
                else
                {
                    if (!shortNameOptions.TryGetValue(name[0], out option))
                    {
                        throw new OptionNotFoundException(name, ConsoleCommand.GetName(command));
                    }
                }

                if (!assignedOption.Add(option) && parseOptions.ThrowOnDuplicateArgument)
                {
                    throw new DuplicateOptionException(option.GetNames());
                }

                option.SetValue(command, ParseOptionValue(option, value, parseOptions));
            }

            foreach (var option in commandOptions.Where(option => option.IsRequired))
            {
                if (!assignedOption.Contains(option))
                {
                    throw new RequiredOptionNotAssignedException(option.GetNames());
                }
            }

            command.Run();
        }

        private static ConsoleCommand CreateCommandInstance(Type commandType)
        {
            if (!typeof(ConsoleCommand).IsAssignableFrom(commandType))
            {
                throw new ArgumentException($"Type '{commandType}' doesn't extend {typeof(ConsoleCommand)} class.", nameof(commandType));
            }

            var constructor = commandType.GetConstructor(Type.EmptyTypes);

            return constructor is null
                ? throw new ArgumentException($"Type '{commandType}' must have a public parameterless constructor.", nameof(commandType))
                : (ConsoleCommand)constructor.Invoke([]);
        }

        private static (string Name, string Value, bool IsLongName) ParseArg(ReadOnlySpan<char> arg)
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

        private static object ParseOptionValue(CommandOption option, string value, ParseOptions options)
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

            for (int i = 0; i < options.Parsers.Count; i++)
            {
                if (options.Parsers[i].CanParse(option.Type))
                {
                    return options.Parsers[i].Parse(value, options);
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
