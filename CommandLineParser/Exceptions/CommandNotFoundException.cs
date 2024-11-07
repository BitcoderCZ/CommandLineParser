namespace CommandLineParser.Exceptions
{
    public sealed class CommandNotFoundException : UserErrorException
    {
        public CommandNotFoundException(string commandName)
            : base($"Command \"{commandName}\" doesn't exist.")
        {
        }
    }
}
