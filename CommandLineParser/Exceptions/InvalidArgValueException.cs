using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class InvalidArgValueException : Exception
    {
        public InvalidArgValueException(string optionName, string message)
            : base($"Option '{optionName}' isn't formated correctly: " + message)
        {
        }
    }
}
