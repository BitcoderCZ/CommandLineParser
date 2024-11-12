using CommandLineParser.Attributes;

namespace CommandLineParser.Commands;

[HelpText("Shows version information.")]
[CommandName("version")]
internal class VersionCommand : ConsoleCommand
{
    public override void Run()
        => throw new InvalidOperationException();
}
