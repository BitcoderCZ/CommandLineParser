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
        private readonly DependsOnAttribute[] _dependsOnAttribs;

        public CommandOption(PropertyInfo prop)
        {
            Debug.Assert(prop.CanRead && prop.CanWrite && (prop.GetGetMethod(true)?.IsPublic ?? false) && (prop.GetSetMethod(true)?.IsPublic ?? false), $"{nameof(prop)} must have a public getter and setter.");

            _prop = prop;

            var attribs = prop.GetCustomAttributes().ToArray();
            _nameAttrib = attribs.OfType<OptionNameAttribute>().FirstOrDefault()
                ?? throw new MissingAttributeException(prop.Name, typeof(OptionNameAttribute));

            _requiredAttrib = attribs.OfType<RequiredAttribute>().FirstOrDefault();
            _dependsOnAttribs = attribs.OfType<DependsOnAttribute>().ToArray();
        }

        public Type Type => _prop.PropertyType;

        public string PropName => _prop.Name;

        public char? ShortName => _nameAttrib.ShortName;

        public string? LongName => _nameAttrib.LongName;

        public bool IsRequired => _requiredAttrib is not null;

        public bool DependsOnAnotherOption => _dependsOnAttribs.Length > 0;

        public object? GetValue(ConsoleCommand instance)
            => _prop.GetGetMethod()!.Invoke(instance, []);

        public void SetValue(ConsoleCommand instance, object value)
            => _prop.GetSetMethod()!.Invoke(instance, [value]);

        public string GetNames()
            => ShortName is null
                ? "--" + LongName
                : LongName is null
                ? "-" + ShortName.Value
                : "-" + ShortName.Value + ", --" + LongName;

        public IEnumerable<(string Name, object? Value)> GetDependencies()
            => _dependsOnAttribs.Select(attrib => (attrib.PropertyName, attrib.PropertyValue));
    }
}
