namespace CommandLineParser.Exceptions;

public sealed class DuplicateOptionException : UserErrorException
{
    public DuplicateOptionException(string optionName)
        : base($"Option with the name '{optionName}' is defined multiple times.")
    {
    }
}
