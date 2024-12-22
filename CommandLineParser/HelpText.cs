using CommandLineParser.Attributes;
using CommandLineParser.CommandParameters;
using CommandLineParser.Commands;
using CommandLineParser.Exceptions;
using CommandLineParser.Utils;
using System.CodeDom.Compiler;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;

namespace CommandLineParser;

public static class HelpText
{
	public static void GenerateVersionInfo(TextWriter writer)
	{
		Assembly? assembly = Assembly.GetEntryAssembly();

		if (assembly is null)
		{
			writer.WriteLine("Failed to get version information.");
			return;
		}

		var name = assembly.GetName();

		FileVersionInfo? versionInfo = null;
		if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
		{
			versionInfo = FileVersionInfo.GetVersionInfo(Environment.ProcessPath);
		}

		writer.WriteLine($"{name.Name} {versionInfo?.ProductVersion ?? name.Version?.ToString()}");
		if (versionInfo is not null)
		{
			if (!string.IsNullOrWhiteSpace(versionInfo.LegalCopyright))
			{
				writer.WriteLine(versionInfo.LegalCopyright);
			}
			else if (!string.IsNullOrWhiteSpace(versionInfo.CompanyName))
			{
				writer.WriteLine($"Copyright (C) {DateTime.UtcNow.Year} {versionInfo.CompanyName}");
			}
		}

		writer.WriteLine();
	}

	public static void GenerateForCommands(IEnumerable<Type> commandTypes, TextWriter writer)
	{
		IndentedTextWriter indentedWriter = writer as IndentedTextWriter ?? new IndentedTextWriter(writer, "  ");

		int maxNameLen = commandTypes.Max(t => ConsoleCommand.GetName(t).Length);

		indentedWriter.WriteLine("Available commands:");

		indentedWriter.Indent++;
		foreach (Type commandType in commandTypes)
		{
			string name = ConsoleCommand.GetName(commandType);
			indentedWriter.Write(name);

			var commandHelpTextAttrib = commandType.GetCustomAttribute<HelpTextAttribute>();
			if (commandHelpTextAttrib is not null)
			{
				indentedWriter.WriteSpaces(maxNameLen - name.Length);
				indentedWriter.WriteLine("  - " + commandHelpTextAttrib.HelpText);
			}
			else
			{
				indentedWriter.WriteLine();
			}
		}

		indentedWriter.Indent--;
	}

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
			var (greaterThan, lessThan) = option.GetRangeValues();
			WriteValidParameterValues(option.Type, greaterThan, lessThan, writer);
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

		indentedWriter.WriteLine("ERROR:");
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

	private static void WriteValidParameterValues(Type paramType, object? greaterThan, object? lessThan, TextWriter writer)
	{
		if (paramType == typeof(char))
		{
			writer.Write("<character>");
		}
		else if (paramType == typeof(bool))
		{
			writer.Write("true|false");
		}
		else if (paramType.IsEnum)
		{
			writer.Write(string.Join('|', EnumUtils.GetNames(paramType)));
		}
		else if (ParameterUtils.IsCollection(paramType))
		{
			Type elementType = ParameterUtils.GetElementType(paramType);
			if (ParameterUtils.IsCollection(elementType))
			{
				writer.Write("<text>");
				return;
			}

			WriteValidParameterValues(elementType, greaterThan, lessThan, writer);
			writer.Write("[]");
		}
		else if (TypeUtils.IsNumber(paramType))
		{
			if (TypeUtils.IsFloatingPoint(paramType))
			{
				writer.Write("<decimal number");
			}
			else if (TypeUtils.IsInteger(paramType))
			{
				writer.Write("<whole number");
			}
			else
			{
				writer.Write("<number");
			}

			object? min;
			object? max;

			if (TypeUtils.TryGetMinMaxValues(paramType, out min, out max) || greaterThan is not null || lessThan is not null)
			{
				string compSymbol1 = "<=";
				string compSymbol2 = "<=";

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

		var (greaterThan, lessThan) = parameter.GetRangeValues();
		WriteValidParameterValues(parameter.Type, greaterThan, lessThan, writer);

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
