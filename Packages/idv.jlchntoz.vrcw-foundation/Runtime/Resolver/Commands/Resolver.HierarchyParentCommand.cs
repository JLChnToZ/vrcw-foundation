using UnityEngine;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        sealed class HierarchyParentCommand : IResolverCommand {
            public static readonly HierarchyParentCommand nonChainedInstance = new HierarchyParentCommand(false);
            public static readonly HierarchyParentCommand chainedInstance = new HierarchyParentCommand(true);
            readonly bool chained;

            HierarchyParentCommand(bool c) => chained = c;

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
                }
            }

            public void Next(ICommandState state) {
                if (state is State s && s.current)
                    s.current = chained ? s.current.parent : null;
            }
            
            sealed class State : ICommandState {
                public Transform current;
                public object Current => current;
                public bool Giveup => current == null;
            }
        }
    }
}