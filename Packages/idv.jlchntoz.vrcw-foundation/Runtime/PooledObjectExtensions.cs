using System.Collections.Generic;
using UnityEngine.Pool;

namespace JLChnToZ.VRC.Foundation {
    public static class PooledObjectExtensions {
#if !COMPILER_UDONSHARP
        public static PooledObject<List<T>> Get<T>(out List<T> list, int capacity = 0) {
            var pooled = ListPool<T>.Get(out list);
            if (capacity > 0 && list.Capacity < capacity) list.Capacity = capacity;
            return pooled;
        }

        public static PooledObject<HashSet<T>> Get<T>(out HashSet<T> set, int capacity = 0) {
            var pooled = HashSetPool<T>.Get(out set);
            if (capacity > 0) set.EnsureCapacity(capacity);
            return pooled;
        }

        public static PooledObject<Dictionary<TKey, TValue>> Get<TKey, TValue>(out Dictionary<TKey, TValue> dict, int capacity = 0) {
            var pooled = DictionaryPool<TKey, TValue>.Get(out dict);
            if (capacity > 0) dict.EnsureCapacity(capacity);
            return pooled;
        }

        public static PooledObject<Stack<T>> Get<T>(out Stack<T> stack) => StackPool<T>.defaultInstance.Get(out stack);

        public static PooledObject<Queue<T>> Get<T>(out Queue<T> queue) => QueuePool<T>.defaultInstance.Get(out queue);

        sealed class StackPool<T> : ObjectPool<Stack<T>> {
            public static readonly StackPool<T> defaultInstance = new();

            static Stack<T> Create() => new();

            static void Clear(Stack<T> stack) => stack.Clear();

            public StackPool(int defaultCapacity = 10, int maxSize = 10000)
                : base(Create, Clear, defaultCapacity: defaultCapacity, maxSize: maxSize) { }
        }

        sealed class QueuePool<T> : ObjectPool<Queue<T>> {
            public static readonly QueuePool<T> defaultInstance = new();

            static Queue<T> Create() => new();

            static void Clear(Queue<T> queue) => queue.Clear();

            public QueuePool(int defaultCapacity = 10, int maxSize = 10000)
                : base(Create, Clear, defaultCapacity: defaultCapacity, maxSize: maxSize) { }
        }
#endif
    }
}