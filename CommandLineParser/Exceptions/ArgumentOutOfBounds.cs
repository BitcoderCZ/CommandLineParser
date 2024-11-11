namespace CommandLineParser.Exceptions;

public sealed class ArgumentOutOfBounds : UserErrorException
{
    public ArgumentOutOfBounds(int positionalCount)
        : base($"There are only {positionalCount} arguments, but more were specified.")
    {
    }
}
