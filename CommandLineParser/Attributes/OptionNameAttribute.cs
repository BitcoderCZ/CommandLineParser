using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Attributes
{
    /// <summary>
    /// Specifies the name(s) of an option.
    /// Required to make a property an option.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class OptionNameAttribute : Attribute
    {
        public OptionNameAttribute(char shortName)
        {
            ShortName = shortName;
        }

        public OptionNameAttribute(string longName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(longName);

            longName = longName.Trim();

            if (longName.Length < 1)
            {
                throw new ArgumentException($"{nameof(longName)} must be longer than 1 character.", nameof(longName));
            }

            LongName = longName;
        }

        public OptionNameAttribute(char shortName, string longName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(longName);

            longName = longName.Trim();

            if (longName.Length < 1)
            {
                throw new ArgumentException($"{nameof(longName)} must be longer than 1 character.", nameof(longName));
            }

            ShortName = shortName;

            LongName = longName;
        }

        public char? ShortName { get; }

        public string? LongName { get; }
    }
}
