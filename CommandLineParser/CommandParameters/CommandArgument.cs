using System.Reflection;
using CommandLineParser.Attributes;
using CommandLineParser.Exceptions;

namespace CommandLineParser.CommandParameters;

internal sealed class CommandArgument : CommandParameter
{
    private readonly ArgumentAttribute _argumentAttrib;

    public CommandArgument(PropertyInfo prop)
        : base(prop)
    {
        _argumentAttrib = prop.GetCustomAttribute<ArgumentAttribute>()
            ?? throw new MissingAttributeException(prop.Name, typeof(ArgumentAttribute));

        if (prop.GetCustomAttribute<OptionAttribute>() is not null)
        {
            throw new ArgumentException($"{nameof(prop)} cannot have both {nameof(OptionAttribute)} and {nameof(ArgumentAttribute)}.", nameof(prop));
        }
    }

    public string Name => _argumentAttrib.Name;

    public int Order => _argumentAttrib.Order;

    public override object? GetValue(ConsoleCommand instance)
        => _prop.GetGetMethod()!.Invoke(instance, []);

    public override void SetValue(ConsoleCommand instance, object? value)
        => _prop.GetSetMethod()!.Invoke(instance, [value]);

    public override string GetNames()
        => Name;
}
