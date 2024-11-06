using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLineParser.Exceptions;
using CommandLineParser.Utils;

namespace CommandLineParser
{
    public ref struct CommandReader
    {
        private readonly ReadOnlySpan<char> _buffer;
        private int _position;

        public CommandReader(ReadOnlySpan<char> buffer)
        {
            _buffer = buffer;
        }

        public enum TokenKind
        {
            WhiteSpace,
            Dash,
            DashDash,
            Equals,
            QuotedText,
            Text,
            EndOfText,
        }

        public readonly int Position => _position;

        public readonly int CharsLeft => _buffer.Length - _position;

        public readonly char CurrentChar => _buffer[_position];

        public readonly TokenKind CurrentKind => Peek(0);

        public readonly string GetBufferAsString()
            => new string(_buffer);

        public readonly TokenKind Peek(int offset)
        {
            int pos = _position + offset;

            return pos >= _buffer.Length
                ? TokenKind.EndOfText
                : _buffer[pos] switch
                {
                    '-' when pos < _buffer.Length - 1 && _buffer[pos + 1] == '-' => TokenKind.DashDash,
                    '-' => TokenKind.Dash,
                    '=' => TokenKind.Equals,
                    '"' => TokenKind.QuotedText,
                    _ => TokenKind.Text,
                };
        }

        public ReadOnlySpan<char> Read(int length)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(length, 0);
            if (_position + length > _buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            int pos = _position;
            _position += length;

            return _buffer.Slice(pos, length);
        }

        public ReadOnlySpan<char> ReadCurrent()
            => CurrentKind switch
            {
                TokenKind.EndOfText => [],
                TokenKind.Dash => Read(1),
                TokenKind.DashDash => Read(2),
                TokenKind.Equals => Read(1),
                TokenKind.WhiteSpace => ReadWhitespace(),
                _ => ReadText(),
            };

        public ReadOnlySpan<char> ReadWhitespace()
        {
            if (CurrentKind != TokenKind.WhiteSpace)
            {
                return [];
            }

            int startPos = _position++;

            while (_position < _buffer.Length && char.IsWhiteSpace(_buffer[_position]))
            {
                _position++;
            }

            return _buffer[startPos.._position];
        }

        public ReadOnlySpan<char> ReadText()
        {
            TokenKind currentKind = CurrentKind;

            if (currentKind != TokenKind.Text && currentKind != TokenKind.QuotedText)
            {
                throw new InvalidOperationException($"{nameof(CurrentKind)} must be {nameof(TokenKind.Text)} or {nameof(TokenKind.QuotedText)}.");
            }

            bool startedWithQuote = currentKind == TokenKind.QuotedText;
            int startPos = startedWithQuote ? ++_position : _position;
            int escapedCharsCount = 0;

            while (_position < _buffer.Length)
            {
                if (!startedWithQuote && char.IsWhiteSpace(_buffer[_position]))
                {
                    break;
                }
                else if (_buffer[_position] == '"')
                {
                    if (startedWithQuote)
                    {
                        break;
                    }
                    else
                    {
                        throw new InvalidCharacterException(_position, '"', $"'\"' cannot be used in text that isn't enclosed in '\"', to enter a '\"' enclose the text in '\"': \"\\\"\".");
                    }
                }
                else if (_buffer[_position] == '\\')
                {
                    escapedCharsCount++;
                    _position++;
                    if (_position >= _buffer.Length)
                    {
                        throw new InvalidEscapeSequenceException(_position - 1);
                    }
                }

                _position++;
            }

            return startedWithQuote && _position >= _buffer.Length
                ? throw new UnclosedStringException(_buffer.Length - 1)
                : escapedCharsCount == 0
                ? _buffer[startPos..(startedWithQuote ? _position++ : _position)]
                : StringUtils.ParseEscapeSequences(_buffer[startPos..(startedWithQuote ? _position++ : _position)], escapedCharsCount);
        }
    }
}
