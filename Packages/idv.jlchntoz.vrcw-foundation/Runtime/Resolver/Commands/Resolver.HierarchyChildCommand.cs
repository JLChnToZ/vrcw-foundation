using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        sealed class HierarchyChildCommand : IResolverCommand {
            readonly string childName;
            readonly Regex childNameRegex;

            public HierarchyChildCommand(string childName) {
                this.childName = childName;
                if (childName.Contains("*")) {
                    var splitted = childName.Split('*');
                    var sb = new StringBuilder();
                    bool asteriskAtStart = false, asteriskAtEnd = false;
                    for (int i = 1; i < splitted.Length; i++) {
                        if (string.IsNullOrEmpty(splitted[i])) {
                            if (sb.Length == 0)
                                asteriskAtStart = true;
                            else if (i >= splitted.Length - 1)
                                asteriskAtEnd = true;
                            continue;
                        }
                        if (sb.Length > 0) sb.Append(".*");
                        else if (!asteriskAtStart) sb.Append('^');
                        sb.Append(Regex.Escape(splitted[i]));
                    }
                    if (!asteriskAtEnd) sb.Append('$');
                    childNameRegex = new Regex(sb.ToString(), RegexOptions.Compiled);
                }
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