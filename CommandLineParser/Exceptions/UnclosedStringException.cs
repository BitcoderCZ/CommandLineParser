using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class UnclosedStringException : ParseException
    {
        public UnclosedStringException(int position)
            : base(position, "Unclosed string.")
        {
        }
    }
}
