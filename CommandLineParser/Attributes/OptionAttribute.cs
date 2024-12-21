namespace CommandLineParser.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class OptionAttribute : Attribute
{
	public OptionAttribute(char shortName)
	{
		ShortName = shortName;
	}

	public OptionAttribute(string longName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(longName);

		longName = longName.Trim();

		if (longName.Length < 1)
		{
			throw new ArgumentException($"{nameof(longName)} must be longer than 1 character.", nameof(longName));
		}

		LongName = longName;
	}

	public OptionAttribute(char shortName, string longName)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(longName);

		longName = longName.Trim();

		if (longName.Length < 1)
		{
			throw new ArgumentException($"{nameof(longName)} must be longer than 1 character.", nameof(longName));
		}

		ShortName = shortName;

		LongName = longName;
	}

	public char? ShortName { get; }

	public string? LongName { get; }
}
