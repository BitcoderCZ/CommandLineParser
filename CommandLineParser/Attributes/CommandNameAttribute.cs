namespace CommandLineParser.Attributes;

/// <summary>
/// Specifies the name of a command.
/// Not required.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CommandNameAttribute : Attribute
{
    public CommandNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (name.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"{nameof(name)} cannot contain whitespace.", nameof(name));
        }

        Name = name;
    }

    public string Name { get; }
}
