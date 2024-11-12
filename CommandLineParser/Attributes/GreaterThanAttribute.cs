using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Attributes;

/// <summary>
/// The option this attribute is applied to can only be assigned values greater than <see cref="Value"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class GreaterThanAttribute : Attribute
{
    public GreaterThanAttribute(object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value is not IComparable)
        {
            throw new ArgumentException($"{nameof(value)} must be {nameof(IComparable)}.", nameof(value));
        }

        Value = value;
    }

    public object Value { get; }
}
