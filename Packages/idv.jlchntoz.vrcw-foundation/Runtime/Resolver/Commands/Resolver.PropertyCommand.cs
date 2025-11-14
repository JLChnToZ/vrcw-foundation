using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        sealed class PropertyCommand : IResolverCommand {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
            readonly string propertyName;
            readonly Dictionary<Type, MemberInfo> memberCache = new Dictionary<Type, MemberInfo>();

            static bool IsObjectValid(object obj) => obj != null && !(obj is UnityObject uobj && uobj == null);

            public PropertyCommand(string propertyName) {
                this.propertyName = propertyName;
            }

            object Resolve(object target) {
                if (!IsObjectValid(target)) return null;
                var type = target.GetType();
                if (!memberCache.TryGetValue(type, out var member))
                    memberCache[type] = member =
                        type.GetField(propertyName, flags) as MemberInfo ??
                        type.GetProperty(propertyName, flags);
                foreach (var resolver in customResolvers)
                    if (resolver(target, propertyName, member, out var result))
                        return result;
                if (member is FieldInfo field)
                    return field.GetValue(target);
                if (member is PropertyInfo property)
                    return property.GetValue(target);
                return null;
            }

            public ICommandState CreateState() => new State();

            public void Reset(ICommandState state, object from) {
                if (state is State s) s.Reset(from);
            }

            public void Next(ICommandState state) {
                if (state is State s) s.Next(this);
            }

            sealed class State : ICommandState {
                object target;
                object current;
                IEnumerator enumerator;

                public object Current => current;

                public bool Giveup => !IsObjectValid(current) && enumerator == null;

                public void Reset(object target) {
                    this.target = target;
                    current = null;
                    enumerator = null;
                }

                public void Next(PropertyCommand cmd) {
                    if (enumerator != null) {
                        while (enumerator.MoveNext()) {
                            current = enumerator.Current;
                            if (IsObjectValid(current)) break;
                        }
                        return;
                    }
                    enumerator = null;
                    if (IsObjectValid(target)) current = cmd.Resolve(target);
                    else current = null;
                    target = null;
                    if (current is IEnumerable enumerable && !(current is string) && !(current is Transform))
                        enumerator = enumerable.GetEnumerator();
                }
            }
        }
    }
}