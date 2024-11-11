using System.Reflection;
using CommandLineParser.Attributes;

namespace CommandLineParser.CommandParameters;

internal abstract class CommandParameter
{
    protected readonly PropertyInfo _prop;

    protected readonly HelpTextAttribute? helpTextAttribute;
    protected readonly RequiredAttribute? requiredAttrib;

    protected CommandParameter(PropertyInfo prop)
    {
        if (!(prop.CanRead && prop.CanWrite && (prop.GetGetMethod(true)?.IsPublic ?? false) && (prop.GetSetMethod(true)?.IsPublic ?? false)))
        {
            throw new ArgumentException($"{nameof(prop)} must have a public getter and setter.", nameof(prop));
        }

        _prop = prop;

        helpTextAttribute = _prop.GetCustomAttribute<HelpTextAttribute>();
        requiredAttrib = _prop.GetCustomAttribute<RequiredAttribute>();
    }

    public Type Type => _prop.PropertyType;

    public string PropName => _prop.Name;

    public string? HelpText => helpTextAttribute?.HelpText;

    public bool IsRequired => requiredAttrib is not null;

    public abstract object? GetValue(ConsoleCommand instance);

    public abstract void SetValue(ConsoleCommand instance, object? value);

    public abstract string GetNames();
}
