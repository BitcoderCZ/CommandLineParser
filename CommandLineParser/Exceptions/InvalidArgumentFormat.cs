namespace CommandLineParser.Exceptions;

public sealed class InvalidArgumentFormat : UserErrorException
{
    public InvalidArgumentFormat(string argName)
        : base($"Argument \"{argName}\" isn't correctly formated.")
    {
    }
}
