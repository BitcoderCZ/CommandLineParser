using CommandLineParser.Attributes;
using CommandLineParser.Exceptions;
using System.Reflection;

namespace CommandLineParser.CommandParameters;

internal sealed class CommandOption : CommandParameter
{
	private readonly OptionAttribute _optionAttrib;
	private readonly DependsOnAttribute[] _dependsOnAttribs;

	public CommandOption(PropertyInfo prop, Type commandType)
		: base(prop, commandType)
	{
		_optionAttrib = prop.GetCustomAttribute<OptionAttribute>()
			?? throw new MissingAttributeException(prop.Name, typeof(OptionAttribute));

		if (prop.GetCustomAttribute<ArgumentAttribute>() is not null)
		{
			throw new ArgumentException($"{nameof(prop)} cannot have both {nameof(OptionAttribute)} and {nameof(ArgumentAttribute)}.", nameof(prop));
		}

		_dependsOnAttribs = prop.GetCustomAttributes()
			.OfType<DependsOnAttribute>()
			.ToArray();
	}

	public char? ShortName => _optionAttrib.ShortName;

	public string? LongName => _optionAttrib.LongName;

	public bool DependsOnAnotherParameter => _dependsOnAttribs.Length > 0;

	public override string GetNames()
		=> ShortName is null
			? "--" + LongName
			: LongName is null
			? "-" + ShortName.Value
			: "-" + ShortName.Value + "|--" + LongName;

	public IEnumerable<(string Name, object? Value)> GetDependencies()
		=> _dependsOnAttribs.Select(attrib => (attrib.PropertyName, attrib.PropertyValue));
}
