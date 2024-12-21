namespace CommandLineParser;

public interface ICommandParameterParser
{
	bool CanParse(Type type);

	object Parse(ReadOnlySpan<char> value, ParseOptions options);
}
