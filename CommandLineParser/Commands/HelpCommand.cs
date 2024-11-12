using CommandLineParser.Attributes;

namespace CommandLineParser.Commands;

[HelpText("Shows this help screen.")]
[CommandName("help")]
internal class HelpCommand : ConsoleCommand
{
    public override void Run()
        => new InvalidOperationException();
}
