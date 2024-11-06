using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class MissingAttributeException : Exception
    {
        public MissingAttributeException(string memberName, Type attributeType)
            : base($"Member '{memberName}' is missing required attribute '{attributeType}'.")
        {
            if (!typeof(Attribute).IsAssignableFrom(attributeType))
            {
                throw new ArgumentException($"{nameof(attributeType)} must be an attribute.", nameof(attributeType));
            }
        }
    }
}
