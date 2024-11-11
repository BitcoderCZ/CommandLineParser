namespace CommandLineParser.Exceptions;

public sealed class RequiredOptionNotAssignedException : UserErrorException
{
    public RequiredOptionNotAssignedException(string optionName)
        : base($"Required option '{optionName}' wasn't assigned.")
    {
    }
}
