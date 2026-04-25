#if !COMPILER_UDONSHARP
using System;
using System.Collections.Generic;

namespace JLChnToZ.Regex2Pattern {
    /// <summary>
    /// Parses a (subset of) regular expression pattern and produces an <see cref="IGenerator"/> that can generate all strings matching the pattern.
    /// </summary>
    /// <remarks>
    /// Supported subset of regular expression features includes:
    /// <list type="bullet">
    /// <item><description>Literal characters</description></item>
    /// <item><description>Character classes (e.g. <c>[abc]</c>, <c>[a-z]</c>, <c>[\d]</c>)</description></item>
    /// <item><description>Grouping with parentheses (e.g. <c>(abc)</c>)</description></item>
    /// <item><description>Alternation with pipe (e.g. <c>a|b</c>)</description></item>
    /// <item><description>Optional elements with question mark (e.g. <c>a?</c>)</description></item>
    /// <item><description>Explicit repetition with curly braces (e.g. <c>a{2,4}</c>)</description></item>
    /// </list>
    /// Other features unlisted above are intentionally not supported (to avoid yielding infinite sequences).<br/>
    /// The parser assumes that the input pattern is well-formed and does not contain syntax errors.
    /// If the pattern is invalid, an <see cref="ArgumentException"/> will be thrown with a descriptive error message.
    /// </remarks>
    public sealed class Parser {
        internal const string WORDS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";
        internal const string DIGITS = "0123456789";
        readonly string pattern;
        int position;

        bool IsEnded => position >= pattern.Length;

        /// <summary>
        /// Parses the given regular expression pattern and returns an <see cref="IGenerator"/> that can generate all strings matching the pattern.
        /// </summary>
        /// <param name="pattern">The regular expression pattern to parse.</param>
        /// <returns>An <see cref="IGenerator"/> that can generate all strings matching the pattern.</returns>
        /// <exception cref="ArgumentException">Thrown if the pattern is invalid.</exception>
        /// <remarks>
        /// See the class-level documentation for details on supported and unsupported regex features and error handling behavior.
        /// </remarks>
        public static IGenerator Parse(string pattern) {
            if (string.IsNullOrEmpty(pattern)) return EmptyNode.instance;
            var state = new Parser(pattern);
            var node = state.ParseExpression();
            if (!state.IsEnded) throw new ArgumentException($"Unexpected character '{state.Peek()}' at position {state.position}");
            return node;
        }

        static void AddRange(ICollection<char> chars, char from, char to) {
            if (to < from) throw new ArgumentException($"Invalid range '{from}-{to}'");
            for (char c = from; c <= to; c++) chars.Add(c);
        }

        static char ParseEscapedCharacter(char c) => c switch {
            'a' => '\a',
            'b' => '\b',
            'e' => '\x1B',
            'f' => '\f',
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            'v' => '\v',
            _ => c,
        };

        static BaseNode BuildSequence(List<BaseNode> items) => items.Count switch {
            0 => EmptyNode.instance,
            1 => items[0],
            _ => CreateSequence(items),
        };

        static SequenceNode CreateSequence(List<BaseNode> items) {
            var sequence = new SequenceNode();
            foreach (var item in items) sequence.Add(item);
            return sequence;
        }

        Parser(string pattern) => this.pattern = pattern;

        char Peek() => pattern[position];

        BaseNode ParseExpression(char terminator = '\0') {
            var options = new List<BaseNode> { ParseSequence(terminator) };
            while (!IsEnded && Peek() == '|') {
                position++;
                options.Add(ParseSequence(terminator));
            }
            if (terminator != '\0' && (IsEnded || Peek() != terminator))
                throw new ArgumentException($"Unmatched opening delimiter '{terminator}'");
            if (options.Count == 1) return options[0];
            var any = new AnyNodeNode();
            foreach (var option in options) any.Add(option);
            return any;
        }

        BaseNode ParseSequence(char terminator) {
            var items = new List<BaseNode>();
            while (!IsEnded) {
                var current = Peek();
                if (current == '|' || current == terminator) break;
                items.Add(ParseQuantifiedAtom());
            }
            return BuildSequence(items);
        }

