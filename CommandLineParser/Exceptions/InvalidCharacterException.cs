using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandLineParser.Exceptions
{
    public sealed class InvalidCharacterException : ParseException
    {
        public InvalidCharacterException(int position, char character, string? message)
            : base(position, $"Encountered invalid character '{character}'{(string.IsNullOrEmpty(message) ? string.Empty : ": " + message)}.")
        {
        }
    }
}
