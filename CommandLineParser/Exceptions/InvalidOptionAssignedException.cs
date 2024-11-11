namespace CommandLineParser.Exceptions;

public sealed class InvalidOptionAssignedException : UserErrorException
{
    public InvalidOptionAssignedException(string optionName)
        : base($"Option '{optionName}' cannot be assigned because not all options it depends on have the specified value.")
    {
    }
}
