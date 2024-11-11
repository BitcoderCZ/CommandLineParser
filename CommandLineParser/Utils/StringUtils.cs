using System.Text;
using CommandLineParser.Exceptions;

namespace CommandLineParser.Utils;

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
                        throw new InvalidEscapeSequenceException();
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

    public static IEnumerable<Range> SplitByMaxLength(string str, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxLength, 0);

        if (maxLength == 1)
        {
            for (int i = 0; i < str.Length; i++)
            {
                yield return new Range(i, i + 1);
            }
        }

        int index = 0;

        List<Range> currentLine = new List<Range>(maxLength / 2);
        int currentLineLength = 0;

        while (index < str.Length)
        {
            int spaceIndex = str.IndexOf(' ', index);

            if (spaceIndex == -1)
            {
                spaceIndex = str.Length;
            }

            Range wordRange = new Range(index, spaceIndex);
            int wordLength = spaceIndex - index;
            if (wordLength == 0)
            {
                goto setIndex;
            }

            // + 1 -> the space between the words and the current word
            if (currentLineLength + wordLength + 1 > maxLength)
            {
                yield return CurrentLineToRange();
                currentLine.Clear();
                currentLineLength = 0;
            }

            if (wordLength >= maxLength)
            {
                for (int i = 0; i < wordLength; i += maxLength)
                {
                    int lengthToAdd = Math.Min(wordLength - i, maxLength);
                    if (lengthToAdd == 0)
                    {
                        break;
                    }

                    currentLine.Add(new Range(index + i, index + i + lengthToAdd));
                    AddLength(lengthToAdd);

                    if (currentLineLength >= maxLength)
                    {
                        yield return CurrentLineToRange();
                        currentLine.Clear();
                        currentLineLength = 0;
                    }
                }
            }
            else
            {
                currentLine.Add(wordRange);
                AddLength(wordLength);
            }

        setIndex:
            index = spaceIndex + 1;
        }

        if (currentLineLength > 0)
        {
            yield return CurrentLineToRange();
        }

        void AddLength(int length)
        {
            if (currentLineLength == 0)
            {
                currentLineLength = length;
            }
            else
            {
                currentLineLength += length + 1;
            }
        }

        Range CurrentLineToRange()
        {
            return currentLine.Count > 0
                ? new Range(currentLine[0].Start, currentLine[^1].End)
                : throw new InvalidOperationException($"{nameof(currentLine)} cannot be empty.");
        }
    }

    public static List<Range> SplitByMaxLength(ReadOnlySpan<char> str, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxLength, 0);

        List<Range> ranges;

        if (maxLength == 1)
        {
            ranges = new List<Range>(str.Length);

            for (int i = 0; i < str.Length; i++)
            {
                ranges.Add(new Range(i, i + 1));
            }

            return ranges;
        }

        ranges = [];

        int index = 0;

        List<Range> currentLine = new List<Range>(maxLength / 2);
        int currentLineLength = 0;

        while (index < str.Length)
        {
            int spaceIndex = str[index..].IndexOf(' ') + index;

            if (spaceIndex - index == -1)
            {
                spaceIndex = str.Length;
            }

            Range wordRange = new Range(index, spaceIndex);
            int wordLength = spaceIndex - index;
            if (wordLength == 0)
            {
                goto setIndex;
            }

            // + 1 -> the space between the words and the current word
            if (currentLineLength + wordLength + 1 > maxLength)
            {
                CurrentLineToRange();
            }

            if (wordLength >= maxLength)
            {
                for (int i = 0; i < wordLength; i += maxLength)
                {
                    int lengthToAdd = Math.Min(wordLength - i, maxLength);
                    if (lengthToAdd == 0)
                    {
                        break;
                    }

                    currentLine.Add(new Range(index + i, index + i + lengthToAdd));
                    AddLength(lengthToAdd);

                    if (currentLineLength >= maxLength)
                    {
                        CurrentLineToRange();
                    }
                }
            }
            else
            {
                currentLine.Add(wordRange);
                AddLength(wordLength);
            }

        setIndex:
            index = spaceIndex + 1;
        }

        if (currentLineLength > 0)
        {
            CurrentLineToRange();
        }

        return ranges;

        void AddLength(int length)
        {
            if (currentLineLength == 0)
            {
                currentLineLength = length;
            }
            else
            {
                currentLineLength += length + 1;
            }
        }

        void CurrentLineToRange()
        {
            if (currentLine.Count == 0)
            {
                return;
            }

            ranges.Add(new Range(currentLine[0].Start, currentLine[^1].End));

            currentLine.Clear();
            currentLineLength = 0;
        }
    }

    // https://stackoverflow.com/a/50068838/15878562
    public static string JoinAnd<T>(IEnumerable<T?> values, in string separator = ", ", in string lastSeparator = " and ")
        => JoinAnd(values, new StringBuilder(), separator, lastSeparator).ToString();

    public static StringBuilder JoinAnd<T>(IEnumerable<T?> values, StringBuilder sb, in string separator = ", ", in string lastSeparator = ", and ")
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(separator);
        ArgumentNullException.ThrowIfNull(lastSeparator);

        using var enumerator = values.GetEnumerator();

        // add first item without separator
        if (enumerator.MoveNext())
        {
            sb.Append(enumerator.Current);
        }

        var nextItem = (hasValue: false, item: default(T?));

        // see if there is a next item
        if (enumerator.MoveNext())
        {
            nextItem = (true, enumerator.Current);
        }

        // while there is a next item, add separator and current item
        while (enumerator.MoveNext())
        {
            sb.Append(separator);
            sb.Append(nextItem.item);
            nextItem = (true, enumerator.Current);
        }

        // add last separator and last item
        if (nextItem.hasValue)
        {
            sb.Append(lastSeparator ?? separator);
            sb.Append(nextItem.item);
        }

        return sb;
    }
}
