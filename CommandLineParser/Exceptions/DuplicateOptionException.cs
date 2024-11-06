using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class DuplicateOptionException : Exception
    {
        public DuplicateOptionException(string optionName)
            : base($"Option with the name '{optionName}' is defined multiple times.")
        {
        }
    }
}
