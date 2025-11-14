using System;
using System.Collections.Generic;
using System.Reflection;

namespace JLChnToZ.VRC.Foundation {
    public static class CastHelper {
        const BindingFlags casterFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.FlattenHierarchy;
        static readonly Dictionary<(Type, Type), MethodInfo> castMethods = new Dictionary<(Type, Type), MethodInfo>();

        static MethodInfo FindCastMethod(Type fromType, Type toType, Type findType) {
            var methods = findType.GetMethods(casterFlags);
            foreach (var method in methods)
                switch (method.Name) {
                    case "op_Implicit":
                    case "op_Explicit":
                        var parameters = method.GetParameters();
                        if (parameters.Length != 1) continue;
                        var inputType = parameters[0].ParameterType;
                        if (inputType != fromType && !inputType.IsAssignableFrom(fromType)) continue;
                        var outputType = method.ReturnType;
                        if (outputType != toType && !toType.IsAssignableFrom(outputType)) continue;
                        return method;
                }
            return null;
        }

        public static bool TryCast(object from, Type toType, out object result) {
            if (from == null) {
                result = null;
                return !toType.IsValueType || Nullable.GetUnderlyingType(toType) != null;
            }
            var fromType = from.GetType();
            if (fromType == toType || toType.IsAssignableFrom(fromType)) {
                result = from;
                return true;
            }
            var key = (fromType, toType);
            if (!castMethods.TryGetValue(key, out var method))
                castMethods[key] = method = FindCastMethod(fromType, toType, fromType) ?? FindCastMethod(fromType, toType, toType);
            if (method != null) {
                result = method.Invoke(null, new object[] { from });
                return true;
            }
            if (from is IConvertible)
                try {
                    result = Convert.ChangeType(from, toType);
                    return true;
                } catch { }
            result = null;
            return false;
        }
    }
}
