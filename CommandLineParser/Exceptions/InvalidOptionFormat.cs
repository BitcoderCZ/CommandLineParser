namespace CommandLineParser.Exceptions;

public sealed class InvalidOptionFormat : UserErrorException
{
    public InvalidOptionFormat(string optionName)
        : base($"Option \"{optionName}\" isn't correctly formated.")
    {
    }
}
