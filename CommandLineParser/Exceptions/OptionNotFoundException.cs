using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class OptionNotFoundException : Exception
    {
        public OptionNotFoundException(string optionName, string commandName)
            : base($"Option '{optionName}' isn't defined by command '{commandName}'.")
        {
        }
    }
}
