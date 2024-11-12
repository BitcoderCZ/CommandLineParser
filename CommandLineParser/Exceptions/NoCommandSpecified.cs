namespace CommandLineParser.Exceptions;

public sealed class NoCommandSpecified : UserErrorException
{
    public NoCommandSpecified()
        : base("No command was specified.", null)
    {
    }
}
