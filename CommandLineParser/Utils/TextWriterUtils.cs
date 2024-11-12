using System.Buffers;

namespace CommandLineParser.Utils;

internal static class TextWriterUtils
{
    public static void WriteSpaces(this TextWriter writer, int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);

        if (length == 0)
        {
            return;
        }

        char[] buffer = ArrayPool<char>.Shared.Rent(length);
        Array.Fill(buffer, ' ');

        writer.Write(buffer, 0, length);

        ArrayPool<char>.Shared.Return(buffer);
    }

    public static void Write2D(this TextWriter writer, ReadOnlySpan<string> span, int writerWidth, int padding)
    {
        const int MinSecondColumnLength = 8;

        writerWidth--; // leave space for newline character

        if (span.Length % 2 != 0)
        {
            throw new ArgumentException($"{nameof(span)} must be divisible by 2.", nameof(span));
        }
        else if ((padding * 2) + 2 > writerWidth)
        {
            throw new ArgumentOutOfRangeException($"({nameof(padding)} * 2) + 2 must be smaller than {nameof(writerWidth)}.", nameof(padding));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(writerWidth, 2);

        int firstColumnMaxLen = 0;

        for (int i = 0; i < span.Length; i += 2)
        {
            firstColumnMaxLen = Math.Max(firstColumnMaxLen, span[i].Length);
        }

        if (firstColumnMaxLen + (padding * 2) + MinSecondColumnLength >= writerWidth)
        {
            Write2DInternalShortWidth(writer, span, writerWidth, padding);
            return;
        }

        Span<char> padString1 = stackalloc char[padding];
        padString1.Fill(' ');

        Span<char> padString2 = stackalloc char[(padding * 2) + firstColumnMaxLen];
        padString2.Fill(' ');

        int secondColumnMaxLen = writerWidth - (firstColumnMaxLen + (padding * 2));

        for (int i = 0; i < span.Length; i += 2)
        {
            writer.Write(padString1);
            writer.Write(span[i]);

            if (string.IsNullOrEmpty(span[i + 1]))
            {
                writer.WriteLine();
                continue;
            }

            writer.WriteSpaces(padding + (firstColumnMaxLen - span[i].Length));

            ReadOnlySpan<char> secondColumnText = span[i + 1];

            if (secondColumnText.Length + padString2.Length < writerWidth)
            {
                writer.WriteLine(secondColumnText);
                continue;
            }

            var split = StringUtils.SplitByMaxLength(secondColumnText, secondColumnMaxLen);

            writer.WriteLine(secondColumnText[split[0]]);

            for (int splitIndex = 1; splitIndex < split.Count; splitIndex++)
            {
                writer.Write(padString2);

                writer.WriteLine(secondColumnText[split[splitIndex]]);
            }
        }
    }

    private static void Write2DInternalShortWidth(TextWriter writer, ReadOnlySpan<string> span, int writerWidth, int padding)
    {
        int widthWithoutPadding = writerWidth - (padding * 2);

        int column1Length = widthWithoutPadding / 2;
        int column2Length = widthWithoutPadding - column1Length; // if widthWithoutPadding isn't divisible by 2, column2Length will be longer than column1Length

        Span<char> padString1 = stackalloc char[padding];
        padString1.Fill(' ');

        Span<char> padString2 = stackalloc char[(padding * 2) + column1Length];
        padString2.Fill(' ');

        for (int i = 0; i < span.Length; i += 2)
        {
            ReadOnlySpan<char> column1 = span[i + 0];
            ReadOnlySpan<char> column2 = span[i + 1];

            var split1 = StringUtils.SplitByMaxLength(column1, column1Length);
            var split2 = StringUtils.SplitByMaxLength(column2, column2Length);

            for (int j = 0; j < Math.Max(split1.Count, split2.Count); j++)
            {
                if (j < split1.Count)
                {
                    writer.Write(padString1);

                    writer.Write(column1[split1[j]]);

                    writer.WriteSpaces(column1Length + padding - column1[split1[j]].Length);
                }
                else if (j < split2.Count)
                {
                    writer.Write(padString2);
                }

                if (j < split2.Count)
                {
                    writer.WriteLine(column2[split2[j]]);
                }
                else
                {
                    writer.WriteLine();
                }
            }
        }
    }
}
