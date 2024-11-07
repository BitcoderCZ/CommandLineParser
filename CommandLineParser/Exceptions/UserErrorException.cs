namespace CommandLineParser.Exceptions
{
    public abstract class UserErrorException : Exception
    {
        protected UserErrorException(string? message)
            : base(message)
        {
        }
    }
}
