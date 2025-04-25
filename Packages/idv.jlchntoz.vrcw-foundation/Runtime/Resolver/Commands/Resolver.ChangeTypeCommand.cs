using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        sealed class ChangeTypeCommand : IResolverCommand {
            const BindingFlags casterFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod;
            static readonly Dictionary<(Type, Type), MethodInfo> castMethods = new Dictionary<(Type, Type), MethodInfo>();
            static readonly List<Component> tempComponents = new List<Component>();
            public readonly Type type;
            
            public static readonly ChangeTypeCommand anyType = new ChangeTypeCommand();

            static MethodInfo FindCastMethod(Type fromType, Type toType)
            {
                if (fromType == toType || toType.IsAssignableFrom(fromType) || fromType.IsAssignableFrom(toType)) return null;
                return FindCastMethod(fromType, toType, fromType) ?? FindCastMethod(fromType, toType, toType);
            }

            static MethodInfo FindCastMethod(Type fromType, Type toType, Type findType) {
                var methods = findType.GetMethods(casterFlags);
                foreach (var method in methods)
                    switch (method.Name) {
                        case "op_Implicit":
                        case "op_Explicit":
                            var parameters = method.GetParameters();
                            if (parameters.Length == 1 &&
                                parameters[0].ParameterType == fromType &&
                                method.ReturnType == toType)
                                return method;
                            break;
                    }
                return null;
            }

            public ChangeTypeCommand(Type type = null) {
                this.type = type;
            }

            public ICommandState CreateState() => new QueueState();

            public void Reset(ICommandState state, object from) {
                if (state is QueueState s) {
                    s.queue.Clear();
                    s.queue.Enqueue(from); // Dummy
                    if (from == null) return;
                    bool selfYielded = false;
                    if (type == null || type.IsInstanceOfType(from)) {
                        selfYielded = true;
                        s.queue.Enqueue(from);
                    }
                    if (from is Component component) {
                        if (!component) return;
                        if (type == typeof(GameObject)) {
                            s.queue.Enqueue(component.gameObject);
                            return;
                        }
                        if (type == null || typeof(Component).IsAssignableFrom(type)) {
                            component.GetComponents(type ?? typeof(Component), tempComponents);
                            foreach (var other in tempComponents)
                                if (other != component)
                                    s.queue.Enqueue(other);
                            return;
                        }
                    } else if (from is GameObject gameObject) {
                        if (!gameObject) return;
                        if (type == null || typeof(Component).IsAssignableFrom(type)) {
                            gameObject.GetComponents(type ?? typeof(Component), tempComponents);
                            foreach (var other in tempComponents)
                                s.queue.Enqueue(other);
                            return;
                        }
                    }
                    if (!selfYielded) {
                        var fromType = from.GetType();
                        if (!castMethods.TryGetValue((fromType, type), out var castMethod))
                            castMethods[(fromType, type)] = castMethod = FindCastMethod(fromType, type);
                        if (castMethod != null)
                            s.queue.Enqueue(castMethod.Invoke(null, new object[] { from }));
                    }
                }
            }

            public void Next(ICommandState state) {
                if (state is QueueState s) s.Next();
            }
        }
    }
}