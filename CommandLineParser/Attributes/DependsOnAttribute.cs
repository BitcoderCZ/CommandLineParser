using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Attributes
{
    /// <summary>
    /// The option this attribute is applied to will only be assignable if a property with the name <see cref="PropertyName"/> has the value <see cref="PropertyValue"/>.
    /// If added multiple times, all of the properties must have the specified value.
    /// Can be combined with <see cref="RequiredAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class DependsOnAttribute : Attribute
    {
        public DependsOnAttribute(string propertyName, object? propertyValue)
        {
            PropertyName = propertyName;
            PropertyValue = propertyValue;
        }

        public string PropertyName { get; }

        public object? PropertyValue { get; }
    }
}
