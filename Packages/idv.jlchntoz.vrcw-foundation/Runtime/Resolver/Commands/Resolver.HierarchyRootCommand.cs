using System.Collections.Generic;
using UnityEngine;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        sealed class HierarchyRootCommand : IResolverCommand {
            readonly List<GameObject> roots = new List<GameObject>();

            public static readonly HierarchyRootCommand instance = new HierarchyRootCommand();

            public ICommandState CreateState() => new QueueState();

            HierarchyRootCommand() { }

            public void Reset(ICommandState state, object from) {
                if (state is QueueState s) {
                    GameObject go;
                    if (from is GameObject gameObject)
                        go = gameObject;
                    else if (from is Component component)
                        go = component.gameObject;
                    else
                        go = null;
                    s.queue.Clear();
                    if (!go) return;
                    go.scene.GetRootGameObjects(roots);
                    foreach (var root in roots) s.queue.Enqueue(root.transform);
                }
            }

            public void Next(ICommandState state) {
                if (state is QueueState s) s.queue.TryDequeue(out var _);
            }
        }
    }
}