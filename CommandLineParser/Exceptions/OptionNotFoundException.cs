namespace CommandLineParser.Exceptions
{
    public sealed class OptionNotFoundException : UserErrorException
    {
        public OptionNotFoundException(string optionName, string commandName)
            : base($"Option '{optionName}' isn't defined by command '{commandName}'.")
        {
        }
    }
}
