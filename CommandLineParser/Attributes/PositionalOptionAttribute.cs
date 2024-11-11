using System.Runtime.CompilerServices;

namespace CommandLineParser.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class PositionalOptionAttribute : Attribute
{
    public PositionalOptionAttribute(string name, [CallerLineNumber] int order = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Order = order;
    }

    public string Name { get; }

    public int Order { get; }
}
