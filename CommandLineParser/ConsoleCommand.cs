using System.Data;
using System.Reflection;
using CommandLineParser.Attributes;
using CommandLineParser.CommandParameters;
using CommandLineParser.Exceptions;
using CommandLineParser.Utils;

namespace CommandLineParser;

public abstract class ConsoleCommand
{
    public abstract void Run();

    internal static string GetName(ConsoleCommand command)
        => GetName(command.GetType());

    internal static string GetName(Type type)
        => type.GetCustomAttribute<CommandNameAttribute>()?.Name ?? StringUtils.ConvertToCliName(type.Name);

    internal static ConsoleCommand CreateInstance(Type commandType)
    {
        ValidateType(commandType);

        var constructor = commandType.GetConstructor(Type.EmptyTypes);

        return constructor is null
            ? throw new ArgumentException($"Type '{commandType}' must have a public parameterless constructor.", nameof(commandType))
            : (ConsoleCommand)constructor.Invoke([]);
    }

    internal static (CommandArgument[] Arguments, CommandOption[] Options) GetParameters(Type commandType)
    {
        ValidateType(commandType);

        var properties = commandType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => prop.CanRead && prop.CanWrite && (prop.GetGetMethod(true)?.IsPublic ?? false) && (prop.GetSetMethod(true)?.IsPublic ?? false))
            .ToArray();

        var arguments = properties
            .Where(prop => prop.GetCustomAttribute<ArgumentAttribute>() is not null)
            .Select(prop => new CommandArgument(prop, commandType))
            .OrderBy(arg => arg.Order)
            .ToArray();

        var duplicates = arguments
            .GroupBy(arg => arg.Order)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicates is not null)
        {
            using var enumerator = duplicates.GetEnumerator();
            enumerator.MoveNext();

            var first = enumerator.Current;
            enumerator.MoveNext();

            var second = enumerator.Current;

            throw new Exception($"Arguments '{first.Name}' and '{second.Name}' have the same order ({first.Order}).");
        }

        bool foundNonRequired = false;
        for (int i = 0; i < arguments.Length; i++)
        {
            if (!arguments[i].IsRequired)
            {
                foundNonRequired = true;
            }
            else if (arguments[i].IsRequired && foundNonRequired)
            {
                throw new Exception($"Required arguments must come before non required ones ({arguments[i - 1].Name}, {arguments[i].Name}).");
            }
        }

        var options = properties
            .Where(prop => prop.GetCustomAttribute<OptionAttribute>() is not null)
            .Select(prop => new CommandOption(prop, commandType))
            .ToArray();

        HashSet<char> shortNameOptions = [];
        HashSet<string> longNameOptions = [];

        for (int i = 0; i < options.Length; i++)
        {
            var option = options[i];

            if (option.ShortName is not null && !shortNameOptions.Add(option.ShortName.Value))
            {
                throw new DuplicateParameterException(option.ShortName.Value.ToString(), option.CommandType);
            }

            if (option.LongName is not null && !longNameOptions.Add(option.LongName))
            {
                throw new DuplicateParameterException(option.LongName, commandType);
            }
        }

        return
        (
            arguments,
            options
        );
    }

    internal static void ValidateType(Type commandType)
    {
        if (!IsCommand(commandType))
        {
            throw new ArgumentException($"Type '{commandType}' doesn't extend {typeof(ConsoleCommand)} class.", nameof(commandType));
        }
    }

    internal static bool IsCommand(Type? type)
        => typeof(ConsoleCommand).IsAssignableFrom(type);
}
