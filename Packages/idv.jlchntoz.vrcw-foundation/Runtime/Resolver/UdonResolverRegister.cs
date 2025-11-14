#if UDON && !COMPILER_UDONSHARP
using System.Reflection;
using VRC.Udon;
#if UNITY_EDITOR
using UnityEditor;
#else
using UnityEngine;
#endif

namespace JLChnToZ.VRC.Foundation.Resolvers {
    static class UdonResolverRegister {
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        static void Register() {
            Resolver.CustomResolveProvider += TryResolveUdon;
        }

        static bool TryResolveUdon(object current, string propertyName, MemberInfo member, out object result) {
            if (current is UdonBehaviour udon && udon.TryGetProgramVariable(propertyName, out result))
                return true;
            result = null;
            return false;
        }
    }
}
#endif