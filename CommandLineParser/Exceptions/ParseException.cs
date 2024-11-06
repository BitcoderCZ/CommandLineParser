using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    /// <summary>
    /// Base class for parse exceptions.
    /// </summary>
    public abstract class ParseException : Exception
    {
        protected ParseException(int position, string? message)
            : base($"A parse exception occured at {position}: " + message)
        { 
        }
    }
}
