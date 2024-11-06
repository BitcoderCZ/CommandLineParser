using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class RequiredOptionNotAssignedException : Exception
    {
        public RequiredOptionNotAssignedException(string optionName)
            : base($"Required option '{optionName}' wasn't assigned.")
        {
        }
    }
}
