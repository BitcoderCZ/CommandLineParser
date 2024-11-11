namespace CommandLineParser;

public sealed class ParseOptions
{
    public bool ThrowOnDuplicateArgument { get; set; } = true;

    public List<ICommandParameterParser> Parsers { get; } = [];
}
