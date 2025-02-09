using System.Reflection;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        sealed class PropertyCommand : IResolverCommand {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            readonly string propertyName;

            public PropertyCommand(string propertyName) {
                this.propertyName = propertyName;
            }

            public ICommandState CreateState() => new State();

            public void Reset(ICommandState state, object from) {
                if (state is State s) {
                    s.Reset(null);
                    s.target = from;
                    if (from != null && (s.member == null || !s.member.DeclaringType.IsInstanceOfType(from))) {
                        var type = from.GetType();
                        var field = type.GetField(propertyName, flags);
                        if (field != null) {
                            s.member = field;
                            return;
                        }
                        var property = type.GetProperty(propertyName, flags);
                        if (property != null) {
                            s.member = property;
                            return;
                        }
                    }
                }
            }

            public void Next(ICommandState state) {
                if (state is State s && s.Next()) {
                    if (s.target == null) return;
                    if (s.target is UnityObject unityObject && !unityObject) {
                        s.current = null;
                        s.flag = StateFlag.Ended;
                    }
                    foreach (var resolver in customResolvers)
                        if (resolver(s.target, propertyName, s.member, out s.current))
                            return;
                    if (s.member is FieldInfo field) {
                        s.current = field.GetValue(s.target);
                        return;
                    }
                    if (s.member is PropertyInfo property) {
                        s.current = property.GetValue(s.target);
                        return;
                    }
                    s.current = null;
                    s.flag = StateFlag.Ended;
                }
            }

            sealed class State : SingleState {
                public object target;
                public MemberInfo member;
            }
        }
    }
}