using System.Reflection;
using CommandLineParser.Attributes;
using CommandLineParser.Exceptions;

namespace CommandLineParser
{
    internal sealed class NamedCommandOption : CommandOption
    {
        private readonly NamedOptionAttribute _optionAttrib;
        private readonly RequiredAttribute? _requiredAttrib;
        private readonly DependsOnAttribute[] _dependsOnAttribs;

        public NamedCommandOption(PropertyInfo prop)
            : base(prop)
        {
            var attribs = prop.GetCustomAttributes().ToArray();
            _optionAttrib = attribs.OfType<NamedOptionAttribute>().FirstOrDefault()
                ?? throw new MissingAttributeException(prop.Name, typeof(NamedOptionAttribute));

            if (prop.GetCustomAttribute<PositionalOptionAttribute>() is not null)
            {
                throw new ArgumentException($"{nameof(prop)} cannot have both {nameof(NamedOptionAttribute)} and {nameof(PositionalOptionAttribute)}.", nameof(prop));
            }

            _requiredAttrib = attribs.OfType<RequiredAttribute>().FirstOrDefault();
            _dependsOnAttribs = attribs.OfType<DependsOnAttribute>().ToArray();
        }

        public override Type Type => _prop.PropertyType;

        public override string PropName => _prop.Name;

        public char? ShortName => _optionAttrib.ShortName;

        public string? LongName => _optionAttrib.LongName;

        public override bool IsRequired => _requiredAttrib is not null;

        public bool DependsOnAnotherOption => _dependsOnAttribs.Length > 0;

        public override object? GetValue(ConsoleCommand instance)
            => _prop.GetGetMethod()!.Invoke(instance, []);

        public override void SetValue(ConsoleCommand instance, object value)
            => _prop.GetSetMethod()!.Invoke(instance, [value]);

        public override string GetNames()
            => ShortName is null
                ? "--" + LongName
                : LongName is null
                ? "-" + ShortName.Value
                : "-" + ShortName.Value + ", --" + LongName;

        public IEnumerable<(string Name, object? Value)> GetDependencies()
            => _dependsOnAttribs.Select(attrib => (attrib.PropertyName, attrib.PropertyValue));
    }
}
