using System.Collections.Generic;
using UnityEngine;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        sealed class HierarchyAnyChildCommand : IResolverCommand {
            public static readonly HierarchyAnyChildCommand nonChainedInstance = new HierarchyAnyChildCommand(false);
            public static readonly HierarchyAnyChildCommand chainedInstance = new HierarchyAnyChildCommand(true);

            readonly bool chained;

            HierarchyAnyChildCommand(bool c) => chained = c;

            public ICommandState CreateState() => new QueueState();

            public void Reset(ICommandState state, object from) {
                if (state is QueueState s) {
                    var queue = s.queue;
                    queue.Clear();
                    queue.Enqueue(from); // Dummy
                    if (from is Transform transform)
                        Init(queue, transform);
                    else if (from is GameObject gameObject)
                        Init(queue, gameObject.transform);
                    else if (from is Component component)
                        Init(queue, component.transform);
                }
            }
            
            void Init(Queue<object> queue, Transform transform) {
                if (chained) queue.Enqueue(transform);
                else EnqueueChildren(queue, transform);
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