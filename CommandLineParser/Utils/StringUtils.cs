using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLineParser.Exceptions;

namespace CommandLineParser.Utils
{
    internal static class StringUtils
    {
        public static ReadOnlySpan<char> ParseEscapeSequences(ReadOnlySpan<char> text, int? numbSequences = null)
        {
            if (numbSequences is null)
            {
                numbSequences = 0;

                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\\')
                    {
                        if (i + 1 >= text.Length)
                        {
                            throw new InvalidEscapeSequenceException(i);
                        }

                        numbSequences++;
                        i++;
                    }
                }
            }

            ArgumentOutOfRangeException.ThrowIfLessThan(numbSequences.Value, 0);

            char[] escapedBuffer = new char[text.Length - numbSequences.Value];

            int escapedIndex = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\\')
                {
                    // don't need to bound check here because all \ must be followed by a character
                    switch (text[++i])
                    {
                        case '\\':
                            escapedBuffer[escapedIndex] = '\\';
                            break;
                        case '"':
                            escapedBuffer[escapedIndex] = '\"';
                            break;
                        case 'n':
                            escapedBuffer[escapedIndex] = '\n';
                            break;
                        case 'r':
                            escapedBuffer[escapedIndex] = '\r';
                            break;
                        case 't':
                            escapedBuffer[escapedIndex] = '\t';
                            break;
                        case 'v':
                            escapedBuffer[escapedIndex] = '\v';
                            break;
                        case 'b':
                            escapedBuffer[escapedIndex] = '\b';
                            break;
                    }
                }
                else
                {
                    escapedBuffer[escapedIndex] = text[i];
                }

                escapedIndex++;
            }

            return escapedBuffer;
        }

        // UUID - uuid, StringUtils - string-utils
        public static string ConvertToCliName(ReadOnlySpan<char> text)
        {
            StringBuilder builder = new();

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                char lowerChar = char.ToLowerInvariant(c);

                if (char.IsWhiteSpace(c))
                {
                    builder.Append('-');
                }
                else if (char.IsUpper(c) && i != 0 && i + 1 < text.Length && !char.IsUpper(text[i + 1]))
                {
                    builder.Append('-');
                    builder.Append(lowerChar);
                }
                else
                {
                    builder.Append(lowerChar);
                }
            }

            return builder.ToString();
        }
    }
}
