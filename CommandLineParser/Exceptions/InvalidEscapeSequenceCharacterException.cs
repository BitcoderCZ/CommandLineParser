using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class InvalidEscapeSequenceCharacterException : ParseException
    {
        public InvalidEscapeSequenceCharacterException(int position, char character)
            : base(position, $"No escape sequence is defined for the character '{character}'.")
        {
        }
    }
}
