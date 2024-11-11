using System.Reflection;
using CommandLineParser.Attributes;
using CommandLineParser.Exceptions;

namespace CommandLineParser;

internal sealed class PositionalCommandOption : CommandOption
{
    private readonly PositionalOptionAttribute _optionAttrib;

    public PositionalCommandOption(PropertyInfo prop)
        : base(prop)
    {
        _optionAttrib = prop.GetCustomAttribute<PositionalOptionAttribute>()
            ?? throw new MissingAttributeException(prop.Name, typeof(PositionalOptionAttribute));

        if (prop.GetCustomAttribute<NamedOptionAttribute>() is not null)
        {
            throw new ArgumentException($"{nameof(prop)} cannot have both {nameof(NamedOptionAttribute)} and {nameof(PositionalOptionAttribute)}.", nameof(prop));
        }
    }

    public string Name => _optionAttrib.Name;

    public int Order => _optionAttrib.Order;

    public override object? GetValue(ConsoleCommand instance)
        => _prop.GetGetMethod()!.Invoke(instance, []);

    public override void SetValue(ConsoleCommand instance, object? value)
        => _prop.GetSetMethod()!.Invoke(instance, [value]);

    public override string GetNames()
        => Name;
}
