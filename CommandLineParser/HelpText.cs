using System.CodeDom.Compiler;
using System.Data;
using System.Reflection;
using CommandLineParser.Attributes;
using CommandLineParser.CommandParameters;
using CommandLineParser.Exceptions;
using CommandLineParser.Utils;

namespace CommandLineParser;

public static class HelpText
{
    public static void GenerateForCommand(Type commandType, TextWriter writer)
    {
        // required to get the default values
        ConsoleCommand command = ConsoleCommand.CreateInstance(commandType);

        var (arguments, options) = ConsoleCommand.GetParameters(commandType);

        writer.Write("Usage: " + ConsoleCommand.GetName(commandType));

        foreach (var arg in arguments)
        {
            writer.Write(arg.IsRequired ? " <" : " [");
            writer.Write(arg.Name);
            writer.Write(arg.IsRequired ? '>' : ']');
        }

        int nonReqNamedCount = 0;

        foreach (var option in options)
        {
            if (!option.IsRequired || option.DependsOnAnotherParameter)
            {
                nonReqNamedCount++;
                continue;
            }

            writer.Write(' ');
            writer.Write(option.GetNames());
            writer.Write('=');
            WriteValidParameterValues(option, writer);
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

        if (arguments.Length > 0)
        {
            writer.WriteLine("Arguments:");

            string[] arr = new string[arguments.Length * 2];

            for (int i = 0; i < arguments.Length; i++)
            {
                arr[(i * 2) + 0] = arguments[i].GetNames();
                arr[(i * 2) + 1] = GetParameterDescription(arguments[i], command);
            }

            writer.Write2D(arr, consoleWidth, 2);

            writer.WriteLine();
        }

        if (options.Length > 0)
        {
            writer.WriteLine("Options:");

            string[] arr = new string[options.Length * 2];

            for (int i = 0; i < options.Length; i++)
            {
                arr[(i * 2) + 0] = options[i].GetNames();
                arr[(i * 2) + 1] = GetParameterDescription(options[i], command);
            }

            writer.Write2D(arr, consoleWidth, 2);

            writer.WriteLine();
        }
    }

    public static void GenerateForError(UserErrorException exception, TextWriter writer)
    {
        IndentedTextWriter indentedWriter = writer as IndentedTextWriter ?? new IndentedTextWriter(writer, "  ");

        indentedWriter.WriteLine("Error:");
        indentedWriter.Indent++;

        indentedWriter.WriteLine(exception.Message);

        if (exception.InnerException is not null)
        {
            indentedWriter.Indent++;

            WriteException(exception.InnerException);

            indentedWriter.Indent--;
        }

        indentedWriter.Indent--;

        indentedWriter.WriteLine();

        void WriteException(Exception? exception)
        {
            while (exception is not null)
            {
                if (exception is AggregateException aggregateException)
                {
                    foreach (Exception ex in aggregateException.InnerExceptions)
                    {
                        WriteException(ex);
                    }

                    return;
                }
                else if (exception is not TargetInvocationException)
                {
                    indentedWriter.WriteLine(exception.Message);
                }

                exception = exception.InnerException;
            }
        }
    }

    private static void WriteValidParameterValues(CommandParameter parameter, TextWriter writer)
    {
        if (parameter.Type == typeof(char))
        {
            writer.Write("<character>");
        }
        else if (parameter.Type == typeof(bool))
        {
            writer.Write("true|false");
        }
        else if (parameter.Type.IsEnum)
        {
            writer.Write(string.Join('|', EnumUtils.GetNames(parameter.Type)));
        }
        else if (TypeUtils.IsNumber(parameter.Type))
        {
            if (TypeUtils.IsFloatingPoint(parameter.Type))
            {
                writer.Write("<decimal number");
            }
            else if (TypeUtils.IsInteger(parameter.Type))
            {
                writer.Write("<whole number");
            }
            else
            {
                writer.Write("<number");
            }

            if (TypeUtils.TryGetMinMaxValues(parameter.Type, out object? min, out object? max))
            {
                string compSymbol1 = "<=";
                string compSymbol2 = "<=";

                if (parameter.HasRangeRequirements)
                {
                    var (greaterThan, lessThan) = parameter.GetRangeValues();

                    if (greaterThan is not null)
                    {
                        min = greaterThan;
                        compSymbol1 = "<";
                    }

                    if (lessThan is not null)
                    {
                        max = lessThan;
                        compSymbol2 = "<";
                    }
                }

                writer.Write($", {ObjectUtils.ToString(min)} {compSymbol1} value {compSymbol2} {ObjectUtils.ToString(max)}>");
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

    private static string GetParameterDescription(CommandParameter parameter, ConsoleCommand command)
    {
        StringWriter writer = new StringWriter();

        WriteValidParameterValues(parameter, writer);

        writer.Write(' ');

        if (parameter is CommandOption option && option.DependsOnAnotherParameter)
        {
            var (args, options) = ConsoleCommand.GetParameters(command.GetType());

            IEnumerable<CommandParameter> parameters = [.. args, .. options];

            writer.Write("Only valid when: ");

            writer.Write(StringUtils.JoinAnd(option.GetDependencies().Select(item =>
            {
                var parentParam = parameters.FirstOrDefault(option => option.PropName == item.Name) ?? throw new InvalidOptionDepencyException(option.GetNames(), item.Name);

                return $"'{parentParam.GetNames()}' has the value '{ObjectUtils.ToString(item.Value)}'";
            })));
            writer.Write(". ");
        }

        if (parameter.IsRequired)
        {
            writer.Write("Required.");
        }
        else
        {
            writer.Write($"Default: '{ObjectUtils.ToString(parameter.GetValue(command))}'.");
        }

        if (!string.IsNullOrEmpty(parameter.HelpText))
        {
            writer.Write(' ');
            writer.Write(parameter.HelpText);
        }

        return writer.ToString();
    }
}
