namespace CommandLineParser.Attributes
{
    /// <summary>
    /// Provides description for a command or an option.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description)
        {
            ArgumentException.ThrowIfNullOrEmpty(description);

            Description = description;
        }

        public string Description { get; }
    }
}
