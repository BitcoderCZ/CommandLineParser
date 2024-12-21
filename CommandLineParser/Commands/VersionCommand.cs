using CommandLineParser.Attributes;

namespace CommandLineParser.Commands;

[HelpText("Shows version information.")]
[CommandName("version")]
internal class VersionCommand : ConsoleCommand
{
	public override int Run()
		=> throw new InvalidOperationException();
}
