using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLineParser.Attributes;
using CommandLineParser.Exceptions;

namespace CommandLineParser
{
    internal sealed class CommandOption
    {
        private readonly PropertyInfo _prop;
        private readonly OptionNameAttribute _nameAttrib;
        private readonly RequiredAttribute? _requiredAttrib;

        public CommandOption(PropertyInfo prop)
        {
            Debug.Assert(prop.CanWrite && (prop.GetSetMethod(true)?.IsPublic ?? false), $"{nameof(prop)} must have a setter and the setter must be public.");

            _prop = prop;

            var attribs = prop.GetCustomAttributes().ToArray();
            _nameAttrib = attribs.OfType<OptionNameAttribute>().FirstOrDefault()
                ?? throw new MissingAttributeException(prop.Name, typeof(OptionNameAttribute));

            _requiredAttrib = attribs.OfType<RequiredAttribute>().FirstOrDefault();
        }

        public char? ShortName => _nameAttrib.ShortName;

        public string? LongName => _nameAttrib.LongName;

        public bool IsRequired => _requiredAttrib is not null;
    }
}
