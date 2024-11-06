using System.Reflection;
using CommandLineParser.Attributes;
using CommandLineParser.Utils;

namespace CommandLineParser
{
    public abstract class ConsoleCommand
    {
        public abstract void Run();

        internal static string GetName(ConsoleCommand command)
            => GetName(command.GetType());

        internal static string GetName(Type type)
            => type.GetCustomAttribute<CommandNameAttribute>()?.Name ?? StringUtils.ConvertToCliName(type.Name);
    }
}
