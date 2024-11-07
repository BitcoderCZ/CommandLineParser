using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class InvalidOptionDepencyException : Exception
    {
        public InvalidOptionDepencyException(string optionName, string propertyName)
            : base($"Property '{propertyName}', which option {optionName} depends on, doesn't exist.")
        {
        }
    }
}
