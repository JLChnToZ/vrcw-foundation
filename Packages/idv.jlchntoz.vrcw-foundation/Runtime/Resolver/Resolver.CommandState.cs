using System.Collections.Generic;

namespace JLChnToZ.VRC.Foundation.Resolvers {
    public partial class Resolver {
        interface ICommandState {
            object Current { get; }
            bool Giveup { get; }
        }

        class SingleState : ICommandState {
            public object current;
            public StateFlag flag;
            public object Current => current;
            public bool Giveup => flag != StateFlag.Available;

            public void Reset(object from) {
                current = from;
                flag = StateFlag.Reseted;
            }

            public bool Next() {
                if (flag < StateFlag.Ended) {
                    flag++;
                    return true;
                }
                return false;
            }
        }

        sealed class QueueState : ICommandState {
            public readonly Queue<object> queue = new Queue<object>();
            public object Current => queue.Peek();
            public bool Giveup => queue.Count <= 0;
            public bool Next() => queue.TryDequeue(out var _);
        }

        enum StateFlag : byte {
            Reseted,
            Available,
            Ended,
        }
    }
}