        BaseNode ParseQuantifiedAtom() {
            var atom = ParseAtom();
            return IsEnded ? atom : Peek() switch {
                '?' => ParseOptional(atom),
                '{' => ParseExplicitRange(atom),
                _ => atom,
            };
        }

        BaseNode ParseAtom() {
            if (IsEnded) throw new ArgumentException("Unexpected end of pattern");
            return Peek() switch {
                '(' => ParseGroup(),
                '[' => ParseCharacterClass(),
                '\\' => ParseEscapedAtom(),
                ')' => throw new ArgumentException("Unmatched closing parenthesis"),
                ']' => throw new ArgumentException("Unmatched closing bracket"),
                '{' => throw new ArgumentException("Quantifier has no target"),
                '}' => throw new ArgumentException("Unmatched closing brace"),
                '?' => throw new ArgumentException("Quantifier has no target"),
                _ => new TextNode(pattern[position++].ToString()),
            };
        }

        BaseNode ParseGroup() {
            position++;
            var group = ParseExpression(')');
            position++;
            return group;
        }

        BaseNode ParseEscapedAtom() {
            position++;
            if (IsEnded) throw new ArgumentException("Dangling escape sequence");
            return pattern[position++] switch {
                'd' => AnyCharNode.Digit,
                'w' => AnyCharNode.Word,
                _ => new TextNode(ParseEscapedCharacter(pattern[position - 1]).ToString()),
            };
        }

        RepeatNode ParseOptional(BaseNode atom) {
            position++;
            return new RepeatNode(atom);
        }

        RepeatNode ParseExplicitRange(BaseNode atom) {
            position++;
            var min = ParseNumber("range minimum");
            if (IsEnded) throw new ArgumentException("Unterminated range quantifier");
            var c = Peek();
            if (c == '}') {
                position++;
                return new RepeatNode(atom, min);
            }
            if (c != ',')
                throw new ArgumentException($"Invalid character '{c}' in range quantifier");
            position++;
            var max = ParseNumber("range maximum");
            if (IsEnded || Peek() != '}') throw new ArgumentException("Unterminated range quantifier");
            if (max < min) throw new ArgumentException("Range maximum must be greater than or equal to range minimum");
            position++;
            return new RepeatNode(atom, min, max);
        }

        int ParseNumber(string context) {
            if (IsEnded || !char.IsDigit(Peek()))
                throw new ArgumentException($"Expected {context}");
            var value = 0;
            do {
                value = (value * 10) + (Peek() - '0');
                position++;
            } while (!IsEnded && char.IsDigit(Peek()));
            return value;
        }

        AnyCharNode ParseCharacterClass() {
            position++;
            var chars = new HashSet<char>();
            while (true) {
                if (IsEnded) throw new ArgumentException("Unterminated character class");
                if (Peek() == ']') break;
                var unit = ParseCharacterClassUnit();
                if (unit.Length == 1 && !IsEnded && Peek() == '-' && LookAhead() != ']') {
                    position++;
                    var rangeEnd = ParseCharacterClassUnit();
                    if (rangeEnd.Length != 1)
                        throw new ArgumentException("Character class ranges require single-character bounds");
                    AddRange(chars, unit[0], rangeEnd[0]);
                    continue;
                }
                chars.UnionWith(unit);
            }
            position++;
            if (chars.Count == 0) throw new ArgumentException("Empty character class");
            var charArray = new char[chars.Count];
            chars.CopyTo(charArray);
            return new AnyCharNode(charArray);
        }

        char[] ParseCharacterClassUnit() {
            if (IsEnded) throw new ArgumentException("Unterminated character class");
            if (Peek() == '\\') {
                position++;
                if (IsEnded) throw new ArgumentException("Dangling escape sequence");
                return pattern[position++] switch {
                    'd' => DIGITS.ToCharArray(),
                    'w' => WORDS.ToCharArray(),
                    _ => new[] { ParseEscapedCharacter(pattern[position - 1]) },
                };
            }
            return new[] { pattern[position++] };
        }

        char LookAhead() => position + 1 < pattern.Length ? pattern[position + 1] : '\0';
    }
}
#endif
