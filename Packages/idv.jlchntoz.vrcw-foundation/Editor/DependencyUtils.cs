using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JLChnToZ.VRC.Foundation.Editors {
    public static class DependencyUtils {
        static readonly Dictionary<Type, Type[]> dependents = new Dictionary<Type, Type[]>(), flattenedDependents = new Dictionary<Type, Type[]>();

        private static Type[] GetDependents(Type type) {
            if (!dependents.TryGetValue(type, out var types)) {
                using var pooledList = PooledObjectExtensions.Get(out List<Type> temp);
                foreach (var requireComponent in type.GetCustomAttributes<RequireComponent>(true)) {
                    AddIfNotNull(temp, requireComponent.m_Type0, type);
                    AddIfNotNull(temp, requireComponent.m_Type1, type);
                    AddIfNotNull(temp, requireComponent.m_Type2, type);
                }
                dependents[type] = types = temp.ToArray();
            }
            return types;
        }

        private static Type[] GetFlattenedDependents(Type type) {
            if (!flattenedDependents.TryGetValue(type, out var types)) {
                using var pooledList = PooledObjectExtensions.Get(out List<Type> temp);
                temp.AddRange(GetDependents(type));
                for (int i = 0; i < temp.Count; i++)
                    foreach (var t in GetDependents(temp[i]))
                        AddIfNotNull(temp, t, type);
                flattenedDependents[type] = types = temp.ToArray();
            }
            return types;
        }

        private static void AddIfNotNull(List<Type> list, Type type, Type parentType) {
            if (type != null && !type.IsAssignableFrom(parentType) && !parentType.IsAssignableFrom(type) && !list.Contains(type)) list.Add(type);
        }

        public static bool IsRequired(Type type, Type checkType, Type capableType = null, bool deep = false) {
            foreach (var t in deep ? GetFlattenedDependents(type) : GetDependents(type))
                if (t.IsAssignableFrom(checkType) && (capableType == null || !t.IsAssignableFrom(capableType)))
                    return true;
            return false;
        }

        public sealed class DependencyComparer : IComparer<Component> {
            public static readonly DependencyComparer instance = new DependencyComparer();

            private DependencyComparer() { }

            public int Compare(Component x, Component y) {
                if (x != y && x != null && y != null) {
                    Type tx = x.GetType(), ty = y.GetType();
                    if (tx != ty && !tx.IsAssignableFrom(ty) && !ty.IsAssignableFrom(tx)) {
                        var txs = GetFlattenedDependents(tx);
                        foreach (var t in txs)
                            if (t.IsAssignableFrom(ty)) return -1;
                        var tys = GetFlattenedDependents(ty);
                        foreach (var t in tys)
                            if (t.IsAssignableFrom(tx)) return 1;
                        return tys.Length.CompareTo(txs.Length); // More dependents should be later in the list.
                    }
                }
                return 0;
            }
        }
    }
}