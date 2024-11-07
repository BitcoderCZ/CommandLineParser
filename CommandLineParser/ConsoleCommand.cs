using System.Data;
using System.Reflection;
using CommandLineParser.Attributes;
using CommandLineParser.Exceptions;
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

        internal static ConsoleCommand CreateInstance(Type commandType)
        {
            ValidateType(commandType);

            var constructor = commandType.GetConstructor(Type.EmptyTypes);

            return constructor is null
                ? throw new ArgumentException($"Type '{commandType}' must have a public parameterless constructor.", nameof(commandType))
                : (ConsoleCommand)constructor.Invoke([]);
        }

        internal static (PositionalCommandOption[] PositionalOptions, NamedCommandOption[] NamedOptions) GetOptions(Type commandType)
        {
            ValidateType(commandType);

            var properties = commandType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => prop.CanRead && prop.CanWrite && (prop.GetGetMethod(true)?.IsPublic ?? false) && (prop.GetSetMethod(true)?.IsPublic ?? false))
                .ToArray();

            var positional = properties
                .Where(prop => prop.GetCustomAttribute<PositionalOptionAttribute>() is not null)
                .Select(prop => new PositionalCommandOption(prop))
                .OrderBy(option => option.Order)
                .ToArray();

            var duplicates = positional
                .GroupBy(option => option.Order)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicates is not null)
            {
                using var enumerator = duplicates.GetEnumerator();
                enumerator.MoveNext();

                var first = enumerator.Current;
                enumerator.MoveNext();

                var second = enumerator.Current;

                throw new Exception($"Positional options '{first.Name}' and '{second.Name}' have the same order ({first.Order}).");
            }

            bool foundNonRequired = false;
            for (int i = 0; i < positional.Length; i++)
            {
                if (!positional[i].IsRequired)
                {
                    foundNonRequired = true;
                }
                else if (positional[i].IsRequired && foundNonRequired)
                {
                    throw new Exception($"Required positional options must come before non required ones ({positional[i - 1].Name}, {positional[i].Name}).");
                }
            }

            var named = properties
                .Where(prop => prop.GetCustomAttribute<NamedOptionAttribute>() is not null)
                .Select(prop => new NamedCommandOption(prop))
                .ToArray();

            HashSet<char> shortNameOptions = [];
            HashSet<string> longNameOptions = [];

            for (int i = 0; i < named.Length; i++)
            {
                var option = named[i];

                if (option.ShortName is not null && !shortNameOptions.Add(option.ShortName.Value))
                {
                    throw new DuplicateOptionException(option.ShortName.Value.ToString());
                }

                if (option.LongName is not null && !longNameOptions.Add(option.LongName))
                {
                    throw new DuplicateOptionException(option.LongName);
                }
            }

            return
            (
                positional,
                named
            );
        }

        internal static void ValidateType(Type commandType)
        {
            if (!IsCommand(commandType))
            {
                throw new ArgumentException($"Type '{commandType}' doesn't extend {typeof(ConsoleCommand)} class.", nameof(commandType));
            }
        }

        internal static bool IsCommand(Type type)
            => typeof(ConsoleCommand).IsAssignableFrom(type);
    }
}
