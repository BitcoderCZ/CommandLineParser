using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class InvalidEscapeSequenceException : ParseException
    {
        public InvalidEscapeSequenceException(int position)
            : base(position, "Another non-whitespace character must follow a '\\', to enter a '\\' type 2 after eachother: \\\\.")
        {
        }
    }
}
