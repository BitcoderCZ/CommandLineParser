using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class InvalidOptionAssignedException : Exception
    {
        public InvalidOptionAssignedException(string optionName)
            : base($"Option '{optionName}' cannot be assigned because not all options it depends on have the specified value.")
        {
        }
    }
}
