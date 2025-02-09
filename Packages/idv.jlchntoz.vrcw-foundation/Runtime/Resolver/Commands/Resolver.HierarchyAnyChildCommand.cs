using System.Collections.Generic;
using UnityEngine;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        sealed class HierarchyAnyChildCommand : IResolverCommand {
            public static HierarchyAnyChildCommand nonChainedInstance = new HierarchyAnyChildCommand(false);
            public static HierarchyAnyChildCommand chainedInstance = new HierarchyAnyChildCommand(true);

            readonly bool chained;

            HierarchyAnyChildCommand(bool c) => chained = c;

            public ICommandState CreateState() => new QueueState();

            public void Reset(ICommandState state, object from) {
                if (state is QueueState s) {
                    var queue = s.queue;
                    queue.Clear();
                    if (from is Transform transform){
                        queue.Enqueue(transform);
                        EnqueueChildren(queue, transform);
                    } else if (from is GameObject gameObject) {
                        transform = gameObject.transform;
                        queue.Enqueue(transform);
                        EnqueueChildren(queue, transform);
                    } else if (from is Component component) {
                        transform = component.transform;
                        queue.Enqueue(transform);
                        EnqueueChildren(queue, transform);
                    }
                }
            }

            public void Next(ICommandState state) {
                if (state is QueueState s && s.Next() && chained && s.queue.TryPeek(out var current))
                    EnqueueChildren(s.queue, current as Transform);
            }

            static void EnqueueChildren(Queue<object> queue, Transform transform) {
                if (queue == null || !transform) return;
                for (int i = 0, count = transform.childCount; i < count; i++)
                    queue.Enqueue(transform.GetChild(i));
            }
        }
    }
}