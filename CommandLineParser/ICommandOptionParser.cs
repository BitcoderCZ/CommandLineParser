namespace CommandLineParser
{
    public interface ICommandOptionParser
    {
        bool CanParse(Type type);

        object Parse(ReadOnlySpan<char> value, ParseOptions options);
    }
}
