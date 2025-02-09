using System;
using System.Collections.Generic;
using UnityEngine;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        sealed class ChangeTypeCommand : IResolverCommand {
            static readonly List<Component> tempComponents = new List<Component>();
            public readonly Type type;

            public ChangeTypeCommand(Type type = null) {
                this.type = type;
            }

            public ICommandState CreateState() => new QueueState();

            public void Reset(ICommandState state, object from) {
                if (state is QueueState s) {
                    s.queue.Clear();
                    s.queue.Enqueue(from); // Dummy
                    if (from == null) return;
                    if (type == null || type.IsInstanceOfType(from))
                        s.queue.Enqueue(from);
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
                }
            }

            public void Next(ICommandState state) {
                if (state is QueueState s) s.Next();
            }
        }
    }
}