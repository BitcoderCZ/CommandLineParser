using System.Reflection;

namespace CommandLineParser
{
    internal abstract class CommandOption
    {
        protected readonly PropertyInfo _prop;

        protected CommandOption(PropertyInfo prop)
        {
            if (!(prop.CanRead && prop.CanWrite && (prop.GetGetMethod(true)?.IsPublic ?? false) && (prop.GetSetMethod(true)?.IsPublic ?? false)))
            {
                throw new ArgumentException($"{nameof(prop)} must have a public getter and setter.", nameof(prop));
            }

            _prop = prop;
        }

        public abstract Type Type { get; }

        public abstract string PropName { get; }

        public abstract bool IsRequired { get; }

        public abstract object? GetValue(ConsoleCommand instance);

        public abstract void SetValue(ConsoleCommand instance, object value);

        public abstract string GetNames();
    }
}
