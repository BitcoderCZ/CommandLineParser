using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class CommandNotFoundException : Exception
    {
        public CommandNotFoundException(string commandName)
            : base($"Command \"{commandName}\" doesn't exist.")
        {
        }
    }
}
