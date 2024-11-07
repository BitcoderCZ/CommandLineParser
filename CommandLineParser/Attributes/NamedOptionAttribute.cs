namespace CommandLineParser.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class NamedOptionAttribute : Attribute
    {
        public NamedOptionAttribute(char shortName)
        {
            ShortName = shortName;
        }

        public NamedOptionAttribute(string longName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(longName);

            longName = longName.Trim();

            if (longName.Length < 1)
            {
                throw new ArgumentException($"{nameof(longName)} must be longer than 1 character.", nameof(longName));
            }

            LongName = longName;
        }

        public NamedOptionAttribute(char shortName, string longName)
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
}
