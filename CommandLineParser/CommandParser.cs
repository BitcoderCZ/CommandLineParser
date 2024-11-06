using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLineParser.Attributes;
using CommandLineParser.Exceptions;
using CommandLineParser.Utils;

namespace CommandLineParser
{
    public class CommandParser
    {
        public static readonly CommandParser Default = new CommandParser();

        private CommandParser()
        {
        }

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
                    if (ConsoleCommand.GetName(ct) ==commandName)
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
                .Where(prop => prop.CanWrite && (prop.GetSetMethod(true)?.IsPublic ?? false))
                .Select(prop => new CommandOption(prop))
                .ToArray();

            Dictionary<char, int> shortNameOptions = [];
            Dictionary<string, int> longNameOptions = [];

            for (int i = 0; i < commandOptions.Length; i++)
            {
                var option = commandOptions[i];

                if (option.ShortName is not null && !shortNameOptions.TryAdd(option.ShortName.Value, i))
                {
                    throw new DuplicateOptionException(option.ShortName.Value.ToString());
                }

                if (option.LongName is not null && !longNameOptions.TryAdd(option.LongName, i))
                {
                    throw new DuplicateOptionException(option.LongName);
                }
            }

            // TODO: remove
            var rawOptions = new RawCommandOption[args.Length];
            HashSet<int> assignedOption = [];

            for (int i = 0; i < args.Length; i++)
            {
                var (name, value, isLongName) = ParseArg(args[i]);

                int optionIndex = -1;

                if (isLongName)
                {
                    if (!longNameOptions.TryGetValue(name, out optionIndex))
                    {
                        throw new OptionNotFoundException(name, ConsoleCommand.GetName(command));
                    }
                }
                else
                {
                    if (!shortNameOptions.TryGetValue(name[0], out optionIndex))
                    {
                        throw new OptionNotFoundException(name, ConsoleCommand.GetName(command));
                    }
                }

                if (!assignedOption.Add(optionIndex) && parseOptions.ThrowOnDuplicateArgument)
                {
                    throw new 
                }

                rawOptions[i] = new RawCommandOption(name, value, isLongName);
            }

            command.Run();
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
    }
}
