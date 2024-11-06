using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class NoCommandSpecified : Exception
    {
        public NoCommandSpecified()
            : base("No command was specified.")
        {
        }
    }
}
