using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MeshTopologyToolkit
{
    public ref struct SpanTokenizer
    {
        static readonly char[] WhitespaceValues = new[] { ' ', '\t', '\n', '\r' };

        // The internal span representing the remainder of the input text.
        private ReadOnlySpan<char> _remainingText;

        /// <summary>
        /// Initializes the tokenizer with a span, converting it to a Span.
        /// </summary>
        public SpanTokenizer(ReadOnlySpan<char> text)
        {
            _remainingText = text;
        }


        /// <summary>
        /// Initializes the tokenizer with an utf8 byte array, converting it to a Span.
        /// </summary>
        public SpanTokenizer(byte[] text)
        {
            _remainingText = new UTF8Encoding(false).GetChars(text);
        }

        /// <summary>
        /// Initializes the tokenizer with a string, converting it to a Span.
        /// </summary>
        public SpanTokenizer(string text)
        {
            _remainingText = text.AsSpan();
        }

        /// <summary>
        /// Initializes the tokenizer with a char array, converting it to a Span.
        /// </summary>
        public SpanTokenizer(char[] text)
        {
            _remainingText = text.AsSpan();
        }

        /// <summary>
        /// Initializes the tokenizer with a utf8 stream, converting it to a Span.
        /// </summary>
        public SpanTokenizer(Stream text)
        {
            _remainingText = new StreamReader(text, new UTF8Encoding(false)).ReadToEnd().AsSpan();
        }

        /// <summary>
        /// Reports whether the tokenizer has any characters left to process.
        /// </summary>
        public bool IsEndOfStream => _remainingText.IsEmpty;

        /// <summary>
        /// Advances the internal pointer past any leading whitespace characters.
        /// </summary>
        public void ConsumeWhitespace()
        {
            _remainingText = _remainingText.TrimStart();
        }

        /// <summary>
        /// Attempts to read the next single-word token (delimited by whitespace or end of stream).
        /// </summary>
        /// <param name="token">The read-only span view of the token.</param>
        /// <returns>True if a token was successfully read; otherwise, false.</returns>
        public bool TryReadWordToken(out ReadOnlySpan<char> token)
        {
            ConsumeWhitespace();

            if (_remainingText.IsEmpty)
            {
                token = default;
                return false;
            }

            // Find the index of the first whitespace character
            int delimiterIndex = _remainingText.IndexOfAny(WhitespaceValues);
            
            if (delimiterIndex < 0)
            {
                // If no delimiter, the rest of the span is the token
                token = _remainingText;
                _remainingText = default; // Clear the remaining text
            }
            else
            {
                // Token found, slice it out
                token = _remainingText.Slice(0, delimiterIndex);
                _remainingText = _remainingText.Slice(delimiterIndex); // Advance the pointer
            }
            return true;
        }


        /// <summary>
        /// Fetches the next token as a float (Single) number.
        /// </summary>
        /// <param name="result">The parsed float value.</param>
        /// <returns>True if a valid float token was read and parsed; otherwise, false.</returns>
        public bool TryReadFloat(out float result)
        {
            result = 0.0f;

            ConsumeWhitespace();
            if (_remainingText.IsEmpty)
            {
                return false;
            }

            Func<char,bool> currentState = (char c)=> false;
            var expDidgits = (char c) =>
            {
                if (char.IsDigit(c))
                {
                    return true;
                }
                return false;
            };
            var expStart = (char c) =>
            {
                if (c == '+' || c == '-')
                {
                    currentState = expDidgits;
                    return true;
                }
                if (char.IsDigit(c))
                {
                    currentState = expDidgits;
                    return true;
                }
                return false;
            };
            var dotDidgits = (char c) =>
            {
                if (c == 'e' || c == 'E')
                {
                    currentState = expStart;
                    return true;
                }
                if (char.IsDigit(c))
                {
                    return true;
                }
                return false;
            };
            var intDidgits = (char c) =>
            {
                if (c == '.')
                {
                    currentState = dotDidgits;
                    return true;
                }
                if (c == 'e' || c == 'E')
                {
                    currentState = expStart;
                    return true;
                }
                if (char.IsDigit(c))
                {
                    return true;
                }
                return false;
            };
            var startOfValue = (char c) =>{
                if (c == '+' || c == '-')
                {
                    currentState = intDidgits;
                    return true;
                }
                if (c == '.')
                {
                    currentState = dotDidgits;
                    return true;
                }
                if (char.IsDigit(c))
                {
                    currentState = intDidgits;
                    return true;
                }
                return false;
            };

            currentState = startOfValue;

            int endOfValue = 0;
            while (endOfValue < _remainingText.Length)
            {
                if (!currentState(_remainingText[endOfValue]))
                    break;
                ++endOfValue;
            }

            if (endOfValue > 0)
            {
                var token = _remainingText.Slice(0, endOfValue);
                _remainingText = _remainingText.Slice(endOfValue);

                // Use float.TryParse on the span (Parse overload that takes ReadOnlySpan<char>)
                // Use InvariantCulture for consistent decimal point parsing across environments
                if (float.TryParse(
                    token,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out result))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Fetches the next token as a integer number.
        /// </summary>
        /// <param name="result">The parsed integer value.</param>
        /// <returns>True if a valid int token was read and parsed; otherwise, false.</returns>
        public bool TryReadInt(out int result)
        {
            result = 0;

            ConsumeWhitespace();
            if (_remainingText.IsEmpty)
            {
                return false;
            }

            Func<char, bool> currentState = (char c) => false;
            
            var intDidgits = (char c) =>
            {
                if (char.IsDigit(c))
                {
                    return true;
                }
                return false;
            };
            var startOfValue = (char c) => {
                if (c == '+' || c == '-')
                {
                    currentState = intDidgits;
                    return true;
                }
                if (char.IsDigit(c))
                {
                    currentState = intDidgits;
                    return true;
                }
                return false;
            };

            currentState = startOfValue;

            int endOfValue = 0;
            while (endOfValue < _remainingText.Length)
            {
                if (!currentState(_remainingText[endOfValue]))
                    break;
                ++endOfValue;
            }

            if (endOfValue > 0)
            {
                var token = _remainingText.Slice(0, endOfValue);
                _remainingText = _remainingText.Slice(endOfValue);

                // Use float.TryParse on the span (Parse overload that takes ReadOnlySpan<char>)
                // Use InvariantCulture for consistent decimal point parsing across environments
                if (int.TryParse(
                    token,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Expect(string prefix, StringComparison stringComparison = StringComparison.Ordinal)
        {
            ConsumeWhitespace();
            if (_remainingText.IsEmpty)
            {
                return prefix.Length != 0;
            }

            if (_remainingText.StartsWith(prefix, stringComparison))
            {
                _remainingText = _remainingText.Slice(prefix.Length);
                return true;
            }

            return false;
        }

        public void ConsumeToEndOfLine()
        {
            int endOfValue = 0;
            while (endOfValue < _remainingText.Length)
            {
                var c = _remainingText[endOfValue];
                if (c == 10)
                {
                    ++ endOfValue;
                    if (endOfValue < _remainingText.Length && _remainingText[endOfValue] == 13)
                        ++endOfValue;
                    break;
                }
                if (c == 13)
                {
                    ++endOfValue;
                    if (endOfValue < _remainingText.Length && _remainingText[endOfValue] == 10)
                        ++endOfValue;
                    break;
                }
                ++endOfValue;
            }
            _remainingText = _remainingText.Slice(endOfValue);
        }
    }
}
