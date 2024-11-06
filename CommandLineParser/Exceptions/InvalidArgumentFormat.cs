using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class InvalidArgumentFormat : Exception
    {
        public InvalidArgumentFormat(string argName)
            : base($"Argument \"{argName}\" isn't correctly formated.")
        {  
        }
    }
}
