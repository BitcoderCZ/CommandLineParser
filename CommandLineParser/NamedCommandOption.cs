using System.Reflection;
using CommandLineParser.Attributes;
using CommandLineParser.Exceptions;

namespace CommandLineParser;

internal sealed class NamedCommandOption : CommandOption
{
    private readonly NamedOptionAttribute _optionAttrib;
    private readonly DependsOnAttribute[] _dependsOnAttribs;

    public NamedCommandOption(PropertyInfo prop)
        : base(prop)
    {
        _optionAttrib = prop.GetCustomAttribute<NamedOptionAttribute>()
            ?? throw new MissingAttributeException(prop.Name, typeof(NamedOptionAttribute));

        if (prop.GetCustomAttribute<PositionalOptionAttribute>() is not null)
        {
            throw new ArgumentException($"{nameof(prop)} cannot have both {nameof(NamedOptionAttribute)} and {nameof(PositionalOptionAttribute)}.", nameof(prop));
        }

        _dependsOnAttribs = prop.GetCustomAttributes()
            .OfType<DependsOnAttribute>()
            .ToArray();
    }

    public char? ShortName => _optionAttrib.ShortName;

    public string? LongName => _optionAttrib.LongName;

    public bool DependsOnAnotherOption => _dependsOnAttribs.Length > 0;

    public override object? GetValue(ConsoleCommand instance)
        => _prop.GetGetMethod()!.Invoke(instance, []);

    public override void SetValue(ConsoleCommand instance, object? value)
        => _prop.GetSetMethod()!.Invoke(instance, [value]);

    public override string GetNames()
        => ShortName is null
            ? "--" + LongName
            : LongName is null
            ? "-" + ShortName.Value
            : "-" + ShortName.Value + "|--" + LongName;

    public IEnumerable<(string Name, object? Value)> GetDependencies()
        => _dependsOnAttribs.Select(attrib => (attrib.PropertyName, attrib.PropertyValue));
}
