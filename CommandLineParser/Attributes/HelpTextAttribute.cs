namespace CommandLineParser.Attributes;

/// <summary>
/// Provides description for a command or an option.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class HelpTextAttribute : Attribute
{
    public HelpTextAttribute(string helpText)
    {
        ArgumentException.ThrowIfNullOrEmpty(helpText);

        HelpText = helpText.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty);
    }

    public string HelpText { get; }
}
