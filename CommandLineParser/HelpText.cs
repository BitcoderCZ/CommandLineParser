using System.Reflection;
using CommandLineParser.Attributes;
using CommandLineParser.Utils;

namespace CommandLineParser;

public static class HelpText
{
    public static void Generate(Type commandType, TextWriter writer)
    {
        // required to get the default values
        ConsoleCommand command = ConsoleCommand.CreateInstance(commandType);

        var (positional, named) = ConsoleCommand.GetOptions(commandType);

        writer.Write("Usage: " + ConsoleCommand.GetName(commandType));

        foreach (var option in positional)
        {
            writer.Write(option.IsRequired ? " <" : " [");
            writer.Write(option.Name);
            writer.Write(option.IsRequired ? '>' : ']');
        }

        int nonReqNamedCount = 0;

        foreach (var option in named)
        {
            if (!option.IsRequired)
            {
                nonReqNamedCount++;
                continue;
            }

            writer.Write(' ');
            writer.Write(option.GetNames());
            writer.Write('=');
            WriteValidOptionValues(option, writer);
        }

        if (nonReqNamedCount > 0)
        {
            writer.Write(" [options]");
        }

        writer.WriteLine();
        writer.WriteLine();

        var commandHelpTextAttrib = commandType.GetCustomAttribute<HelpTextAttribute>();
        if (commandHelpTextAttrib is not null)
        {
            writer.WriteLine(commandHelpTextAttrib.HelpText);
            writer.WriteLine();
        }

        int consoleWidth = Console.WindowWidth;

        if (positional.Length > 0)
        {
            writer.WriteLine("Arguments:");

            string[] arr = new string[positional.Length * 2];

            for (int i = 0; i < positional.Length; i++)
            {
                arr[(i * 2) + 0] = positional[i].GetNames();
                arr[(i * 2) + 1] = GetOptionDescription(positional[i], command);
            }

            writer.Write2D(arr, consoleWidth, 2);

            writer.WriteLine();
        }

        if (named.Length > 0)
        {
            writer.WriteLine("Options:");

            string[] arr = new string[named.Length * 2];

            for (int i = 0; i < named.Length; i++)
            {
                arr[(i * 2) + 0] = named[i].GetNames();
                arr[(i * 2) + 1] = GetOptionDescription(named[i], command);
            }

            writer.Write2D(arr, consoleWidth, 2);

            writer.WriteLine();
        }
    }

    private static void WriteValidOptionValues(CommandOption option, TextWriter writer)
    {
        if (option.Type == typeof(char))
        {
            writer.Write("<character>");
        }
        else if (option.Type == typeof(bool))
        {
            writer.Write("true|false");
        }
        else if (option.Type.IsEnum)
        {
            writer.Write(string.Join('|', EnumUtils.GetNames(option.Type)));
        }
        else if (TypeUtils.IsNumber(option.Type))
        {
            if (TypeUtils.IsFloatingPoint(option.Type))
            {
                writer.Write("<decimal number");
            }
            else if (TypeUtils.IsInteger(option.Type))
            {
                writer.Write("<whole number");
            }
            else
            {
                writer.Write("<number");
            }

            // TODO: account for greater than, less than attribs ...
            if (TypeUtils.TryGetMinMaxValues(option.Type, out object? min, out object? max))
            {
                writer.Write($", {min} to {max}>");
            }
            else
            {
                writer.Write('>');
            }
        }
        else
        {
            writer.Write("<text>");
        }
    }

    private static string GetOptionDescription(CommandOption option, ConsoleCommand command)
    {
        StringWriter writer = new StringWriter();

        WriteValidOptionValues(option, writer);

        writer.Write(' ');

        if (option.IsRequired)
        {
            writer.Write("Required.");
        }
        else
        {
            writer.Write($"Default: '{ObjectUtils.ToString(option.GetValue(command))}'.");
        }

        if (!string.IsNullOrEmpty(option.HelpText))
        {
            writer.Write(' ');
            writer.Write(option.HelpText);
        }

        return writer.ToString();
    }
}
