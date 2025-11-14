using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        sealed class HierarchyChildCommand : IResolverCommand {
            static readonly char[] globSpecialChars = new [] { '*', '?', '[', ']', '{', '}' };
            readonly string childName;
            readonly Regex childNameRegex;

            public HierarchyChildCommand(string childName) {
                this.childName = childName;
                if (childName.IndexOfAny(globSpecialChars) < 0) return;
                var pattern = new StringBuilder(childName.Length);
                pattern.Append('^');
                bool bracketOpen = false;
                int braceDepth = 0;
                for (int i = 0; i < childName.Length; i++) {
                    char c = childName[i];
                    switch (c) {
                        case '*':
                            if (bracketOpen) goto case '\\';
                            pattern.Append(".*");
                            break;
                        case '?':
                            if (bracketOpen) goto case '\\';
                            pattern.Append('.');
                            break;
                        case '[':
                            if (bracketOpen) goto case '\\';
                            bracketOpen = true;
                            goto default;
                        case ']':
                            if (bracketOpen && i < childName.Length - 1 && childName[i + 1] == ']') // []]
                                goto case '\\';
                            bracketOpen = false;
                            goto default;
                        case '!':
                        case '^':
                            if (!bracketOpen || i == 0 || childName[i - 1] != '[') {
                                if (c == '^') goto case '\\';
                                goto default;
                            }
                            pattern.Append('^');
                            break;
                        case '{':
                            if (bracketOpen) goto case '\\';
                            pattern.Append("(?:");
                            braceDepth++;
                            break;
                        case '}':
                            if (braceDepth <= 0 || bracketOpen) goto default;
                            pattern.Append(')');
                            braceDepth--;
                            break;
                        case ',':
                            if (braceDepth <= 0) goto default;
                            pattern.Append('|');
                            break;
                        case '\\':
                        case '+':
                        case '|':
                        case '(':
                        case ')':
                        case '$':
                        case '.':
                        case '#':
                        case ' ':
                            pattern.Append('\\');
                            goto default;
                        default:
                            pattern.Append(c);
                            break;
                    }
                }
                if (braceDepth > 0)
                    throw new ArgumentException("Unclosed brace in glob pattern.", nameof(childName));
                if (bracketOpen)
                    throw new ArgumentException("Unclosed bracket in glob pattern.", nameof(childName));
                pattern.Append('$');
                childNameRegex = new Regex(pattern.ToString(), RegexOptions.Compiled);
            }

            public ICommandState CreateState() => new State();

            public void Reset(ICommandState state, object from) {
                if (state is State s) {
                    if (from is Transform transform)
                        s.current = transform;
                    else if (from is GameObject gameObject)
                        s.current = gameObject.transform;
                    else if (from is Component component)
                        s.current = component.transform;
                    else
                        s.current = null;
                    s.index = 0;
                }
            }

            public void Next(ICommandState state) {
                if (state is State s && s.current) {
                    for (int count = s.current.childCount; s.index < count; s.index++) {
                        var child = s.current.GetChild(s.index);
                        if (childNameRegex != null ? childNameRegex.IsMatch(child.name) : child.name == childName)
                            return;
                    }
                    s.current = null;
                }
            }

            sealed class State : ICommandState {
                public Transform current;
                public int index;
                public object Current => current.GetChild(index);
                public bool Giveup => !current || index >= current.childCount;
            }
        }
    }
}