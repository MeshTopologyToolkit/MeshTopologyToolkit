using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MeshTopologyToolkit.Fbx
{
    /// <summary>
    /// Parses ASCII FBX files (both the 6.x and 7.x flavours) into an <see cref="FbxDocument"/>.
    /// Array declarations like <c>Vertices: *9 { a: 1,2,3 }</c> are collapsed into a single array property.
    /// </summary>
    public static class FbxAsciiReader
    {
        private const int MinArrayRun = 16;

        public static FbxDocument Parse(string text)
        {
            var doc = new FbxDocument { IsBinary = false, Version = 0 };
            var p = new Parser(text);
            doc.Version = p.HeaderVersion;
            while (true)
            {
                var node = p.ParseNode(0);
                if (node == null)
                    break;
                doc.Roots.Add(node);
            }
            if (doc.Version == 0)
                doc.Version = doc.Child("FBXVersion")?.GetInt(0) ?? 0;
            if (doc.Version == 0)
                doc.Version = doc.Child("FBXHeaderExtension")?.Child("FBXVersion")?.GetInt(0) ?? 7400;
            return doc;
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _pos;

            public int HeaderVersion { get; private set; }

            public Parser(string s)
            {
                _s = s;
                // Skip BOM.
                if (_s.Length > 0 && _s[0] == '﻿')
                    _pos = 1;
            }

            private bool SkipTrivia()
            {
                while (_pos < _s.Length)
                {
                    char c = _s[_pos];
                    if (c == ';')
                    {
                        int start = _pos;
                        while (_pos < _s.Length && _s[_pos] != '\n')
                            ++_pos;
                        if (HeaderVersion == 0)
                            TryParseHeaderComment(_s.Substring(start, _pos - start));
                    }
                    else if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    {
                        ++_pos;
                    }
                    else
                    {
                        return true;
                    }
                }
                return false;
            }

            private void TryParseHeaderComment(string comment)
            {
                // "; FBX 7.4.0 project file"
                var idx = comment.IndexOf("FBX ", StringComparison.Ordinal);
                if (idx < 0)
                    return;
                var parts = comment.Substring(idx + 4).Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3
                    && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var major)
                    && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor)
                    && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var patch))
                {
                    HeaderVersion = major * 1000 + minor * 100 + patch;
                }
            }

            private static bool IsIdentChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.' || c == ':';

            /// <summary>Parses a node, returns null at the end of the current block (consuming the closing brace) or of the file.</summary>
            public FbxNode? ParseNode(int depth)
            {
                if (depth > 512)
                    throw new FormatException("FBX node nesting is too deep.");
                if (!SkipTrivia())
                    return null;

                if (_s[_pos] == '}')
                {
                    ++_pos;
                    return null;
                }

                int start = _pos;
                while (_pos < _s.Length && (char.IsLetterOrDigit(_s[_pos]) || _s[_pos] == '_'))
                    ++_pos;
                if (_pos == start || _pos >= _s.Length || _s[_pos] != ':')
                    throw new FormatException($"Expected node name at offset {start}.");
                var node = new FbxNode(_s.Substring(start, _pos - start));
                ++_pos; // ':'

                bool arrayDeclaration = ParseValues(node);

                if (SkipTrivia() && _s[_pos] == '{')
                {
                    ++_pos;
                    while (true)
                    {
                        var child = ParseNode(depth + 1);
                        if (child == null)
                            break;
                        node.Children.Add(child);
                    }
                    if (arrayDeclaration && node.Children.Count == 1 && node.Children[0].Name == "a")
                    {
                        node.Properties.Clear();
                        node.Properties.AddRange(node.Children[0].Properties);
                        node.Children.Clear();
                    }
                }
                return node;
            }

            private readonly List<double> _run = new List<double>();
            private readonly List<long> _runLong = new List<long>();
            private bool _runAllInts;

            private void FlushRun(List<FbxProperty> props)
            {
                if (_run.Count == 0)
                    return;
                if (_run.Count >= MinArrayRun)
                {
                    if (_runAllInts)
                    {
                        var ints = new int[_run.Count];
                        bool fits = true;
                        for (int i = 0; i < ints.Length; ++i)
                        {
                            if (_runLong[i] > int.MaxValue || _runLong[i] < int.MinValue) { fits = false; break; }
                            ints[i] = (int)_runLong[i];
                        }
                        if (fits)
                            props.Add(FbxProperty.FromArray(ints));
                        else
                            props.Add(FbxProperty.FromArray(_runLong.ToArray()));
                    }
                    else
                    {
                        props.Add(FbxProperty.FromArray(_run.ToArray()));
                    }
                }
                else
                {
                    for (int i = 0; i < _run.Count; ++i)
                    {
                        if (_runAllInts)
                            props.Add(Math.Abs(_runLong[i]) <= int.MaxValue ? FbxProperty.FromInt((int)_runLong[i]) : FbxProperty.FromLong(_runLong[i]));
                        else
                            props.Add(FbxProperty.FromDouble(_run[i]));
                    }
                }
                _run.Clear();
                _runLong.Clear();
                _runAllInts = true;
            }

            /// <summary>Parses the comma separated values after the node name. Returns true if an array declaration (<c>*N</c>) was seen.</summary>
            private bool ParseValues(FbxNode node)
            {
                bool arrayDeclaration = false;
                _run.Clear();
                _runLong.Clear();
                _runAllInts = true;
                bool first = true;
                while (true)
                {
                    // Values may continue on the next line only after a comma.
                    SkipSpacesOnLine();
                    if (_pos >= _s.Length)
                        break;
                    char c = _s[_pos];
                    if (first && (c == '\n' || c == '\r'))
                    {
                        // Legacy files can start the values on the next line ("Key: " followed by a line of numbers).
                        int look = _pos;
                        while (look < _s.Length && (_s[look] == ' ' || _s[look] == '\t' || _s[look] == '\r' || _s[look] == '\n'))
                            ++look;
                        if (look < _s.Length && (char.IsDigit(_s[look]) || _s[look] == '-' || _s[look] == '+' || _s[look] == '"'))
                        {
                            _pos = look;
                            c = _s[_pos];
                        }
                    }
                    if (c == '\n' || c == '\r' || c == ';' || c == '{' || c == '}')
                        break;

                    if (c == '*')
                    {
                        ++_pos;
                        while (_pos < _s.Length && char.IsDigit(_s[_pos]))
                            ++_pos;
                        arrayDeclaration = true;
                    }
                    else if (c == '"')
                    {
                        FlushRun(node.Properties);
                        int end = _s.IndexOf('"', _pos + 1);
                        if (end < 0)
                            throw new FormatException("Unterminated string.");
                        node.Properties.Add(FbxProperty.FromString(_s.Substring(_pos + 1, end - _pos - 1)));
                        _pos = end + 1;
                    }
                    else if (c == '-' || c == '+' || c == '.' || char.IsDigit(c))
                    {
                        ParseNumber();
                    }
                    else if (char.IsLetter(c) || c == '_')
                    {
                        FlushRun(node.Properties);
                        int start = _pos;
                        while (_pos < _s.Length && IsIdentChar(_s[_pos]))
                            ++_pos;
                        node.Properties.Add(FbxProperty.FromString(_s.Substring(start, _pos - start)));
                    }
                    else
                    {
                        throw new FormatException($"Unexpected character '{c}' at offset {_pos}.");
                    }
                    first = false;

                    // Separator: a comma may be followed by whitespace including new lines.
                    // A comma can also start the next line.
                    int separator = _pos;
                    while (separator < _s.Length && (_s[separator] == ' ' || _s[separator] == '\t' || _s[separator] == '\r' || _s[separator] == '\n'))
                        ++separator;
                    if (separator < _s.Length && _s[separator] == ',')
                    {
                        _pos = separator + 1;
                        SkipWhitespaceAndComments();
                    }
                    else
                    {
                        break;
                    }
                }
                _ = first;
                FlushRun(node.Properties);
                return arrayDeclaration;
            }

            private void SkipSpacesOnLine()
            {
                while (_pos < _s.Length && (_s[_pos] == ' ' || _s[_pos] == '\t'))
                    ++_pos;
            }

            private void SkipWhitespaceAndComments()
            {
                while (_pos < _s.Length)
                {
                    char c = _s[_pos];
                    if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                        ++_pos;
                    else if (c == ';')
                    {
                        while (_pos < _s.Length && _s[_pos] != '\n')
                            ++_pos;
                    }
                    else
                        break;
                }
            }

            private void ParseNumber()
            {
                int start = _pos;
                bool isInt = true;
                if (_s[_pos] == '-' || _s[_pos] == '+')
                    ++_pos;
                while (_pos < _s.Length)
                {
                    char c = _s[_pos];
                    if (char.IsDigit(c))
                        ++_pos;
                    else if (c == '.')
                    {
                        isInt = false;
                        ++_pos;
                    }
                    else if (c == 'e' || c == 'E')
                    {
                        isInt = false;
                        ++_pos;
                        if (_pos < _s.Length && (_s[_pos] == '-' || _s[_pos] == '+'))
                            ++_pos;
                    }
                    else
                        break;
                }
                var token = _s.AsSpan(start, _pos - start);
                if (!double.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    throw new FormatException($"Invalid number '{token.ToString()}' at offset {start}.");
                long longValue;
                if (isInt)
                {
                    if (!long.TryParse(token.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out longValue))
                        longValue = (long)value;
                }
                else
                {
                    longValue = (long)value;
                    _runAllInts = false;
                }
                _run.Add(value);
                _runLong.Add(longValue);
            }
        }
    }
}
