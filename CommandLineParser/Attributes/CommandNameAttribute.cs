using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CommandNameAttribute : Attribute
    {
        public CommandNameAttribute(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (name.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException($"{nameof(name)} cannot contain whitespace.", nameof(name));
            }

            Name = name;
        }

        public string Name { get; }
    }
}
