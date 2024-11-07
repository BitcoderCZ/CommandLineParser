namespace CommandLineParser.Exceptions
{
    public sealed class PositionalOptionOutOfBounds : UserErrorException
    {
        public PositionalOptionOutOfBounds(int positionalCount)
            : base($"There are only {positionalCount} positional options, but more were specified.")
        {
        }
    }
}
